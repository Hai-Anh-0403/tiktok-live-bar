# System documentation — Nightclub stage

Status: Development preview; Windows/Unity release validation pending.

Base: Hai-Anh-0403/tiktok-live-bar @ eff8b3f132647ecc9e2929e388a7e9f7b542f762.
Version: existing application 1.0.0; this change does not increase the version.

## Stage presentation

- Reuses the raised stage, DJ booth, turntables, speakers, side LED arrays and existing material factories.
- Adds three nested LED portal frames, a dark dance deck with fine grid rails, and plinths aligned with existing main character/Top 1/2/3 coordinates. A dark rear backplate separates the nested portals from the existing external background, which remains visible around the set.
- The 25 × 20 floor extends beyond the decorative LED rectangle to support the existing walk circuits from the outer crowd slots without changing their paths.
- Preserves the external backdrop loader, main GIF loader, disco ball and existing moving beams. Ambient/wash, floor and beam colors use the DESIGN.md palette.
- Equalizer bars register once in ClubPulseController. Every frame adjusts cached emission and bar transforms with their bottom edges fixed. No new objects, textures or materials are allocated by the added animation loop.
- Lighting follows the existing configured BPM (default 128); it does not analyze music audio. This change adds no real-time lights, strobe effects or post-processing dependencies.

## HUD

- A 540 × 960 minimum reference frame fits portrait and desktop viewports. Header at y=12, gift/Top cards at y=72, gift banner anchored above the feed with its bottom at virtual height minus 250 (at least y=420 for its top).
- Header connection label reports the Unity-to-Bridge socket only; it does not certify the TikTok LIVE state.
- Gift copy describes effects; Top uses real accumulated gift data. The Rosa row describes the configured medal effect. Feed draws recent entries inside a bounded bottom region.
- Existing F1 controls, F2 chroma, F3 board toggle and F11 fullscreen remain available. Event handling, player creation, gift rules, donor accumulation, camera focus and fireworks logic are unchanged. Viewer body poses now continue moving in Idle and transition smoothly with action intensity; root spawn/jump/walk paths and gift growth/focus scale remain unchanged.

## Character clarity and motion

- Original art is preserved: viewer sprites are about 240-256 px; the main GIF frames are 416 x 380. Turning off lossy texture compression preserves source edges and alpha; it does not invent missing HD detail.
- CharacterTextureImporter is versioned at 1 so associated textures reimport when the script is recompiled. The source maximum size remains 512 for characters and no new textures are created per animation frame. Uncompressed textures increase GPU memory usage; verify the target PC before release.
- CharacterMotion is a shared immutable, allocation-free scalar pose sampler. Six whole-body motifs rotate every 16 beats in a 96-beat cycle with a three-beat smooth transition. Per-character phase avoids synchronized identical poses. Invalid numeric inputs are neutral and intensity is clamped.
- Viewer motion intensity eases between Idle, Dance, Jump, Walk and Celebrate. Flipbook playback is eased within 1.05-1.7x instead of abrupt 1.5/2.7x. Sprite lean uses a rotated-bounds ground offset, and names/avatars retain their camera-facing layout.
- The main GIF keeps its frames, 25 FPS timing and loaders, with only gentle secondary sway/lift/lean. This is whole-sprite animation; the assets do not contain independent limb rigs.
- Unity menu: TikTok Live Game > Verify Character Motion runs reusable bounds, six-motif, transition, invalid-input and determinism checks. This is an Editor verification tool, not a user-facing runtime feature.
- Full HD can be requested through Unity's native `-screen-width 1080 -screen-height 1920 -screen-fullscreen 0` arguments. No explicit resolution keeps the original default. Larger output requires a suitable monitor/GPU and does not add art resolution.

## IPC, configuration and dependencies

No WebSocket schema, API, TikFinity setting, player data or package dependency changes. Existing Unity native resolution command-line options are honored when supplied; otherwise the original 540 x 960 window default remains. The launcher continues to run TikTokBridge/server.js. Existing configuration and personal media must not be overwritten.

## Verification and release

The Mac development machine has no Unity installation. C# syntax/geometry/animation checks and a browser concept preview cannot establish Unity rendering, a Windows build, real LIVE behavior, device FPS or absence of all regressions. Keep the old executable while validating a development build.

Apply only the source patch to a development branch of the complete base repository. Build using the existing Unity version and build.bat; run the existing capture harness plus a real session check for Rose/join, Top, gift effects, music, GIF, camera and keyboard controls before replacing the live executable. The sparse development checkout is not a full installation archive.

## Documentation checks

Run `node scripts/check-docs.js`. Review behavior notes before refreshing markers.

<!-- docs-check: UnityProject/Assets/Scripts/ClubSceneBuilder.cs 09320ff6540f54b878771c56e442c1838f3e3dbd7acab243f8abef6354e35d14 -->
<!-- docs-check: UnityProject/Assets/Scripts/ClubPulseController.cs f0a3aa937cc6dfc02c78da8db446fe5cfdb2d92f5bc3750a1367ec71586fb3bb -->
<!-- docs-check: UnityProject/Assets/Scripts/FloorLightShow.cs d86f474af19c0c9a6aa78e9f00f6d7af0000ae71f288991cba50276f39a0199f -->
<!-- docs-check: UnityProject/Assets/Scripts/StageExtras.cs 3c6b1d384fb4f958f7d472430164bac1bc1b5f142eaa2c49aebc407c6c841885 -->
<!-- docs-check: UnityProject/Assets/Scripts/TikTokGameController.cs 8eeb2b1c60a1a3e0d1ac1e0bb897ebf34905a83f79ff1698e11d2bc14ee24d6e -->
<!-- docs-check: UnityProject/Assets/Scripts/PlayerActor.cs a91aeb30e30fcfc615a727f3208fbf2ddd5e6c7797480723d0752d706ea06d6b -->
<!-- docs-check: UnityProject/Assets/Scripts/RuntimeBootstrap.cs aefb9b114492b7d2232d2c6dbf4541ecc42077723cc4c652d4f602c404c3b2e1 -->
<!-- docs-check: UnityProject/Assets/Scripts/CharacterMotion.cs 679a091f06cb31b62eeede12820fd585509371926d63502eb89d42ba12cd363d -->
<!-- docs-check: UnityProject/Assets/Editor/CharacterTextureImporter.cs c526b149d97e8b0e1bdec9590a1671c4bee122a39c99bcb4df26d93a764551f6 -->
<!-- docs-check: UnityProject/Assets/Editor/CharacterMotionChecks.cs 79f35260b03698d1532944e0e3a29bf27e84e897655c9cc5ba352019b3122a96 -->
