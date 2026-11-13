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
- Viewer arrivals (`member`, including spectator-only events with `joinedNow=false`) show “Name đã vào bar” in a clipped 40-pixel strip at virtual y=12, centered with width min(viewport width minus 24, 680). Each name scrolls right-to-left for eight unscaled seconds. The strip hides when idle, while controls are open or when the F3 master HUD switch is off; hiding an individual gift/Top card keeps greetings visible. The former decorative brand header remains removed.
- The welcome queue holds at most eight pending arrivals, discards the oldest on overflow and expires pending items after 24 seconds. Reset clears both active and pending messages. Names fall back from nickname to uniqueId to “Khách mới” and replace control/whitespace characters with spaces. The ticker preserves the received name, reusing the existing Bridge limits of 80 UTF-16 code units for nickname and 64 for uniqueId. It does not add a second truncation, which avoids splitting flags or ZWJ emoji with older Unity/Mono StringInfo behavior. Rich text is disabled and the rendering region clips the scrolling text. This consumes the existing member event and does not change who may join the floor, create points or focus the camera.
- Gift copy describes effects; Top uses real accumulated gift data. The Rosa row describes the configured medal effect. Feed draws recent entries inside a bounded bottom region.
- Existing F1 controls, F2 chroma, F3 board toggle and F11 fullscreen remain available. Existing player creation, gift rules, donor accumulation, camera focus and fireworks logic are unchanged; the member event additionally feeds the welcome strip. Viewer body poses now continue moving in Idle and transition smoothly with action intensity; root spawn/jump/walk paths and gift growth/focus scale remain unchanged.
- F4 toggles the gift card and F5 the compact Top card independently. Both default to visible. Two buttons below both existing F1 tabs provide the same actions and display the next action in their labels. F3 retains its master behavior and preserves these per-card choices when restored. The enlarged control panel keeps old controls in their original positions. Visibility is session-only and does not write PlayerPrefs or configuration.
- Only card drawing is gated: gifts, donor scores, rank changes, podiums/wings, feed, cinematic focus and reset/snapshot processing continue while either card is hidden. The browser has independent card buttons with aria-controls/aria-pressed, a compact three-column mobile toolbar and label placement that reserves space only for visible HUD panels.

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

## Camera direction

- The existing native BPM director now cycles twelve profiles: establishing view, front/middle/back crowd sweeps, the current Top row, side dolly, backdrop reveal, opposite dolly, stage-performer push-in, Top arc, stage crane and opposite truck/orbit. The Top target follows the actual z=-4.15 podium row. The four added profiles retain a level horizon and use eased motion.
- A two-beat blend from the outgoing position/look target/FOV joins shots, including startup and the return from an event focus; existing exponential damping continues afterward. Director framing projects all eight corners of each intended subject box with a 0.9 framing margin for the actual camera aspect and moves backward along the viewing direction as needed. This applies only to director shots; existing gift, walk, fireworks and welcome focus paths retain their durations, priorities and floor anchors.
- F6 advances to the next director shot when no event focus is active. The browser provides a twelve-view button and an automatic director toggle, retains Xem Top and gift close-ups, and freezes position, target, FOV and shot timing on Pause.
- Browser camera poses are sampled from the actual native ConfigureDirectorShot method into camera-shots.json (two directions, 65 samples per shot) and fitted to the browser aspect. The exported source hash binds the data to its C# source. Its representative crowd bounds are fixed at x=-5.2..5.2, z=-4.15..3.4; native framing still uses live PlayerManager bounds. The browser remains a concept renderer, not a native camera/framing or Windows release test.

## Ranked wing themes

- The current Top 1 uses the existing gold/orange `Banners/wings-fire`, Top 2 the silver/cyan `wings-ice`, and Top 3 the violet/pink `wings-neon`. Rank takes priority over gift-focus and total-gift wing variants. Ordinary gift/VIP wings, banner/title variants, score calculation and podium assignment retain their existing rules.
- Base wing widths are clamped by tier: Top 1 2.55-2.8, Top 2 2.2-2.4, Top 3 1.85-2.05 world units. Small 2.5% horizontal and 1.2% vertical flutter uses the scaled game clock; it follows actor position, body rotation and visibility alpha behind the character. Ranked wings anchor above the feet independently of centered legacy sprites versus the bottom-pivot cast atlas. Changing character refreshes its decoration size; rank swaps transfer the theme and demotion restores the ordinary gift wing behavior. No new sprite asset, material, light or dependency is added to the native game.
- `TikTok Live Game > Verify Rank Wings` checks all three original resources, rank priority across gift thresholds and focus, descending bounded widths, sorting/alpha, changes of rank/character and demotion. The preview reuses exactly three wing sprites attached to the current ranked donors, including when a new donor overtakes third place at 400 actors. “Khách vào” simulates a viewer greeting without adding a dancer or score. Preview Pause freezes the welcome scroll and wings; reduced-motion preference displays the greeting statically.

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
<!-- docs-check: UnityProject/Assets/Scripts/TikTokGameController.cs a725209634f5fae2cb752d787e53c8910aeb54202cb46cc38550c0662a7f2300 -->
<!-- docs-check: UnityProject/Assets/Scripts/PlayerActor.cs 18f6130192025000d02d40398c7938092e1afd4e14b43b67d9d00a3c040189f7 -->
<!-- docs-check: UnityProject/Assets/Scripts/RuntimeBootstrap.cs aefb9b114492b7d2232d2c6dbf4541ecc42077723cc4c652d4f602c404c3b2e1 -->
<!-- docs-check: UnityProject/Assets/Scripts/CharacterMotion.cs 679a091f06cb31b62eeede12820fd585509371926d63502eb89d42ba12cd363d -->
<!-- docs-check: UnityProject/Assets/Scripts/CharacterLibrary.cs 0583a1c9149da2e4e45497cf76d245e3505747e83629b485625341fd2f35e5f8 -->
<!-- docs-check: UnityProject/Assets/Scripts/ClubCameraController.cs 423759a5f1da21dcc3a5c0fb537e78084e341e68ff51bbc2b3c460a6863ee449 -->
<!-- docs-check: UnityProject/Assets/Scripts/PlayerManager.cs 584fb5de6c5eb208d6dc45b960200b8e7aa5dfb33c8216b71a4ec474a274b891 -->
<!-- docs-check: UnityProject/Assets/Scripts/GiftEffectManager.cs 0942227bdca28aaf68c44b975f59868d2547c679cb6a7d8fafcae17b45a1050a -->
<!-- docs-check: UnityProject/Assets/Editor/CharacterTextureImporter.cs 4727945070f742451bd9c9619a46be647c7d35c8b3ecda607f471bf8e7b277d2 -->
<!-- docs-check: UnityProject/Assets/Editor/CharacterMotionChecks.cs 79f35260b03698d1532944e0e3a29bf27e84e897655c9cc5ba352019b3122a96 -->
<!-- docs-check: UnityProject/Assets/Editor/CharacterLibraryChecks.cs a1e3f955dff269f8848a360c36762c59391d156f4923fe69e762eb705003ee22 -->
<!-- docs-check: UnityProject/Assets/Resources/Characters/club_cast.png 8e3e695c75c4abf259258df064a3aca6d9958ac89b306f54be8c7347f30dbe91 -->
