import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import {
  argsFor,
  contractFor,
  entryFromScVal,
  Keypair,
  nativeToScVal,
  query,
  sendOperation,
  transactionStatus,
} from './network.mjs';
let raw = '';
for await (const chunk of process.stdin) {
  raw += chunk;
  if (raw.length > 16384) throw new Error('Solicitud demasiado grande');
}
try {
  const input = JSON.parse(raw);
  const manifest = JSON.parse(
    await readFile(new URL('./testnet.json', import.meta.url), 'utf8'),
  );
  if (manifest.network !== 'Testnet')
    throw new Error('Solo se permite Testnet.');
  const contractId = input.contractId || manifest.contractId;
  const contract = contractFor(contractId);
  let output;
  if (input.command === 'status')
    output = {
      contractId,
      admin: manifest.admin,
      network: 'Testnet',
      rpc: 'https://soroban-testnet.stellar.org',
    };
  else if (input.command === 'transaction')
    output = await transactionStatus(input.transactionId);
  else if (input.command === 'read') {
    const args = argsFor(input.kind, input.id);
    const op = input.version
      ? contract.call(
          'get',
          ...args,
          nativeToScVal(input.version, { type: 'u32' }),
        )
      : contract.call('latest', ...args);
    output = {
      contractId,
      entry: entryFromScVal(await query(manifest.admin, op)),
    };
  } else if (input.command === 'register') {
    if (!input.secret)
      throw new Error(
        'Configure Stellar__SecretSeed o el secreto Stellar:SecretSeed en el backend.',
      );
    if (!/^[a-f0-9]{64}$/i.test(input.hash))
      throw new Error('SHA-256 invalido.');
    const key = Keypair.fromSecret(input.secret);
    if (key.publicKey() !== manifest.admin)
      throw new Error(
        'La cuenta configurada no es la administradora de este contrato.',
      );
    const bytes = Uint8Array.from(input.hash.match(/../g), (x) =>
      parseInt(x, 16),
    );
    const sent = await sendOperation(
      key,
      contract.call(
        'register',
        ...argsFor(input.kind, input.id),
        nativeToScVal(bytes, { type: 'bytes' }),
      ),
    );
    output = {
      contractId,
      transactionId: sent.transactionId,
      entry: entryFromScVal(sent.result.returnValue),
    };
  } else throw new Error('Operacion no permitida.');
  process.stdout.write(JSON.stringify({ ok: true, ...output }));
} catch (error) {
  process.stdout.write(
    JSON.stringify({
      ok: false,
      error: String(error.message).replace(/S[A-Z2-7]{55}/g, '[redactado]'),
      transactionId: error.transactionId || null,
      pending: !!error.transactionId && !error.definitive,
    }),
  );
  process.exitCode = 1;
}
