const CACHE_PREFIX="memeopoly-shell-v";
const CACHE=CACHE_PREFIX+"27";
const ASSETS=["./","./index.html","./style.css","./game.js","./manifest.webmanifest","./icon.svg","./assets/memeopoly-sprites.svg"];
self.addEventListener("install",event=>event.waitUntil(caches.open(CACHE).then(cache=>cache.addAll(ASSETS)).then(()=>self.skipWaiting())));
self.addEventListener("activate",event=>event.waitUntil(caches.keys().then(keys=>Promise.all(keys.filter(key=>key.startsWith(CACHE_PREFIX)&&key!==CACHE).map(key=>caches.delete(key)))).then(()=>self.clients.claim())));
self.addEventListener("fetch",event=>{
  if(event.request.method!=="GET"||new URL(event.request.url).origin!==self.location.origin)return;
  event.respondWith(fetch(event.request).then(response=>{
    if(response.ok){
      const copy=response.clone();
      event.waitUntil(caches.open(CACHE).then(cache=>cache.put(event.request,copy)));
    }
    return response;
  }).catch(async()=>{
    const cached=await caches.match(event.request);
    if(cached)return cached;
    // Only a document navigation can safely receive the offline HTML shell.
    // Returning HTML for missing JS/CSS/image requests breaks the game on reload.
    if(event.request.mode==="navigate")return await caches.match("./index.html")||Response.error();
    return Response.error();
  }));
});