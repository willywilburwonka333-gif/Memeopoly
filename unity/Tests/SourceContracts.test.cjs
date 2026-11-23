// Source-contract checks are fast CI guards, NOT Unity Editor/play-mode tests.
// Run: node unity/Tests/SourceContracts.test.cjs
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname,'../..');
const read = p => fs.readFileSync(path.join(root,p),'utf8');
const rules=read('unity/Assets/Scripts/MemeopolyRules.cs');
const city=read('unity/Assets/Scripts/MemeopolyCity.cs');
const game=read('unity/Assets/Scripts/MemeopolyGame.cs');
const setup=read('unity/Assets/Editor/MemeopolyProjectSetup.cs');
const web=read('game.js');
const match=rules.match(/public static readonly string\[\] TileKinds = \(([\s\S]*?)\)\.Split/);
assert.ok(match,'Unity board 60 types defined');
const tiles=[...match[1].matchAll(/"([^"]*)"/g)].map(m=>m[1]).join('').split(',');
const old=JSON.parse(web.slice(web.indexOf('const board=')+'const board='.length,
    web.indexOf('];\n',web.indexOf('const board='))+1));
assert.equal(tiles.length,60,'one continuous track with 60 spaces');
assert.deepEqual(tiles,old.map(tile=>tile.type),'track matches production web game order');
const names=[...rules.match(/public static readonly string\[\] Landmarks\s*=\s*\{([\s\S]*?)\};/)[1].matchAll(/"([^"]+)"/g)].map(m=>m[1]);
assert.deepEqual(names,['Sharko Surf Shack','Crocwing Flight Academy','Prima Mocha Café',
  'Viral Video Tower','Meme Factory','Algorithm Palace']);
assert.match(rules,/public const int MaxLevel = 5/);
assert.match(rules,/return 500 \+ s\.buildings\[index\] \* 750 \+ era \* 350/);
assert.match(rules,/return TotalUpgrades\(s\) >= 30/);
assert.match(game,/MemeopolyRules\.Roll\(save,multiplier,a,b,random\)/);
assert.match(game,/city\.SetDice\(a,b\)/,'actual dice final faces match the roll');
assert.match(game,/city\.Token\.localPosition=Vector3\.Lerp/,'token moves smoothly');
assert.match(game,/FitWholeBoard\(\)/);
assert.doesNotMatch(game,/boardZoomIn|boardZoomOut/,'there must be no zoom controls');
assert.match(city,/private void CreateTrack\(\)/);
assert.match(city,/private void CreatePlots\(\)/);
assert.match(city,/private IEnumerator Construct\(Transform stage,int index\)/);
assert.match(city,/ParticleSystem/);
assert.match(city,/DiceTop\(int value\)/);
assert.match(setup,/EditorSceneManager\.SaveScene/);
assert.match(read('unity/Assets/Plugins/WebGL/MemeopolyLegacy.jslib'),/memeopoly-save-v2/);
for (const file of ['unity/Packages/manifest.json','unity/Assets/Scripts/Memeopoly.Runtime.asmdef',
                    'unity/Assets/Tests/Editor/Memeopoly.EditorTests.asmdef'])
    JSON.parse(read(file));
console.log('Unity 3D migration source contracts pass: 60 spaces, 6x5 buildings, dice, track, legacy save. Unity compilation and play-mode still require the Editor.');
