# Nightclub stage design plan

Status: Implemented in development; Unity/Windows validation pending

## Goal

Modernize the existing stage and improve portrait readability while preserving working TikFinity gifts, player joins, rankings, music, GIF loading and keyboard controls.

## Implementation

1. Reuse the existing stage, DJ booth and side LED builders; add nested portal rails, a floor deck and podiums aligned with existing player coordinates. Keep external background and main character loading.
2. Extend the existing pulse controller to animate only registered equalizer bars with cached transforms, anchored at their lower edge. Keep other light pulses and the configured BPM source.
3. Harmonize floor and beam colors. Keep the number of real-time lights and gift effects unchanged.
4. Fit the HUD to a 540 × 960 reference frame; add a venue/status header, separate gift/Top cards and a banner below them. Preserve the entire event-handling and control flow.
5. Verify the bridge baseline, actual source diff, C# syntax, geometry and two desktop/mobile preview rounds. Package a patch for the operator's complete repository.

## Character refinement requested during implementation

6. Keep source art; disable lossy character texture compression and bump the existing asset postprocessor version so Unity reimports associated textures.
7. Add one shared, pure motion sampler for viewer sprites and the main GIF. Six bounded whole-body motifs crossfade on a 96-beat cycle. This shared sampler avoids duplicating formulas and can be tested without Unity. Preserve root walk/jump/spawn paths and gift scales; soften excessive flipbook acceleration.
8. Allow Unity's existing resolution command-line options to override the current forced 540 × 960 default. Keep the original window size when no explicit resolution was requested.
9. Update the preview with actual flipbook frames and sampler motion; rerun the final checks and two desktop/mobile rounds. Existing source images do not gain new detail and independent limb gestures require rigged art, outside this sprite refinement.

## Validation limits

This Mac has no Unity installation. Do not claim a Windows executable, Unity rendering, actual LIVE regression coverage or measured game FPS. The operator's Unity build and LIVE check remain release requirements.

## Post-implementation notes

The source retains the original sprite artwork. Six blended whole-body motions improve variety; independent arm and leg animation still requires rigged assets. The browser preview uses the original 475 main frames, all 13 viewer clips, exported C# geometry and sampled C# motion. Its two desktop/mobile stress rounds establish preview behavior only.

The browser disco sphere initially omitted the native rig's rotation and emission. Its correction adds emissive mirror facets and a halo, keeps the native 24-degree-per-second speed, and freezes on Pause. The regression check first reproduced the stationary sphere; two full browser rounds then cover the corrected light/spin behavior. Native Unity scripts remain unchanged in this follow-up.

## Character diversity and Top refinement plan

Status: Implemented in development; Unity/Windows release validation pending.

1. Reproduce mushroom-dominated selection and missing Top identity in the current preview. Preserve its disco correction and all existing controls.
2. Generate and inspect one transparent 4×4 sprite atlas: cat, robot, duck, astronaut; four frames per row. Add the selected PNG and Unity metadata only, with no raster editing scripts.
3. Update CharacterLibrary inside its existing selection/cache boundary: six weighted visual families, different-family changes, cached four-frame atlas rows, and a safe original-art fallback for absent or invalid sheets. Add a native Editor diversity check and actual-source CPU red/green verification. Raise only the atlas import size limit.
4. Reuse the existing browser sprite loader/motion. Start with eight mixed actors (four legacy/four new); bind Top identity to the three podium actors and add a focused Top view. Keep current buttons, simulated gift changes and 400-person cap. Move and raise the third podium/assigned slot so all three Top slots line up at the stage edge.
5. Review the actual source/artifact diff, run source build/test/lint/docs checks and two browser stress rounds on desktop/mobile. Update the source and preview ZIPs; native Unity/Windows remains a release requirement.

Rollback: revert this bounded source change and restore the previously packaged preview. No live machine, Bridge configuration, user media, deployment or push is part of this task.

User steering during implementation: preserve 50% total weight for the two legacy families; four new families share the other 50%. All three ranked actors must stand on podiums close to the stage. These refinements replace the initial equal-family/seven-actor proposal.

### Entrance refinement

User requests a polished fall from above for new people. Reuse existing PlayerActor spawn state and add a smooth 10-unit/1.8-second drop with a small landing bounce, squash and a short cyan ground pulse. Keep gift/identity/rank state and existing jump/walk interruptions. During entry the camera must anchor the assigned floor slot so it does not follow the falling actor upward. Browser joins use the same timing and must freeze on Pause; initial demo actors remain settled. Verify actual-source bounds/return/camera/one-shot behavior and rerun both desktop/mobile rounds, including repeated gifts and effect cleanup.

