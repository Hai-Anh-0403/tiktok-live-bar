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

- A 540 × 960 minimum reference frame fits portrait and desktop viewports. The decorative venue/title/status strip has been removed from the default LIVE HUD and browser preview at the operator's request. Gift/Top cards remain at y=72; the gift banner stays above the feed with its bottom at virtual height minus 250 (at least y=420 for its top).
- Existing controls/alternate diagnostic HUD and Party energy behavior remain available; this removal changes only the decorative default header.
- Gift copy describes effects; Top uses real accumulated gift data. The Rosa row describes the configured medal effect. Feed draws recent entries inside a bounded bottom region.
- Existing F1 controls, F2 chroma, F3 board toggle and F11 fullscreen remain available. Event handling, player creation, gift rules, donor accumulation, camera focus and fireworks logic are unchanged. Viewer body poses now continue moving in Idle and transition smoothly with action intensity; root spawn/jump/walk paths and gift growth/focus scale remain unchanged.

## Character clarity and motion

- Original art is preserved: viewer sprites are about 240-256 px; the main GIF frames are 416 x 380. Turning off lossy texture compression preserves source edges and alpha; it does not invent missing HD detail.
- CharacterTextureImporter is versioned at 2 so associated textures reimport when the script is recompiled. Existing character textures retain max512; only club_cast.png uses max2048 and NPOT None. No new textures are created per animation frame. Uncompressed textures increase GPU memory usage; verify the target PC before release.
- CharacterMotion is a shared immutable, allocation-free scalar pose sampler. Six whole-body motifs rotate every 16 beats in a 96-beat cycle with a three-beat smooth transition. Per-character phase avoids synchronized identical poses. Invalid numeric inputs are neutral and intensity is clamped.
- Viewer motion intensity eases between Idle, Dance, Jump, Walk and Celebrate. Flipbook playback is eased within 1.05-1.7x instead of abrupt 1.5/2.7x. Sprite lean uses a rotated-bounds ground offset, and names/avatars retain their camera-facing layout.
- The main GIF keeps its frames, 25 FPS timing and loaders, with only gentle secondary sway/lift/lean. This is whole-sprite animation; the assets do not contain independent limb rigs.
- Unity menu: TikTok Live Game > Verify Character Motion runs reusable bounds, six-motif, transition, invalid-input and determinism checks. This is an Editor verification tool, not a user-facing runtime feature.
- Full HD can be requested through Unity's native `-screen-width 1080 -screen-height 1920 -screen-fullscreen 0` arguments. No explicit resolution keeps the original default. Larger output requires a suitable monitor/GPU and does not add art resolution.

## Character diversity and current Top behavior

- All 13 original clips remain. They represent 12 mushroom-face variants and one panda. CharacterLibrary now chooses visual families with weights 2:2:1:1:1:1: mushroom and panda each 25%, four new families each 12.5%. A change excludes the current family and renormalizes remaining weights, so it cannot merely switch to another mushroom-face action. This is a selection probability, not a guaranteed ratio for a short random native session.
- Four original chibi characters were generated for this project: orange cat, cyan robot, yellow duck and astronaut. Resources/Characters/club_cast.png is a transparent 1254×1254 PNG, four columns by four rows in that order. Sixteen cached FullRect sprites share one texture; fractional 313.5-pixel cells preserve the source without raster resizing. Sprite pivots remain bottom-center at 100 PPU. The new clips use 6 FPS before existing motion playback scaling; measured transparent bottom padding is compensated per character.
- If the atlas is missing or malformed, the library logs once and falls back to the original mushroom/panda families. The original Resources must still exist in the complete installation. Runtime caches are populated once; replacing the atlas requires a fresh game run.
- Entry still selects a random character. Gift action `change` (configured TikTok gift) and supported chat commands such as “đổi nv” still change the existing player's character, preserving identity, score and rank. The default native filler remains four NPCs; it was the browser demo that originally started with 18 actors.
- Real Top uses gift-score totals received by the tool, including snapshot data; it is not a separate query of TikTok's global leaderboard. Top 1 is at (-2.55,0.65,-4.15), Top 2 at (2.55,0.65,-4.15), Top 3 at (-4.6,0.55,-4.15). All three podiums are now at the stage edge; the third actor's feet match its raised rim. Existing names, avatars, TOP labels, rank scaling and the upper-right donor board remain unchanged. Ranked players still retain existing gift actions, including temporary walks before returning to their assigned podium.
- The preview starts with eight actors: four legacy and four new. It binds podium identities/ranks/simulated scores to the same donor objects as its board. “Xem Top” frames the podium actors and dims other actors; labels choose free slots between HUD cards and controls, including during camera transitions. “Đổi nhân vật” demonstrates a different family while preserving the selected identity and points. These are local simulation controls, not LIVE commands. Repeated Rose demos reuse the same guest and accumulate that guest's score; passing a ranked score moves that guest to the corresponding podium.
- Native verification menu: TikTok Live Game > Verify Character Diversity checks all original clips, six-family balance, different-family changes, atlas bounds/pivots and cache identity, restoring Editor random state afterward. Actual-source CPU tests and browser checks do not replace importing and running the Windows game in Unity.

