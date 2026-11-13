'use strict';

const { before, after, beforeEach, test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const os = require('node:os');
const net = require('node:net');
const { spawn } = require('node:child_process');
const { once } = require('node:events');
const WebSocket = require('ws');

const bridgeDir = path.resolve(__dirname, '..');
let tempRoot, child, socket, baseUrl;
const messages = [];
const audio = Buffer.from([0, 255, 73, 68, 51, 13, 10, 42]);
let musicDirs;

before(async () => {
    tempRoot = await fs.mkdtemp(path.join(os.tmpdir(), 'music-upload-'));
    const codeDir = path.join(tempRoot, 'TikTokBridge');
    await fs.mkdir(codeDir);
    for (const entry of ['server.js', 'src', 'config', 'public']) {
        await fs.cp(path.join(bridgeDir, entry), path.join(codeDir, entry), { recursive: true });
    }
    musicDirs = [path.join(tempRoot, 'DJ_MUSIC'), path.join(tempRoot, 'Build', 'DJ_MUSIC')];
    const listener = net.createServer();
    listener.listen(0, '127.0.0.1');
    await once(listener, 'listening');
    const port = listener.address().port;
    await new Promise(resolve => listener.close(resolve));
    baseUrl = `http://127.0.0.1:${port}`;
    child = spawn(process.execPath, [path.join(codeDir, 'server.js')], {
        cwd: codeDir,
        env: { PATH: process.env.PATH, NODE_PATH: path.join(bridgeDir, 'node_modules'), HOST: '127.0.0.1', PORT: String(port), ALLOW_LAN: '0' },
        stdio: ['ignore', 'pipe', 'pipe']
    });
    let output = '';
    child.stdout.on('data', chunk => { output += chunk; });
    child.stderr.on('data', chunk => { output += chunk; });
    for (let attempt = 0; attempt < 200; attempt++) {
        if (child.exitCode !== null) throw new Error(output);
        if (output.includes('TikTok Live Game:')) break;
        await new Promise(resolve => setTimeout(resolve, 25));
    }
    assert.match(output, /TikTok Live Game:/, output);
    socket = new WebSocket(baseUrl.replace('http:', 'ws:'));
    socket.on('message', raw => {
        const message = JSON.parse(raw);
        if (message.type === 'music_reload') messages.push(message);
    });
    await once(socket, 'open');
});

after(async () => {
    if (socket) socket.terminate();
    if (child && child.exitCode === null) {
        const exited = once(child, 'exit');
        child.kill('SIGTERM');
        await exited;
    }
    if (tempRoot) await fs.rm(tempRoot, { recursive: true, force: true });
});

beforeEach(async () => {
    for (const dir of musicDirs) await fs.rm(dir, { recursive: true, force: true });
    messages.length = 0;
});

async function upload(fileName, query = '') {
    return fetch(`${baseUrl}/api/music/upload${query}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/octet-stream', 'X-FileName': encodeURIComponent(fileName) },
        body: audio
    });
}

async function flushMessages() {
    const pong = once(socket, 'pong');
    socket.ping();
    await pong;
}

test('legacy upload normalizes the filename, copies exact bytes and broadcasts once', async () => {
    const response = await upload("DJ's mix.MP3");
    assert.equal(response.status, 200);
    assert.deepEqual(await response.json(), { success: true, fileName: 'DJ_s_mix.MP3' });
    for (const dir of musicDirs) assert.deepEqual(await fs.readFile(path.join(dir, 'DJ_s_mix.MP3')), audio);
    await flushMessages();
    assert.deepEqual(messages, [{ type: 'music_reload', message: 'DJ_s_mix.MP3' }]);
});

test('batch uploads stay silent, keep old songs and start the ordered folder once', async () => {
    await fs.mkdir(musicDirs[0], { recursive: true });
    await fs.writeFile(path.join(musicDirs[0], '01-old.mp3'), audio);
    for (const name of ['Z-last.ogg', 'b-second.wav', 'A-first.MP3']) {
        const response = await upload(name, '?autoplay=false');
        assert.equal(response.status, 200);
        assert.deepEqual(await response.json(), { success: true, fileName: name });
        for (const dir of musicDirs) assert.deepEqual(await fs.readFile(path.join(dir, name)), audio);
    }
    await flushMessages();
    assert.deepEqual(messages, []);
    assert.deepEqual(await (await fetch(`${baseUrl}/api/music`)).json(), {
        files: ['01-old.mp3', 'A-first.MP3', 'b-second.wav', 'Z-last.ogg']
    });
    const response = await fetch(`${baseUrl}/api/music/play`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ fileName: '' })
    });
    assert.equal(response.status, 200);
    assert.deepEqual(await response.json(), { success: true });
    await flushMessages();
    assert.deepEqual(messages, [{ type: 'music_reload', message: '' }]);
    assert.deepEqual(await fs.readFile(path.join(musicDirs[0], '01-old.mp3')), audio);
});

test('music listing uses case-insensitive ASCII filename order and filters extensions', async () => {
    await fs.mkdir(musicDirs[0], { recursive: true });
    for (const name of ['z.OGG', 'B.mp3', 'a.wav', '_mix.mp3', '-intro.wav', '10.mp3', '2.mp3', 'VOLUME.txt']) {
        await fs.writeFile(path.join(musicDirs[0], name), audio);
    }
    const response = await fetch(`${baseUrl}/api/music`);
    assert.equal(response.status, 200);
    assert.deepEqual(await response.json(), { files: ['-intro.wav', '10.mp3', '2.mp3', 'a.wav', 'B.mp3', 'z.OGG', '_mix.mp3'] });
});

test('invalid extensions return 400 without storage writes or autoplay', async () => {
    for (const query of ['', '?autoplay=false']) {
        const response = await upload('invalid.exe', query);
        assert.equal(response.status, 400);
        assert.equal(typeof (await response.json()).error, 'string');
    }
    for (const dir of musicDirs) await assert.rejects(fs.access(dir), { code: 'ENOENT' });
    await flushMessages();
    assert.deepEqual(messages, []);
});

test('per-track play preserves the requested filename', async () => {
    const response = await fetch(`${baseUrl}/api/music/play`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ fileName: 'B.mp3' })
    });
    assert.equal(response.status, 200);
    assert.deepEqual(await response.json(), { success: true });
    await flushMessages();
    assert.deepEqual(messages, [{ type: 'music_reload', message: 'B.mp3' }]);
});

test('copy failure returns 500 without autoplay and keeps previously stored songs', async () => {
    await fs.mkdir(musicDirs[0], { recursive: true });
    await fs.mkdir(musicDirs[1], { recursive: true });
    for (const dir of musicDirs) await fs.writeFile(path.join(dir, 'old.mp3'), audio);
    for (const [name, query] of [['legacy.mp3', ''], ['silent.mp3', '?autoplay=false']]) {
        await fs.mkdir(path.join(musicDirs[1], name), { recursive: true });
        const response = await upload(name, query);
        assert.equal(response.status, 500);
        assert.equal(typeof (await response.json()).error, 'string');
        assert.deepEqual(await fs.readFile(path.join(musicDirs[0], name)), audio);
        for (const dir of musicDirs) assert.deepEqual(await fs.readFile(path.join(dir, 'old.mp3')), audio);
    }
    await flushMessages();
    assert.deepEqual(messages, []);
});
