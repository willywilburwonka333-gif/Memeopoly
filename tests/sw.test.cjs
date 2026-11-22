const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');

// Exercise the actual service worker in an isolated environment without a browser.
async function run() {
  const handlers = {};
  const entries = new Map();
  const shell = { kind: 'html' };
  entries.set('./index.html', shell);
  const cache = {
    addAll: async () => {},
    put: async (request, response) => { entries.set(request.url, response); },
  };
  const caches = {
    open: async () => cache,
    keys: async () => [],
    delete: async () => true,
    match: async request => entries.get(typeof request === 'string' ? request : request.url),
  };
  let network = async () => { throw Error('offline'); };
  const context = {
    caches, URL,
    Response: { error: () => ({ kind: 'network-error' }) },
    fetch: request => network(request),
    self: {
      location: { origin: 'https://memeopoly.example' },
      clients: { claim: async () => {} },
      skipWaiting: async () => {},
      addEventListener: (name, handler) => { handlers[name] = handler; },
    },
  };
  vm.runInNewContext(fs.readFileSync('sw.js', 'utf8'), context);
  async function dispatch(url, mode = 'no-cors', method = 'GET') {
    let result;
    handlers.fetch({
      request: { url, mode, method },
      respondWith: promise => { result = promise; },
      waitUntil: () => {},
    });
    return result;
  }
  const origin = 'https://memeopoly.example';
  assert.equal((await dispatch(`${origin}/game.js`)).kind, 'network-error', 'missing JS must not receive HTML');
  assert.equal((await dispatch(`${origin}/style.css`)).kind, 'network-error', 'missing CSS must not receive HTML');
  assert.equal(await dispatch(`${origin}/offline`, 'navigate'), shell, 'navigation gets offline shell');
  const cachedAsset = { kind: 'javascript' };
  entries.set(`${origin}/game.js`, cachedAsset);
  assert.equal(await dispatch(`${origin}/game.js`), cachedAsset, 'cached asset is available offline');
  network = async () => ({ ok: true, clone: () => ({ kind: 'new' }), kind: 'online' });
  assert.equal((await dispatch(`${origin}/game.js`)).kind, 'online', 'online response wins');
  assert.equal(await dispatch('https://external.example/game.js'), undefined, 'cross-origin request is untouched');
  assert.equal(await dispatch(`${origin}/game.js`, 'no-cors', 'POST'), undefined, 'non-GET request is untouched');
  console.log('Service worker offline routing tests passed.');
}
run().catch(error => { console.error(error); process.exitCode = 1; });
