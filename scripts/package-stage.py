"""Package the scoped source update, not a Windows game executable."""

import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import zipfile


ROOT = Path(__file__).resolve().parent.parent
BASE = "eff8b3f132647ecc9e2929e388a7e9f7b542f762"
SCRIPTS = [
    "UnityProject/Assets/Scripts/" + name
    for name in [
        "ClubSceneBuilder.cs", "ClubPulseController.cs", "FloorLightShow.cs",
        "StageExtras.cs", "TikTokGameController.cs", "PlayerActor.cs",
        "RuntimeBootstrap.cs", "CharacterMotion.cs", "CharacterLibrary.cs", "ClubCameraController.cs", "PlayerManager.cs", "GiftEffectManager.cs", "MusicPlaylistPlayer.cs",
    ]
] + [
    "UnityProject/Assets/Scripts/CharacterMotion.cs.meta",
    "UnityProject/Assets/Editor/CharacterTextureImporter.cs",
    "UnityProject/Assets/Editor/CharacterMotionChecks.cs",
    "UnityProject/Assets/Editor/CharacterMotionChecks.cs.meta",
    "UnityProject/Assets/Editor/CharacterLibraryChecks.cs",
    "UnityProject/Assets/Editor/CharacterLibraryChecks.cs.meta",
    "UnityProject/Assets/Resources/Characters/club_cast.png",
    "UnityProject/Assets/Resources/Characters/club_cast.png.meta",
    "TikTokBridge/server.js",
    "TikTokBridge/public/control.js",
    "TikTokBridge/public/control.html",
    "TikTokBridge/public/control.css",
    "TikTokBridge/test/music-upload.test.js",
    ".gitignore",
]
FILES = SCRIPTS + [
    "DESIGN.md", "SYSTEM_DOCUMENTATION.md", "Makefile", "LICENSE",
    "scripts/check-docs.js", "scripts/package-stage.py", "docs/stage-design-plan.md",
]
INSTALL = """# Nightclub stage source update

This is a development source update, not an executable or complete installation.
Base: Hai-Anh-0403/tiktok-live-bar @ eff8b3f132647ecc9e2929e388a7e9f7b542f762.

1. Keep the running LIVE installation and executable intact. Use a development branch or separate complete repository with the same source base.
2. Preferred: run `git apply --check nightclub-stage.patch` in that repository, then `git apply nightclub-stage.patch`. If the check fails, stop and compare the changes; do not force it.
3. Alternatively, compare and copy the included Unity scripts and their .meta files, plus the new Assets/Resources/Characters/club_cast.png atlas and its .meta. The atlas is required for the four new character families. Merge the included TikTokBridge server.js/public control files and music-upload test for batch music upload; do not replace the whole Bridge, config, .env, personal media, or UnityProject folder. The streaming upload API and control page must be updated together.
4. DESIGN.md and SYSTEM_DOCUMENTATION.md describe the changes and verification limits. The scripts/Makefile files are optional source-package verification helpers.
5. Open UnityProject with the Unity version in ProjectSettings/ProjectVersion.txt. Let the versioned character importer refresh textures, then run TikTok Live Game > Verify Character Motion, Verify Character Diversity and Verify Rank Wings before building with the existing build.bat. The original Banners/wings-fire, wings-ice and wings-neon resources must remain in the complete repository. This development machine did not run Unity or produce a tested Windows executable.
6. In the development build verify a new player falls from above, lands on the assigned floor/podium and emits one short ground pulse; repeated gifts do not restart entry and camera stays aimed at the floor during the fall. Check the top scrolling welcome from a real member/viewer event (including a spectator who has not joined the floor), reset/queue expiry, distinct Top wing tiers through rank swaps, high gifts, focus and demotion, character changes, Top podium updates, all other gifts, music, GIF, camera and chroma. F1 opens controls with independent gift/Top buttons; F4 toggles gifts, F5 toggles Top, F3 masks/restores the HUD while preserving these choices. Verify data still updates while hidden. Check all twelve director angles, F6 next angle, smooth transitions, portrait Top/DJ/crane framing and event focus priority, plus F2/F11. Test portrait/desktop and a busy crowd before replacing the live build.
7. In the control page Nhạc Nền tab, upload multiple MP3/WAV/OGG files and verify only one playback request after the batch. Drag the handle (or use arrow buttons/keys) to reorder; verify the order persists after reload, new tracks append and the current song continues while the next song follows the saved order. Update Bridge/control and native source together for music_order handling. Test save/upload failures, the start-from-beginning button, single/per-track compatibility and native next-track/wrap. Keep music folders and playlist.json; do not copy a development playlist over user data. Install existing locked Bridge dependencies with npm ci before tests; no new runtime dependency is added.

The browser preview is a concept renderer with simulated data. It does not connect to TikTok and does not establish Windows rendering or FPS.

The original 540 x 960 default is preserved. To request a larger render size, Unity's native command-line options are now honored, for example:
OngChuMMO_Live.exe -screen-width 1080 -screen-height 1920 -screen-fullscreen 0
Use a resolution suitable for the monitor and GPU. Original sprites remain 240-256 px (main GIF 416 x 380); new cast atlas cells are 313.5 px. Render resolution does not create missing art detail. Lossless character textures consume more GPU memory than compressed textures.
"""


