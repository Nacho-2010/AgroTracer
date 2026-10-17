#![no_std]
use soroban_sdk::{
    contract, contracterror, contractevent, contractimpl, contracttype, panic_with_error,
    symbol_short, Address, BytesN, Env, Symbol,
};

const THRESHOLD: u32 = 17_280;
const EXTEND_TO: u32 = 518_400;

#[contracttype]
#[derive(Clone)]
enum Key {
    Admin,
    Latest(Symbol, u64),
    Record(Symbol, u64, u32),
}

#[contracttype]
#[derive(Clone, Debug, Eq, PartialEq)]
pub struct Entry {
    pub hash: BytesN<32>,
    pub author: Address,
    pub version: u32,
    pub ledger: u32,
    pub timestamp: u64,
}

#[contracterror]
#[derive(Copy, Clone, Debug, Eq, PartialEq)]
#[repr(u32)]
pub enum Error {
    InvalidKind = 1,
    InvalidId = 2,
    VersionOverflow = 3,
    MissingRecord = 4,
}

#[contractevent(topics = ["record"])]
pub struct Registered {
    #[topic]
    pub kind: Symbol,
    #[topic]
    pub id: u64,
    pub entry: Entry,
}

#[contract]
pub struct AgroTrace;

fn validate(env: &Env, kind: &Symbol, id: u64) {
    if kind != &symbol_short!("lote")
        && kind != &symbol_short!("pedido")
        && kind != &Symbol::new(env, "certificado")
    {
        panic_with_error!(env, Error::InvalidKind);
    }
    if id == 0 {
        panic_with_error!(env, Error::InvalidId);
    }
}

#[contractimpl]
impl AgroTrace {
    pub fn __constructor(env: Env, admin: Address) {
        env.storage().instance().set(&Key::Admin, &admin);
    }

    pub fn admin(env: Env) -> Address {
        env.storage().instance().get(&Key::Admin).unwrap()
    }

    pub fn register(env: Env, kind: Symbol, id: u64, hash: BytesN<32>) -> Entry {
        let admin = Self::admin(env.clone());
        admin.require_auth();
        validate(&env, &kind, id);
        env.storage().instance().extend_ttl(THRESHOLD, EXTEND_TO);
        let pointer = Key::Latest(kind.clone(), id);
        let version: u32 = env.storage().persistent().get(&pointer).unwrap_or(0);
        if version > 0 {
            let key = Key::Record(kind.clone(), id, version);
            let last: Entry = env.storage().persistent().get(&key).unwrap();
            if last.hash == hash {
                env.storage()
                    .persistent()
                    .extend_ttl(&key, THRESHOLD, EXTEND_TO);
                env.storage()
                    .persistent()
                    .extend_ttl(&pointer, THRESHOLD, EXTEND_TO);
                return last;
            }
        }
        let next = version
            .checked_add(1)
            .unwrap_or_else(|| panic_with_error!(&env, Error::VersionOverflow));
        let entry = Entry {
            hash,
            author: admin,
            version: next,
            ledger: env.ledger().sequence(),
            timestamp: env.ledger().timestamp(),
        };
        let key = Key::Record(kind.clone(), id, next);
        env.storage().persistent().set(&key, &entry);
        env.storage().persistent().set(&pointer, &next);
        env.storage()
            .persistent()
            .extend_ttl(&key, THRESHOLD, EXTEND_TO);
        env.storage()
            .persistent()
            .extend_ttl(&pointer, THRESHOLD, EXTEND_TO);
        Registered {
            kind,
            id,
            entry: entry.clone(),
        }
        .publish(&env);
        entry
    }

    pub fn latest(env: Env, kind: Symbol, id: u64) -> Option<Entry> {
        validate(&env, &kind, id);
        let version: Option<u32> = env
            .storage()
            .persistent()
            .get(&Key::Latest(kind.clone(), id));
        version.and_then(|v| Self::get(env, kind, id, v))
    }

    pub fn get(env: Env, kind: Symbol, id: u64, version: u32) -> Option<Entry> {
        validate(&env, &kind, id);
        env.storage()
            .persistent()
            .get(&Key::Record(kind, id, version))
    }

