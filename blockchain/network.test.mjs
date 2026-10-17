import test from 'node:test';
import assert from 'node:assert/strict';
import {
  argsFor,
  entryFromScVal,
  nativeToScVal,
  validateTarget,
} from './network.mjs';

test('only traceable kinds and SQL identifiers are accepted', () => {
  for (const kind of ['lote', 'pedido', 'certificado'])
    assert.equal(argsFor(kind, 1).length, 2);
  for (const [kind, id] of [
    ['otro', 1],
    ['lote', 0],
    ['lote', -1],
    ['lote', 1.1],
    ['lote', 2147483648],
  ])
    assert.throws(() => validateTarget(kind, id));
});
test('empty contract storage is reported as missing evidence', () => {
  assert.equal(entryFromScVal(nativeToScVal(null)), null);
});
test('contract evidence preserves all 32 hash bytes and numeric metadata', () => {
  const value = nativeToScVal({
    hash: Uint8Array.from({ length: 32 }, (_, n) => n),
    author: 'admin',
    version: 3,
    ledger: 100,
    timestamp: 2000n,
  });
  const entry = entryFromScVal(value);
  assert.equal(
    entry.hash,
    '000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f',
  );
  assert.deepEqual(
    { ...entry, hash: undefined },
    {
      hash: undefined,
      author: 'admin',
      version: 3,
      ledger: 100,
      timestamp: 2000,
    },
  );
});
