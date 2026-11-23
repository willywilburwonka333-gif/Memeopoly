# Memeopoly Unity — major playable build candidate

This is an **actual Unity 6 project**, NOT the older PWA.

## Build on the phone through Unity Cloud

Unity Cloud → Memeopoly → Build Automation → Build History → Build.
Choose **Memeopoly WebGL Test**, branch **main**, project subfolder `unity`,
Unity editor **6000.0.60f1**, Windows Micro.

Check the build's **Last commit** against the most recent GitHub `main` SHA. A new build
is required to run the new gameplay; the old cloud preview link continues to show its
old compiled version.

**NOTE:** GitHub CI does not have Unity Editor access, so passing CI means **source
and static checks only**, not confirmed compilation or gameplay. Please upload the
complete `.log` file if Unity Cloud reports a build error.

## Test matrix (the next big mobile playthrough)

1. Open fullscreen in Safari, rotate portrait/landscape; the **entire continuous
   60-space board** must be visible, with header, notification and dock not overlapping.
2. Tap **ROLL** ten times. Verify both actual 3D top dice faces agree with the
   text result, and the Sharko character walks precisely that number of tiles.
3. Tap each of six landmarks, and **BUILD**. Verify the highlighted plot grows
   across **five distinct stages**, with construction sparks and a temporary crane.
4. Landing on **RAID** opens **ALGORITHM HEIST** with three *actual* vault choices.
   Complete one, verify coin delta; close/reopen the browser during a pending
   heist and verify you can still finish it (you should not be able to skip it).
5. Landing on **ATTACK** opens a three-choice **CREATOR SHOWDOWN**. Choose a rival;
   check rewards, shield use and saved progress.
6. Tap **MENU** → **MISSIONS**. Three daily challenges should show progress,
   only enable a reward once its objective is reached, and never pay twice.
7. Tap **MENU** → **STICKER ALBUM**. Stickers are collected on CARD tiles,
   duplicates pay coins, and completing all eight triggers a one-time bonus.
8. Tap **MENU** → **CHARACTERS**. Switch between **SHARKO**, **CROCWING**
   and **VIRAL CAT**; verify the 3D token changes on the board and the selected
   skin stays after reloading.
9. Tap **MENU** → **SOUND ON/OFF**. Check that music and sounds start only after
   a user gesture and that mute persists after restarting.
10. Build all thirty construction stages to finish World 1, collect the world
    reward and advance. Verify the city colour palette changes by world.
11. Test on an iPhone for >10 minutes, watch heat, frame-rate, memory and any
    Safari reloads. WebGL is a preview; native iOS/Android builds are a separate
    release target.

## Major changes included in this build

- 3 original 3D character models, distinct landmarks / 5 stages, custom triangular
  roof meshes, glowing façades and an animated central monument.
- Procedurally generated 128px tile art/icons; shared materials reduce temporary
  allocations from repeated construction upgrades.
- Heists, rivals, daily missions, collectible album bonus, audio, settings and
  contextual menus. All locally saved without API keys, external services or ads.
- Dice motion now bounces and eases to the **real top-face result**, instead of
  abruptly snapping from a generic spinning animation.
- Native Unity ParticleSystem impact and construction effects, light mobile
  performance tuning, eight world colour palettes.
- Every C# source is checked for invalid backslashes / unbalanced structure
  by GitHub CI, plus new Unity EditMode NUnit tests for gameplay logic.

## Known limitations — do not confuse this with a finished store release

The 3D characters/buildings are **original authored procedural models**, not
professional rigged/hand-sculpted art. The economy and rival events are currently
offline single-player NPC simulations. Real multiplayer, advanced social features,
monetisation and professional artwork are not yet implemented. There are no
native iPhone (TestFlight) or Android packages yet. Performance and visuals
must be validated in Unity Cloud and on real hardware.
