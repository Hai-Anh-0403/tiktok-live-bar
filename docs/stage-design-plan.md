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

## Original stage rollback

Apply the patch only on a development branch based on eff8b3f132647ecc9e2929e388a7e9f7b542f762. Git preserves the original scene and presentation scripts. No deployment, push, restart or remote configuration change is authorized.