## New-player drop and landing

- Reuses PlayerActor's existing one-time spawn state: 10-unit drop over 1.8 seconds. The first 72% uses smoothstep height interpolation, followed by a small damped bounce. A brief stretch/squash replaces ambient whole-body motion during entry, then normal dancing resumes.
- A real new player emits one expanding cyan ground glow at first contact. The existing DiscoBallRig quad/glow helper and shared material/mesh are reused; the effect lasts 0.55 seconds and has scheduled destruction even if the actor disappears. NPCs retain their existing drop but do not emit this glow. No new lights, audio or dependencies.
- PlayerActor.FocusPosition exposes the assigned floor/podium during pending/active entry and the actual position afterward. Camera focus and PlayerManager crowd bounds use this anchor, so both individual and crowd-welcome shots stay aimed at the floor while people fall. All camera focus entry points keep the matching actor reference; camera release clears it. ReturnToAssignedSlot leaves an in-progress entry intact. Existing jump/walk actions still cancel entry and follow their existing paths.
- Repeated gifts, focusing or changing the same actor do not restart the drop or pulse. The browser simulates this for new Rose guests and added crowd actors, freezes the entire entry/effect clock on Pause, and disposes completed landing materials. Initial demo actors start settled.
- CPU tests exercise actual extracted source and dependencies represented by doubles: profile bounds/settling, interrupts, one-shot glow scheduling, return guard, camera anchoring and two stress rounds. Browser desktop/mobile checks cover visible entry, pause/resume, landing, repeated gifts and cleanup. Native rendering, effect visibility and Windows gameplay still require Unity validation.

## Stage flame jets

- GiftEffectManager.PlayFlameJets adds four short gold/orange ParticleSystems at x=-5,-1.6,1.6,5; y=0.8; z=-5.6, on the stage deck behind Top. Each emitter is stopped before configuration, uses the existing SimpleParticle shader (Sprites/Default fallback), and shares a cached material. No additional lights or audio.
- Emission lasts 0.9 seconds, particle lifetime 0.35-0.55 seconds, with warm fade/shrink and max160 particles per emitter. A three-second cooldown limits new jets to four simultaneous emitters; scheduled cleanup removes each emitter before the next allowed wave.
- LaunchFireworks still starts the original firework sound, aerial sequence and cold-fountain sequence; it now also requests the flame jets. Existing gift thresholds, event actions and Party energy rules are unchanged. The new C# method does not add a WebSocket command.
- The preview provides a Phụt lửa button and combines jets with its fireworks demonstration. A reusable 640-point buffer and four base glows limit allocations; each wave stops by 1.5 seconds and obeys the same three-second cooldown. Pause freezes particle positions/colors and timing. It is a separate concept renderer, not native particle-rendering evidence.

## IPC, configuration and dependencies

No WebSocket schema, API, TikFinity setting, player data or package dependency changes. Existing Unity native resolution command-line options are honored when supplied; otherwise the original 540 x 960 window default remains. The launcher continues to run TikTokBridge/server.js. Existing configuration and personal media must not be overwritten.

## Verification and release

The Mac development machine has no Unity installation. C# syntax/geometry/animation checks and a browser concept preview cannot establish Unity rendering, a Windows build, real LIVE behavior, device FPS or absence of all regressions. Keep the old executable while validating a development build.