### Flame-jet refinement

User additionally requests stage fire fountains. Reuse GiftEffectManager/ParticleSystem for four brief warm-color jets behind Top, add them to existing fireworks/Party triggers without replacing current effects, cap native emission at 160 particles per jet with a three-second cooldown and scheduled cleanup. Preview gets an independent Phụt lửa button, shared sprite glow geometry/texture, a bounded particle list and pause/cleanup checks. Native and browser code remain separate renderers; run actual-source CPU checks, full source syntax, Bridge/package checks and two desktop/mobile rounds before packaging.

## Header removal

Post-implementation UI correction: remove the entire decorative venue header from the browser preview and default Unity LIVE HUD, including its brand, subtitle, connection badge and attached energy line. Keep card positions and existing alternate HUD/controls; refresh source/preview packages and the portrait illustration.

## Viewer welcome and ranked-wing refinement

Status: Implemented in development; native Unity/Windows validation pending.

Scope: add a top scrolling welcome for the existing `member` event and give the current Top 1/2/3 different wings. Reuse TikFinity normalization, Controller HUD, PlayerActor wing renderer/anchor and existing fire/ice/neon sprites. Viewer entry must not bypass the existing player join rules. The ticker is clipped at virtual y=12, height=40, 8 seconds per name, at most eight pending names with a 24-second expiry; reset clears it. Ranked wings override gift-focus/score variants for wings only; non-Top gift wings, titles, identity, podiums and actions remain.

Implementation order: verify member-to-HUD and rank-to-decoration paths; add actual-source failing checks; implement the two bounded native changes and matching local preview; exercise event floods, name safety, reset, rank swaps, focus and demotion in two CPU rounds; run two desktop/mobile browser rounds covering new and previous controls; review actual diff, update documentation hashes and source/preview artifacts. No new dependency or live-server/config change. Rollback is a revert of this scoped dev commit.

Post-implementation notes: Top wing placement uses the actor's feet plus bounce and 55% of visible height, since old sprites have centered pivots and new atlas sprites have bottom pivots. Ordinary gift wings retain their original placement. Review removed the unnecessary 32-grapheme name cut because Unity/Mono StringInfo differs from modern .NET; the ticker preserves received names within the existing Bridge boundary and clips the display region. Two actual-source CPU stress rounds and two desktop/mobile browser rounds cover welcome floods/expiry, name safety, all three wing tiers and rank changes alongside previous gift/entry/effect/camera behavior. These are development checks, not a native Unity/Windows release.

## Independent gift/Top board visibility

Status: Implemented in development; Unity/Windows validation pending.

Scope: reuse Controller's F3 master display switch and existing F1 panel, add two per-board visibility flags defaulting to visible, F4/F5 shortcuts and two shared control-panel buttons. Keep the welcome strip independent of the per-board flags and preserve incoming gift/snapshot/rank behavior while boards are hidden. The browser gets matching accessible buttons and uses only visible HUD panels when placing Top identities. No persistence, dependencies, configuration or event-contract changes.

Plan: verify existing controls/callers; write actual-source and browser failing checks; implement the bounded Controller/preview edits; run two stress rounds covering all visibility combinations, F3/F1, data updates while hidden and previous gift/Top/wing/entry/FX/camera behavior on desktop/mobile; review the diff; update documentation checks and source/preview artifacts. Rollback: revert this scoped dev commit. Production is unchanged.

Post-implementation notes: the original F3 master remains intact; F4/F5 only gate drawing, and the shared F1 panel also describes F6. Two source-based CPU rounds and two desktop/mobile browser rounds verified independent switches, hidden score updates and previous interactions.

## Expanded camera direction

Status: Implemented in development; Unity/Windows validation pending.

Scope: extend the existing eight native director shots to twelve with stage/DJ, ranked podium arc, crane and diagonal movement; correct the Top target to the current z=-4.15 podium row. Reuse BPM, crowd bounds and exponential camera damping; blend outgoing poses over two beats and frame director subjects for the actual aspect ratio. Focus/welcome/fireworks routing and floor anchors remain authoritative. F6 skips ahead outside focus; browser camera controls preview all twelve and an automatic director mode.

