'use strict';

const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const { createHash } = require('node:crypto');
const path = require('node:path');

const root = path.resolve(__dirname, '..');
const documentation = readFileSync(path.join(root, 'SYSTEM_DOCUMENTATION.md'), 'utf8');
const files = ['ClubSceneBuilder.cs', 'ClubPulseController.cs', 'FloorLightShow.cs', 'StageExtras.cs', 'TikTokGameController.cs', 'PlayerActor.cs', 'RuntimeBootstrap.cs', 'CharacterMotion.cs', 'CharacterLibrary.cs', 'ClubCameraController.cs', 'PlayerManager.cs', 'GiftEffectManager.cs']
    .map(name => `UnityProject/Assets/Scripts/${name}`)
    .concat(['UnityProject/Assets/Editor/CharacterTextureImporter.cs', 'UnityProject/Assets/Editor/CharacterMotionChecks.cs', 'UnityProject/Assets/Editor/CharacterLibraryChecks.cs', 'UnityProject/Assets/Resources/Characters/club_cast.png']);
const markers = [...documentation.matchAll(/<!-- docs-check: (\S+) ([a-f0-9]{64}) -->/g)];
assert.equal(markers.length, files.length, 'Document every changed presentation script');
for (const file of files) {
    const marker = markers.find(match => match[1] === file);
    assert.ok(marker, `Missing docs-check marker for ${file}`);
    const digest = createHash('sha256').update(readFileSync(path.join(root, file))).digest('hex');
    assert.equal(marker[2], digest, `${file} changed: review SYSTEM_DOCUMENTATION.md and update its marker`);
}
console.log(`docs-check: OK (${files.length} presentation, verification and asset files)`);
