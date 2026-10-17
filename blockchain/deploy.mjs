import { readFile, writeFile } from 'node:fs/promises';
import { createHash, randomBytes } from 'node:crypto';
import { Operation, Address } from '@stellar/stellar-sdk';
import { Keypair, sendOperation, scValToNative } from './network.mjs';
let input = '';
for await (const chunk of process.stdin) input += chunk;
try {
  const { secret } = JSON.parse(input);
  const key = Keypair.fromSecret(secret);
  const wasm = await readFile(
    new URL(
      '../contracts/target/wasm32v1-none/release/agrotrace_registry.wasm',
      import.meta.url,
    ),
  );
  const sha256 = createHash('sha256').update(wasm).digest('hex');
  const upload = await sendOperation(
    key,
    Operation.uploadContractWasm({ wasm }),
  );
  const deploy = await sendOperation(
    key,
    Operation.createCustomContract({
      address: Address.fromString(key.publicKey()),
      wasmHash: Uint8Array.from(sha256.match(/../g), (x) => parseInt(x, 16)),
      salt: randomBytes(32),
      constructorArgs: [Address.fromString(key.publicKey()).toScVal()],
    }),
  );
  const contractId = scValToNative(deploy.result.returnValue);
  const manifest = {
    network: 'Testnet',
    rpc: 'https://soroban-testnet.stellar.org',
    contractId,
    admin: key.publicKey(),
    wasmSha256: sha256,
    uploadTransaction: upload.transactionId,
    deployTransaction: deploy.transactionId,
    sdkVersion: '29.0.1',
  };
  await writeFile(
    new URL('./testnet.json', import.meta.url),
    JSON.stringify(manifest, null, 2) + '\n',
  );
  console.log(JSON.stringify(manifest));
} catch (error) {
  console.error(String(error.message).replace(/S[A-Z2-7]{55}/g, '[redactado]'));
  process.exitCode = 1;
}
