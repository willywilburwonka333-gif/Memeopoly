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

// Unity Cloud Build #2: a literal \n between NUnit assertions broke C# compilation.
// Test all C# sources so accidental backslash-n statement separators cannot return.
for (const sourcePath of [
  'unity/Assets/Scripts/MemeopolyGame.cs',
  'unity/Assets/Scripts/MemeopolyCity.cs',
  'unity/Assets/Scripts/MemeopolyRules.cs',
  'unity/Assets/Editor/MemeopolyProjectSetup.cs',
  'unity/Assets/Editor/MemeopolyWebGLBuild.cs',
  'unity/Assets/Tests/Editor/RulesTests.cs'
]) {
  assert.doesNotMatch(read(sourcePath), /;\\n(?=\s*[A-Za-z_])/,
    sourcePath+' has a literal backslash-n after a statement, not a C# line break');
}
assert.match(read('unity/Assets/Tests/Editor/RulesTests.cs'),
  /Assert\.AreEqual\(6,s\.lastDieA\);\s+Assert\.AreEqual\(4,s\.lastDieB\);/,
  'roll assertions remain intact as two valid C# statements');


// Mobile WebGL / presentation regression guards.
// The old default fixed-size WebGL page clipped the board inside iPhone Safari.
const mobileTemplate = read('unity/Assets/WebGLTemplates/MemeopolyMobile/index.html');
const mobileHook = read('unity/Assets/Editor/MemeopolyMobileWebGLPrebuild.cs');
assert.match(mobileTemplate,/viewport-fit=cover/,'supports iPhone safe areas');
assert.match(mobileTemplate,/width:100%;height:100%;height:100dvh/,'fills mobile browser viewport');
assert.match(mobileTemplate,/matchWebGLToCanvasSize:true/,'Unity render target follows resize');
assert.match(mobileTemplate,/createUnityInstance\(canvas,config/,'boots the actual Unity player');
assert.match(mobileHook,/IPreprocessBuildWithReport/,'works with Unity Cloud Build Automation');
assert.match(mobileHook,/PROJECT:MemeopolyMobile/,'Unity Cloud selects our mobile HTML template');
assert.match(game,/Screen\.width > Screen\.height \? 1f : 0f/,
  'landscape embedded players use height-based UI scale');
assert.match(game,/boardCamera\.orthographicSize\s*=/,
  'camera fits projected board bounds');
assert.match(game,/private void FitWholeBoard\(\)/);
assert.match(game,/new Vector2\(0,-92\)/,'compact top HUD');
assert.match(game,/new Vector2\(0,162\)/,'compact bottom dock');
assert.doesNotMatch(game,/new Vector2\(0,202\)/,'old oversized dock removed');
assert.match(city,/CENTRAL VIRAL PLAZA/,'empty boards still have a central feature');
assert.match(city,/void HighlightSelection\(int chosen\)/,'selected buildable plot highlighted');
assert.match(city,/foundation blueprints/,'zero-level buildings have visible foundations');


// Gameplay depth and original content regression guards.
// These validate source intent; a Unity Cloud build and a phone playthrough
// are still required to prove actual engine and rendering behaviour.
const progression=read('unity/Assets/Scripts/MemeopolyProgression.cs');
const audio=read('unity/Assets/Scripts/MemeopolyAudio.cs');
const art=read('unity/Assets/Scripts/MemeopolyArt.cs');
const effects=read('unity/Assets/Scripts/MemeopolyVfx.cs');
assert.match(rules,/public bool pendingHeist/);
assert.match(rules,/public bool pendingAttack/);
assert.match(rules,/s\.pendingHeist = true/);
assert.match(rules,/s\.pendingAttack = true/);
assert.match(rules,/MemeopolyProgression\.AwardCompleteAlbum/);
assert.match(progression,/string ResolveHeist\(/);
assert.match(progression,/string ResolveRival\(/);
assert.match(progression,/bool ClaimDaily\(/);
assert.match(progression,/int CardCount\(/);
assert.match(progression,/public static readonly string\[\] Tokens/);
assert.match(game,/ShowPanel\(ScreenPanel\.Heist\)/);
assert.match(game,/ShowPanel\(ScreenPanel\.Rival\)/);
assert.match(game,/ShowPanel\(ScreenPanel\.Missions\)/);
assert.match(game,/ShowPanel\(ScreenPanel\.Album\)/);
assert.match(game,/ShowPanel\(ScreenPanel\.Tokens\)/);
assert.match(game,/MemeopolyProgression\.OnRoll/);
assert.match(game,/MemeopolyProgression\.OnBuild/);
assert.match(game,/city\.SelectToken\(save\.selectedToken\)/);
assert.match(game,/MemeopolyCity\.DiceTop\(rolled\)/);
assert.match(game,/MemeopolyVfx\.Burst/);
assert.match(city,/public void SelectToken\(int index\)/);
assert.match(city,/new GameObject\("SHARKO/);
assert.match(city,/new GameObject\("CROCWING/);
assert.match(city,/new GameObject\("VIRAL CAT/);
assert.match(city,/new Material\(tileMaterials\[k\]\)/);
assert.match(city,/MemeopolyArt\.MakeTile\(k\)/);
assert.match(city,/void ApplyWorldTheme/);
assert.match(city,/private GameObject Roof\(/);
assert.match(city,/materialCache\.TryGetValue/);
assert.match(art,/new Texture2D\(N,N,TextureFormat\.RGBA32,false\)/);
assert.match(audio,/AudioClip\.Create/);
assert.match(audio,/public void SetMuted/);
assert.match(audio,/public void Play\(MemeopolySfx kind\)/);
assert.match(read('unity/Assets/Scripts/MemeopolyUIArt.cs'),/Sprite\.Create/);
assert.match(game,/MemeopolyUIArt\.Rounded/,'all HUD panels and buttons use original rounded visuals');
assert.match(effects,/public static void Burst/);
assert.match(effects,/new ParticleSystem\.MinMaxGradient/);
assert.match(read('unity/Assets/Tests/Editor/RulesTests.cs'),/AlbumCollectingAllEightAwardsOnce/);
assert.match(read('unity/Assets/Tests/Editor/RulesTests.cs'),/HeistRequiresPlayerChoiceAndCannotBeSkipped/);
assert.match(read('unity/Assets/Tests/Editor/RulesTests.cs'),/MissionsPayOnlyOnceAndResetOnNewUtcDay/);

console.log('Unity 3D migration source contracts pass: 60 spaces, 6x5 buildings, dice, track, legacy save. Unity compilation and play-mode still require the Editor.');
