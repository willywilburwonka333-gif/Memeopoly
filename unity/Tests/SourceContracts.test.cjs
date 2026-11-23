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

// Build-safety guards: the Unity project must compile a real UGUI runtime
// and ship a registered gameplay scene even on a clean cloud checkout.
assert.doesNotMatch(game, /;\\n[ \t]*(?:private|int|diceSettled|Notify)/,
  'literal backslash-n outside a C# string makes compilation fail');
const runtimeAssembly = JSON.parse(read('unity/Assets/Scripts/Memeopoly.Runtime.asmdef'));
assert.ok(runtimeAssembly.references.includes('UnityEngine.UI'),
  'the custom runtime assembly must reference the Unity UGUI package');
const scene = read('unity/Assets/Scenes/Memeopoly.unity');
const sceneMeta = read('unity/Assets/Scenes/Memeopoly.unity.meta');
const scriptMeta = read('unity/Assets/Scripts/MemeopolyGame.cs.meta');
const buildSettings = read('unity/ProjectSettings/EditorBuildSettings.asset');
const sceneGuid = sceneMeta.match(/^guid: ([a-f0-9]{32})$/m)?.[1];
const scriptGuid = scriptMeta.match(/^guid: ([a-f0-9]{32})$/m)?.[1];
assert.ok(sceneGuid && scriptGuid,'Unity scene and gameplay script have stable asset GUIDs');
assert.ok(scene.includes('guid: '+scriptGuid+', type: 3'),'scene attaches the actual MemeopolyGame component');
assert.ok(buildSettings.includes('path: Assets/Scenes/Memeopoly.unity'));
assert.ok(buildSettings.includes('guid: '+sceneGuid),'scene is registered for cloud builds');
assert.ok(scene.includes('m_Name: Memeopoly'),'the launch scene contains the player');

// Unity's Color.Lerp accepts Color, not Material. Keep both shaded-building paths compile-safe.
assert.match(city,/Color\.Lerp\(buildingColours\[i\],ink\.color,\.68f\)/,
  'plot colour interpolates the ink material colour');
assert.match(city,/Color\.Lerp\(buildingColours\[index\],cream\.color,\.48f\)/,
  'stage colour interpolates the cream material colour');
assert.doesNotMatch(city,/Color\.Lerp\([^\n]*,(?:ink|cream),/,
  'never pass a UnityEngine.Material to Color.Lerp');

// Unity Cloud Build #2 showed a literal \\n between two NUnit assertions.
// Guard every tracked C# file; these invalid escapes caused real C# compiler failures.
for (const sourcePath of [
  'unity/Assets/Scripts/MemeopolyGame.cs',
  'unity/Assets/Scripts/MemeopolyCity.cs',
  'unity/Assets/Scripts/MemeopolyRules.cs',
  'unity/Assets/Editor/MemeopolyProjectSetup.cs',
  'unity/Assets/Editor/MemeopolyWebGLBuild.cs',
  'unity/Assets/Tests/Editor/RulesTests.cs'
]) {
  assert.doesNotMatch(read(sourcePath), /;\\\\n(?=\\s*[A-Za-z_])/, 
    sourcePath+' has a literal \\\\n after a statement instead of a real C# line break');
}
assert.match(read('unity/Assets/Tests/Editor/RulesTests.cs'),
  /Assert\\.AreEqual\\(6,s\\.lastDieA\\);\\s+Assert\\.AreEqual\\(4,s\\.lastDieB\\);/,
  'roll assertions remain intact as two valid C# statements');

console.log('Unity 3D migration source contracts pass: 60 spaces, 6x5 buildings, dice, track, legacy save. Unity compilation and play-mode still require the Editor.');
