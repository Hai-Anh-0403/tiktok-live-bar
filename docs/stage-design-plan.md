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

## Rollback

Apply the patch only on a development branch based on eff8b3f132647ecc9e2929e388a7e9f7b542f762. Git preserves the original scene and presentation scripts. No deployment, push, restart or remote configuration change is authorized.
