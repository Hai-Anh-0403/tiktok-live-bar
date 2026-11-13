# Project-specific instructions

- This checkout develops the Unity stage of `Hai-Anh-0403/tiktok-live-bar`; it is separate from the operator's running Windows build.
- Follow `DESIGN.md` for stage and HUD changes.
- The launcher runs `TikTokBridge/server.js`; preserve its TikFinity configuration and gift contracts.
- Baseline validation: `npm test --prefix TikTokBridge` (19 tests).
- Run the native Unity menus `TikTok Live Game > Verify Character Motion` and `Verify Character Diversity`; pure samplers can also be compiled without Unity for CPU checks. Validate entry, all three stage podiums and the new jets in the native build.
- Windows build: Unity version in `UnityProject/ProjectSettings/ProjectVersion.txt`, then `build.bat` or the existing Unity build menu. A browser preview is not Windows build evidence.
- Do not copy this sparse development checkout over an installation: external media and Unity Resources remain in the operator's complete repository.