def build_archive(output):
    ancestry = subprocess.run(
        ["git", "merge-base", "--is-ancestor", BASE, "HEAD"],
        cwd=ROOT, capture_output=True,
    )
    if ancestry.returncode != 0:
        raise RuntimeError("Source base is not an ancestor; review the package before building")
    patch = subprocess.check_output(
        ["git", "diff", "--binary", BASE, "--", *SCRIPTS], cwd=ROOT
    )
    if not patch:
        raise RuntimeError("No stage changes to package")
    output.parent.mkdir(parents=True, exist_ok=True)
    manifest = {
        "base": BASE,
        "artifact": "development-source-update",
        "windowsBuildVerified": False,
        "sha256": {
            name: hashlib.sha256((ROOT / name).read_bytes()).hexdigest()
            for name in FILES
        },
    }
    with zipfile.ZipFile(output, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        for name in FILES:
            archive.write(ROOT / name, name)
        archive.writestr("nightclub-stage.patch", patch)
        archive.writestr("INSTALL.md", INSTALL)
        archive.writestr("manifest.json", json.dumps(manifest, indent=2) + "\n")
    return manifest


def verify_archive(output, manifest):
    with zipfile.ZipFile(output) as archive:
        assert archive.testzip() is None, "Corrupt archive"
        assert set(archive.namelist()) == set(FILES + [
            "nightclub-stage.patch", "INSTALL.md", "manifest.json"
        ]), "Unexpected package contents"
        for name, digest in manifest["sha256"].items():
            assert hashlib.sha256(archive.read(name)).hexdigest() == digest, name
        assert json.loads(archive.read("manifest.json"))["windowsBuildVerified"] is False
        patch = archive.read("nightclub-stage.patch")
        # Confirm that the included patch describes exactly the files being shipped.
        subprocess.run(
            ["git", "apply", "--reverse", "--check", "-"], cwd=ROOT,
            input=patch, check=True, capture_output=True,
        )


if __name__ == "__main__":
    if sys.argv[1:] == ["--check"]:
        with tempfile.TemporaryDirectory(prefix="nightclub-package-check-") as directory:
            output = Path(directory) / "stage.zip"
            manifest = build_archive(output)
            verify_archive(output, manifest)
        print("Source package checks: PASS (allowlist, hashes, patch applicability)")
    elif not sys.argv[1:]:
        output = ROOT / "Build" / "nightclub-stage-source.zip"
        manifest = build_archive(output)
        verify_archive(output, manifest)
        print(f"Source update: {output}")
        print("Windows executable: not built; Unity validation required")
        print("Build succeeded")
    else:
        raise SystemExit("Usage: python3 -B scripts/package-stage.py [--check]")