Apply only the source patch to a development branch of the complete base repository. Build using the existing Unity version and build.bat; run the existing capture harness plus a real session check for Rose/join, Top, gift effects, music, GIF, camera and keyboard controls before replacing the live executable. The sparse development checkout is not a full installation archive.

### Browser disco correction

The packaged concept preview replaces its stationary, unlit metallic sphere with an emissive core, instanced mirror tiles and a soft cyan halo. Rotation follows the existing Unity rig's 24 degrees per second, using the preview simulation clock so Pause freezes spin and brightness. The correction adds no real-time lights or dependencies. This fixes the browser representation; Unity already calls DiscoBallRig.Tick and is unchanged by this correction.

The preview ZIP includes its browser source and regression checks. Two desktop/mobile rounds verify spin, emission, visible mirror facets and pause together with the existing simulated gift, crowd, motion and layout checks. These checks do not establish native Unity rendering or Windows FPS.

## Documentation checks

Run `node scripts/check-docs.js`. Review behavior notes before refreshing markers.

<!-- docs-check: UnityProject/Assets/Scripts/ClubSceneBuilder.cs 6c004bc14ed4b40247f97e2f8b18ffe6d7c858d82d2ae822a3e6d72d28eacc15 -->
<!-- docs-check: UnityProject/Assets/Scripts/ClubPulseController.cs f0a3aa937cc6dfc02c78da8db446fe5cfdb2d92f5bc3750a1367ec71586fb3bb -->
<!-- docs-check: UnityProject/Assets/Scripts/FloorLightShow.cs d86f474af19c0c9a6aa78e9f00f6d7af0000ae71f288991cba50276f39a0199f -->
<!-- docs-check: UnityProject/Assets/Scripts/StageExtras.cs 3c6b1d384fb4f958f7d472430164bac1bc1b5f142eaa2c49aebc407c6c841885 -->
<!-- docs-check: UnityProject/Assets/Scripts/TikTokGameController.cs f836e191ae5b7ca79f240ca8e87146e707a5b63601615c33bc32696fcd144d25 -->
<!-- docs-check: UnityProject/Assets/Scripts/PlayerActor.cs abd78a3f9154e2fa5cd6e2230abdb13e50f94caf77be0ca43e389d03af1cf417 -->
<!-- docs-check: UnityProject/Assets/Scripts/RuntimeBootstrap.cs aefb9b114492b7d2232d2c6dbf4541ecc42077723cc4c652d4f602c404c3b2e1 -->
<!-- docs-check: UnityProject/Assets/Scripts/CharacterMotion.cs 679a091f06cb31b62eeede12820fd585509371926d63502eb89d42ba12cd363d -->
<!-- docs-check: UnityProject/Assets/Scripts/CharacterLibrary.cs 0583a1c9149da2e4e45497cf76d245e3505747e83629b485625341fd2f35e5f8 -->
<!-- docs-check: UnityProject/Assets/Scripts/ClubCameraController.cs c075e1886bbe20761a3cd75b5f158d2663051b93ad1a3e8951b1a81564e6c10e -->
<!-- docs-check: UnityProject/Assets/Scripts/PlayerManager.cs 584fb5de6c5eb208d6dc45b960200b8e7aa5dfb33c8216b71a4ec474a274b891 -->
<!-- docs-check: UnityProject/Assets/Scripts/GiftEffectManager.cs 0942227bdca28aaf68c44b975f59868d2547c679cb6a7d8fafcae17b45a1050a -->
<!-- docs-check: UnityProject/Assets/Editor/CharacterTextureImporter.cs 4727945070f742451bd9c9619a46be647c7d35c8b3ecda607f471bf8e7b277d2 -->
<!-- docs-check: UnityProject/Assets/Editor/CharacterMotionChecks.cs 79f35260b03698d1532944e0e3a29bf27e84e897655c9cc5ba352019b3122a96 -->
<!-- docs-check: UnityProject/Assets/Editor/CharacterLibraryChecks.cs b34c38b4adcc4aeb4149461666de6ac0e8f0cdec72e6ca0ab462cf24f94cdaa4 -->
<!-- docs-check: UnityProject/Assets/Resources/Characters/club_cast.png 8e3e695c75c4abf259258df064a3aca6d9958ac89b306f54be8c7347f30dbe91 -->
