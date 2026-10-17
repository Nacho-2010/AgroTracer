import {
  Address,
  BASE_FEE,
  Contract,
  Keypair,
  Networks,
  TransactionBuilder,
  nativeToScVal,
  rpc,
  scValToNative,
} from '@stellar/stellar-sdk';
export const RPC = 'https://soroban-testnet.stellar.org';
export const server = new rpc.Server(RPC, { timeout: 10_000 });
export const networkPassphrase = Networks.TESTNET;
export const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
export function validateTarget(kind, id) {
  if (
    !['lote', 'pedido', 'certificado'].includes(kind) ||
    !Number.isSafeInteger(id) ||
    id < 1 ||
    id > 2147483647
  )
    throw new Error('Tipo o identificador de trazabilidad invalido.');
}
export function argsFor(kind, id) {
  validateTarget(kind, id);
  return [
    nativeToScVal(kind, { type: 'symbol' }),
    nativeToScVal(BigInt(id), { type: 'u64' }),
  ];
}
export function contractFor(id) {
  return new Contract(id);
}
export function entryFromScVal(value) {
  const e = scValToNative(value);
  if (e === undefined || e === null) return null;
  return {
    hash:
      typeof e.hash === 'string'
        ? e.hash
        : Array.from(e.hash, (b) => b.toString(16).padStart(2, '0')).join(''),
    author: e.author,
    version: Number(e.version),
    ledger: Number(e.ledger),
    timestamp: Number(e.timestamp),
  };
}
export async function query(accountId, operation) {
  const account = await server.getAccount(accountId);
  const tx = new TransactionBuilder(account, {
    fee: BASE_FEE,
    networkPassphrase,
  })
    .addOperation(operation)
    .setTimeout(60)
    .build();
  const simulation = await server.simulateTransaction(tx);
  if (!rpc.Api.isSimulationSuccess(simulation) || !simulation.result)
    throw new Error(
      'No se pudo consultar el contrato: ' +
        (simulation.error || 'respuesta sin resultado'),
    );
  return simulation.result.retval;
}
export async function transactionStatus(hash) {
  if (!/^[a-f0-9]{64}$/i.test(hash))
    throw new Error('Hash de transaccion invalido.');
  const result = await server.getTransaction(hash);
  return {
    status: result.status,
    entry:
      result.status === 'SUCCESS' && result.returnValue
        ? entryFromScVal(result.returnValue)
        : null,
  };
}
export async function sendOperation(key, operation) {
  const account = await server.getAccount(key.publicKey());
  const base = new TransactionBuilder(account, {
    fee: BASE_FEE,
    networkPassphrase,
  })
    .addOperation(operation)
    .setTimeout(120)
    .build();
  const prepared = await server.prepareTransaction(base);
  if (BigInt(prepared.fee) > 50_000_000n)
    throw new Error('La comision de prueba supera el limite de 5 XLM.');
  prepared.sign(key);
  const hash = Array.from(prepared.hash(), (b) =>
    b.toString(16).padStart(2, '0'),
  ).join('');
  try {
    const sent = await server.sendTransaction(prepared);
    if (sent.status === 'ERROR')
      throw Object.assign(
        new Error('Stellar rechazo la transaccion del contrato.'),
        { transactionId: hash, definitive: true },
      );
    for (let n = 0; n < 30; n++) {
      const result = await server.getTransaction(hash);
      if (result.status === 'SUCCESS') return { transactionId: hash, result };
      if (result.status === 'FAILED')
        throw Object.assign(
          new Error('La invocacion del contrato fallo en Testnet.'),
          { transactionId: hash, definitive: true },
        );
      await sleep(1500);
    }
    throw new Error(
      'La confirmacion sigue pendiente. Consulta nuevamente antes de registrar otro cambio.',
    );
  } catch (error) {
    error.transactionId = hash;
    throw error;
  }
}
export { Address, Keypair, nativeToScVal, scValToNative };
