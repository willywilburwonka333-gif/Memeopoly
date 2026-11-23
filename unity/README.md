# Memeopoly · Native Unity 3D migration (playable first stage)

This is the **real Unity implementation** alongside the original Vercel game. The browser app in the repository root remains untouched. Work in `unity/` to build the native edition.

## Open it

1. Install **Unity Hub** and a compatible **Unity 6** editor with **Android Build Support**, **iOS Build Support** (macOS), and optional **WebGL Build Support**.
2. In Unity Hub, **Add project from disk** and select the `Memeopoly/unity` folder, **not** the repository root.
3. Let packages and C# scripts import. The included Editor initializer generates and opens `Assets/Scenes/Memeopoly.unity` on first import and adds it to build settings.
4. Press **Play**. You should see an isometric **single 60-space continuous board**, the six central animated constructions, the persistent Sharko token and two actual 3D dice. Use mouse or touchscreen to tap a building and **BUILD**; tap **ROLL THE FEED** for real 2–12 movement.
5. To build a test APK, select **File > Build Profiles > Android**. For iPhone use iOS Build Support and Xcode on a Mac. Set orientation to portrait. Optional WebGL builds can import an existing root-webapp browser save on the **same origin**.

## Committed functionality

- Original web game still works; /unity is separate.
- Board **fits in the available camera viewport**, no scroll, no zoom UI, no full-board re-render when the token moves.
- Exactly **60 perimeter spaces**; rolling two independently generated 1–6 dice animates both, then settles on matching face values. Movement does **not** create a second token.
- Central **six landmarks x five upgrades = 30 builds** in each world. Level 0 is an empty lot. Every purchase grows the corresponding unique 3D silhouette, adds a floor and visual details with a construction pop. Levels persist.
- Landmark names: **Sharko Surf Shack**, **Crocwing Flight Academy**, **Prima Mocha Café**, **Viral Video Tower**, **Meme Factory**, **Algorithm Palace**.
- Coin cost: `500 + current landmark level*750 + era*350`, aligning with the web economy. Purchase rewards, six-upgrade era rewards and board completion match the current browser rules.
- GO lap rewards, coins, cards, hype, shields, strikes, heist **payouts**, and randomized event payouts are playable. Unlike the existing web version, **interactive choice overlays for heists/events are not yet ported**; these currently resolve via simple random outcomes.
- Dice energy, five-minute regeneration, x1/x2/x5/x10 multipliers, world progression, offline local persistence, duplicate-card coin rewards, responsive screen layout and safe area margins.
- **WebGL legacy import**: if a Unity save does not exist, read a `memeopoly-save-v2` backup directly from same-origin localStorage without deleting it. Save import brings across position, money, cards, 6 building levels and core currencies. It is **not** a cross-device sync or native Android/iOS importer.

## Important: verification / limitation

This commit supplies Unity **source**, not a compiled APK/IPA/WebGL build. CI can validate source structure, but **a Unity Editor, license/build setup and device runtime are required** for shader, play-mode, touch, frame-rate and visual QA. No Unity runtime test has been executed by ChatGPT's current environment. A desktop or device preview must be inspected before declaring the game production-ready.

## Controls and design targets

- **One board on one screen**; centre city always visible at once; no "grid of cards" presentation.
- Building touch targets are invisible world-space hit volumes. Visible level states 0/1/2/3/4/5 are constructed in Unity, never just the level badge changing.
- One original shark-creator token, not an emoji placeholder; 3D dice pips correspond to outcomes, no "always lands on 1".
- The town is built from primitive meshes/materials as an **initial art pass**. Replace these with authored meshes, materials, animation clips, VFX and sound for target Monopoly GO production polish **without copying their protected characters or assets**.
- `MemeopolyRules` is intentionally independent of visuals. Use it as authoritative gameplay logic.

## Next production passes

1. In Editor, test live on phone and adjust composition, readability, colors, lighting and camera to screen size.
2. Swap procedural landmark geometry with six bespoke illustrated 3D assets and use construction skeleton/particle VFX for all six build stages.
3. Restore full web interaction parity: heist pick-your-vault choices, rival screens/raids, cards/stickers, contracts, daily missions, cosmetic selection, sounds.
4. Profile Android/iOS, export WebGL build to a separate build output, then connect real accounts/backend only **after** gameplay and art pass are approved.

## Unity QA checklist

- Load a fresh save; 30 dice, 5000 coins, 0/30 upgrade counter and token at space 0.
- Roll 20+ times. Die mesh top values must match the HUD, the move count and destination. No board flash.
- Spend dice energy at x1, x2, x5, x10; insufficient balance must block rolls.
- Tap each of the six plots and upgrade through stages 0–5, verify geometry visibly builds with each purchase.
- Reopen the game: world, levels, position, coins, cards and last dice face must match.
- Complete 30/30 builds, collect one-time completion reward, select NEXT WORLD; persistent resources remain, buildings reset.
- Test a tall iPhone, small Android and landscape desktop. Board must remain wholly visible and UI must not overlap the safe area.
- Run Unity Test Runner EditMode tests in `Assets/Tests/Editor`.

## Project structure

`Assets/Scripts/MemeopolyRules.cs`: deterministic state, tile outcomes, economy and persistence.

`Assets/Scripts/MemeopolyCity.cs`: 3D board, geometry, six distinct building designs, stage animations, dice, shark token.

`Assets/Scripts/MemeopolyGame.cs`: Unity startup, touch, camera, HUD, dice/token animation, saving and WebGL import.

`Assets/Editor/MemeopolyProjectSetup.cs`: scene auto-creation on Unity import.

`Assets/Plugins/WebGL/MemeopolyLegacy.jslib`: same-origin web-save import.

This is the **first actual Unity buildable milestone**, not a claim that every existing web feature has been ported.