Plan: inspect all focus callers and shot timing; add failing source-based projection/continuity and browser checks; implement bounded camera profiles and matching preview; run two CPU and desktop/mobile stress rounds for twelve shots, transitions, focus priority, pause and existing gameplay; review actual diff, document new controls and refresh artifacts. No dependencies, data/schema or production changes. Rollback: revert this scoped dev commit together with its docs.

Post-implementation notes: subject-box framing includes the main performer at z=-4.1, the Top row at z=-4.15 and disco/crane space up to y=8.2; the framing margin is 0.9. The stage push-in starts farther away so aspect fitting still permits an approach in portrait. Existing shot 6 keeps its 4.5-bar duration. The preview consumes samples exported from the actual native profiles, with fixed representative crowd bounds. Two CPU and desktop/mobile rounds passed; native Unity rendering and a Windows executable remain unverified.

## Multiple music uploads and ordered playback

Status: Implemented in development; original filename-order behavior superseded by saved drag-and-drop ordering below; native audio validation pending.

Scope: extend the existing Nhạc Nền tab and upload endpoint. Select multiple MP3/WAV/OGG files, validate them before starting, upload sequentially with autoplay suppressed, then request playback once. Multiple uploads start the displayed playlist from the beginning; one upload preserves the old behavior of playing that file. Keep existing tracks and per-track playback. Order is case-insensitive filename A–Z, matching the existing native player for sanitized upload names; numeric prefixes provide custom order. No playlist schema or native player change is required.

Contract: POST /api/music/upload?autoplay=false stores/synchronizes without emitting music_reload; absent flag retains immediate playback. GET /api/music returns sorted names. Existing POST /api/music/play with an empty fileName reloads from index zero, and native MusicPlaylistPlayer automatically advances and wraps. Reject invalid extensions and normalized filename collisions before a batch; stop on upload/copy failure without starting the incomplete list. Do not remove existing files.

Plan: add failing isolated HTTP/WebSocket contract tests using temporary music folders; implement the minimal Bridge/DOM edits; verify bulk progress, duplicate-click protection, error recovery, old single/per-track behavior and playlist order in two desktop/mobile rounds; verify unchanged native auto-next logic with explicit boundary doubles; review actual changes, update documentation hashes and include Bridge sources/tests in the source package. Rollback: revert the scoped music commit; media files are not deleted.

## Drag-and-drop playlist and camera clearance

Status: Implemented in development; native Unity/audio validation pending.

Scope: add accessible desktop/touch playlist drag-and-drop with persistent metadata in both existing music folders; GET uses saved order, PUT validates a complete permutation and safely synchronizes playlist.json, and a music_order event refreshes the native queue without restarting the playing clip. Retain batch/single uploads, playback controls, audio files, fallback ordering and existing event contracts. No dependency or audio rename/delete.

Camera investigation: the textured room has fixed walls and a ceiling derived from the 941x1672 backdrop: side x=±12.3816, ceiling y=17.6. Portrait framing may move the camera outside those limits during retreat. Reproduce against real room planes, then expand only the decorative room about its fixed back-wall anchor to keep camera/near plane inside; preserve all director/focus paths, four faces, aspect ratio and chroma.

Plan: source-based failing tests for room crossings and ordering, minimal dev changes, two actual HTTP/UI and native-source CPU rounds, desktop/mobile rendered room checks, independent diff review and refreshed docs/source/preview artifacts. Rollback: revert this scoped commit; music/audio content remains intact.

## Original stage rollback

Apply the patch only on a development branch based on eff8b3f132647ecc9e2929e388a7e9f7b542f762. Git preserves the original scene and presentation scripts. No deployment, push, restart or remote configuration change is authorized.

Music development verification: six isolated HTTP/WebSocket cases and two full UI rounds across desktop/mobile/320px and dark/light preferences passed; 25 Bridge tests passed. Two actual-source CPU rounds confirmed existing next/wrap behavior. The optional autoplay flag preserves legacy single-upload behavior; native audio decoding remains unverified.

Post-implementation notes for drag-and-drop/clearance: ordering is saved without renaming audio; live queue refresh preserves the current clip. Empty refresh cancels pending loads safely, and failed rollback retains recovery metadata. Two HTTP/UI rounds passed (31 Node tests; desktop/mobile/320, dark/light), with two native-source playlist rounds. Clearance red tests reproduced room crossings; corrected native and browser rounds keep all four backdrop faces and every tested retreat clear. Review found no remaining P1/P2 issues. Unity rendering/audio remain unverified.
