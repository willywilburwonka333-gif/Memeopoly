// Run with: node --test tests/cache-cleanup.test.cjs
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');

test('activation removes only obsolete Memeopoly shell caches', async () => {
  const keys = ['memeopoly-shell-v21', 'memeopoly-shell-v22', 'memeopoly-shell-v23', 'another-app-cache', 'memeopoly-artwork', 'memeopoly-shell-v23-extra'];
  const deleted = [];
  let activation;
  let claimed = false;
  const context = {
    caches: {
      keys: async () => keys,
      delete: async key => { deleted.push(key); return true; }
    },
    self: {
      addEventListener(type, handler) { if (type === 'activate') activation = handler; },
      clients: { claim: async () => { claimed = true; } }
    }
  };
  vm.runInNewContext(fs.readFileSync(path.join(__dirname, '..', 'sw.js'), 'utf8'), context);
  assert.equal(typeof activation, 'function');
  let pending;
  activation({ waitUntil(promise) { pending = promise; } });
  assert.ok(pending, 'activation must extend service-worker lifetime');
  await pending;
  assert.deepEqual(deleted.sort(), ['memeopoly-shell-v21', 'memeopoly-shell-v22', 'memeopoly-shell-v23-extra'].sort());
  assert.equal(claimed, true);
});

test('activation retains current cache even when it is the only cache', async () => {
  const deleted = [];
  let activation;
  const context = {
    caches: { keys: async () => ['memeopoly-shell-v23'], delete: async key => { deleted.push(key); return true; } },
    self: { addEventListener(type, handler) { if (type === 'activate') activation = handler; }, clients: { claim: async () => {} } }
  };
  vm.runInNewContext(fs.readFileSync(path.join(__dirname, '..', 'sw.js'), 'utf8'), context);
  let pending;
  activation({ waitUntil(promise) { pending = promise; } });
  await pending;
  assert.deepEqual(deleted, []);
});