    pub fn renew(env: Env, kind: Symbol, id: u64, version: u32) {
        Self::admin(env.clone()).require_auth();
        validate(&env, &kind, id);
        let key = Key::Record(kind.clone(), id, version);
        if !env.storage().persistent().has(&key) {
            panic_with_error!(&env, Error::MissingRecord);
        }
        env.storage().instance().extend_ttl(THRESHOLD, EXTEND_TO);
        env.storage()
            .persistent()
            .extend_ttl(&key, THRESHOLD, EXTEND_TO);
        env.storage()
            .persistent()
            .extend_ttl(&Key::Latest(kind, id), THRESHOLD, EXTEND_TO);
    }
}

#[cfg(test)]
mod test {
    use super::*;
    use soroban_sdk::testutils::{Address as _, Ledger};

    fn setup() -> (Env, Address, Address) {
        let env = Env::default();
        let admin = Address::generate(&env);
        let contract = env.register(AgroTrace, (&admin,));
        (env, admin, contract)
    }

    #[test]
    fn initializes_admin_and_empty_state() {
        let (env, admin, id) = setup();
        let client = AgroTraceClient::new(&env, &id);
        assert_eq!(client.admin(), admin);
        assert_eq!(client.latest(&symbol_short!("lote"), &1), None);
    }

    #[test]
    fn versions_are_immutable_and_duplicate_is_idempotent() {
        let (env, admin, id) = setup();
        env.mock_all_auths();
        env.ledger().with_mut(|l| {
            l.sequence_number = 100;
            l.timestamp = 1000;
        });
        let client = AgroTraceClient::new(&env, &id);
        let first = client.register(
            &symbol_short!("lote"),
            &1,
            &BytesN::from_array(&env, &[1; 32]),
        );
        assert_eq!(first.version, 1);
        assert_eq!(first.author, admin);
        assert_eq!(first.ledger, 100);
        let duplicate = client.register(&symbol_short!("lote"), &1, &first.hash);
        assert_eq!(duplicate, first);
        let second = client.register(
            &symbol_short!("lote"),
            &1,
            &BytesN::from_array(&env, &[2; 32]),
        );
        assert_eq!(second.version, 2);
        assert_eq!(client.get(&symbol_short!("lote"), &1, &1), Some(first));
        assert_eq!(client.latest(&symbol_short!("lote"), &1), Some(second));
    }

    #[test]
    fn kinds_and_ids_are_separate() {
        let (env, _, id) = setup();
        env.mock_all_auths();
        let c = AgroTraceClient::new(&env, &id);
        let a = c.register(
            &symbol_short!("lote"),
            &1,
            &BytesN::from_array(&env, &[1; 32]),
        );
        let b = c.register(
            &symbol_short!("pedido"),
            &1,
            &BytesN::from_array(&env, &[2; 32]),
        );
        let cert = c.register(
            &Symbol::new(&env, "certificado"),
            &1,
            &BytesN::from_array(&env, &[3; 32]),
        );
        assert_eq!(a.version, 1);
        assert_eq!(b.version, 1);
        assert_eq!(cert.version, 1);
        assert!(c.latest(&symbol_short!("lote"), &2).is_none());
    }

    #[test]
    fn unsigned_registration_is_rejected() {
        let (env, _, id) = setup();
        let c = AgroTraceClient::new(&env, &id);
        assert!(c
            .try_register(
                &symbol_short!("lote"),
                &1,
                &BytesN::from_array(&env, &[1; 32])
            )
            .is_err());
    }

    #[test]
    fn invalid_kind_and_zero_id_are_rejected() {
        let (env, _, id) = setup();
        env.mock_all_auths();
        let c = AgroTraceClient::new(&env, &id);
        assert_eq!(
            c.try_register(
                &symbol_short!("otro"),
                &1,
                &BytesN::from_array(&env, &[1; 32])
            ),
            Err(Ok(soroban_sdk::Error::from_contract_error(
                Error::InvalidKind as u32
            )))
        );
        assert_eq!(
            c.try_register(
                &symbol_short!("lote"),
                &0,
                &BytesN::from_array(&env, &[1; 32])
            ),
            Err(Ok(soroban_sdk::Error::from_contract_error(
                Error::InvalidId as u32
            )))
        );
    }

    #[test]
    fn renewal_preserves_evidence() {
        let (env, _, id) = setup();
        env.mock_all_auths();
        let c = AgroTraceClient::new(&env, &id);
        let entry = c.register(
            &symbol_short!("lote"),
            &1,
            &BytesN::from_array(&env, &[1; 32]),
        );
        c.renew(&symbol_short!("lote"), &1, &1);
        assert_eq!(c.get(&symbol_short!("lote"), &1, &1), Some(entry));
    }
}
