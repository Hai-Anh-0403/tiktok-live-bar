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
let tempRoot, codeDir, port, child, socket, baseUrl;
const messages = [];
const audio = Buffer.from([0, 255, 73, 68, 51, 13, 10, 42]);
let musicDirs;

before(async () => {
    tempRoot = await fs.mkdtemp(path.join(os.tmpdir(), 'music-upload-'));
    codeDir = path.join(tempRoot, 'TikTokBridge');
    await fs.mkdir(codeDir);
    for (const entry of ['server.js', 'src', 'config', 'public']) {
        await fs.cp(path.join(bridgeDir, entry), path.join(codeDir, entry), { recursive: true });
    }
    musicDirs = [path.join(tempRoot, 'DJ_MUSIC'), path.join(tempRoot, 'Build', 'DJ_MUSIC')];
    await fs.writeFile(path.join(tempRoot, 'fault.cjs'), `
        const fs = require('node:fs/promises');
        const path = require('node:path');
        const rename = fs.rename;
        fs.rename = async (from, to) => {
            if (to.endsWith('/Build/DJ_MUSIC/playlist.json')) {
                try {
                    await fs.unlink(${JSON.stringify(path.join(tempRoot, 'fail-rename'))});
                    throw Object.assign(new Error('injected second rename failure'), { code: 'EIO' });
                } catch (error) { if (error.code !== 'ENOENT') throw error; }
            }
            if (from.endsWith('.rollback')) {
                try {
                    await fs.access(${JSON.stringify(path.join(tempRoot, 'fail-rollback'))});
                    throw Object.assign(new Error('injected rollback refusal'), { code: 'EACCES' });
                } catch (error) { if (error.code !== 'ENOENT') throw error; }
            }
            return rename(from, to);
        };
    `);
    const listener = net.createServer();
    listener.listen(0, '127.0.0.1');
    await once(listener, 'listening');
    port = listener.address().port;
    await new Promise(resolve => listener.close(resolve));
    baseUrl = `http://127.0.0.1:${port}`;
    await startServer();
});

async function startServer() {
    child = spawn(process.execPath, ['--require', path.join(tempRoot, 'fault.cjs'), path.join(codeDir, 'server.js')], {
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
        if (message.type.startsWith('music_')) messages.push(message);
    });
    await once(socket, 'open');
}

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
    for (const flag of ['fail-rename', 'fail-rollback']) await fs.rm(path.join(tempRoot, flag), { force: true });
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

async function seed(files) {
    for (const dir of musicDirs) {
        await fs.mkdir(dir, { recursive: true });
        for (const name of files) await fs.writeFile(path.join(dir, name), audio);
    }
}

async function order(files) {
    return fetch(`${baseUrl}/api/music/order`, {
        method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ files })
    });
}

async function listing() {
    return (await (await fetch(`${baseUrl}/api/music`)).json()).files;
}

test('order persists in both manifests across restart, appends new files, and only refreshes native order', async () => {
    await seed(['a.mp3', 'B.wav', 'z.ogg']);
    const files = ['z.ogg', 'a.mp3', 'B.wav'];
    const response = await order(files);
    assert.equal(response.status, 200);
    assert.deepEqual(await response.json(), { success: true });
    assert.deepEqual(await listing(), files);
    for (const dir of musicDirs) {
        assert.deepEqual(JSON.parse(await fs.readFile(path.join(dir, 'playlist.json'), 'utf8')), { files });
        for (const name of files) assert.deepEqual(await fs.readFile(path.join(dir, name)), audio);
        assert.deepEqual((await fs.readdir(dir)).sort(), [...files, 'playlist.json'].sort());
    }
    await flushMessages();
    assert.deepEqual(messages, [{ type: 'music_order' }]);
    socket.terminate();
    const exited = once(child, 'exit'); child.kill('SIGTERM'); await exited;
    await startServer();
    assert.deepEqual(await listing(), files);
    for (const name of ['-new.mp3', 'C-new.wav']) assert.equal((await upload(name, '?autoplay=false')).status, 200);
    assert.deepEqual(await listing(), [...files, '-new.mp3', 'C-new.wav']);
});

test('listing ignores stale, duplicate, non-exact manifest entries and malformed manifests without losing audio', async () => {
    await seed(['a.mp3', 'B.wav', 'z.ogg']);
    await fs.mkdir(path.join(musicDirs[0], 'folder.mp3'));
    for (const manifest of ['{', '{}', '{"files":null}', '{"files":"z.ogg"}']) {
        await fs.writeFile(path.join(musicDirs[0], 'playlist.json'), manifest);
        assert.deepEqual(await listing(), ['a.mp3', 'B.wav', 'z.ogg']);
    }
    await fs.writeFile(path.join(musicDirs[0], 'playlist.json'), JSON.stringify({ files: ['z.ogg', 'z.ogg', 'b.wav', '../a.mp3', 'missing.mp3', 1, 'B.wav'] }));
    assert.deepEqual(await listing(), ['z.ogg', 'B.wav', 'a.mp3']);
});

test('order rejects invalid or stale permutations without metadata, audio changes or events', async () => {
    await seed(['a.mp3', 'b.wav']);
    for (const files of [null, 'a.mp3', [1, 'b.wav'], ['a.mp3', 'a.mp3'], ['../a.mp3', 'b.wav'], ['a.mp3', 'sub/b.wav'], ['a.mp3', 'b\\wav'], ['a.mp3'], ['a.mp3', 'unknown.wav'], ['A.mp3', 'b.wav'], ['a.mp3', 'b.wav', 'new.ogg']]) {
        const response = await order(files);
        assert.ok([400, 409].includes(response.status), `${JSON.stringify(files)}: ${response.status}`);
        assert.equal(typeof (await response.json()).error, 'string');
    }
    for (const dir of musicDirs) {
        assert.deepEqual((await fs.readdir(dir)).sort(), ['a.mp3', 'b.wav']);
        for (const name of ['a.mp3', 'b.wav']) assert.deepEqual(await fs.readFile(path.join(dir, name)), audio);
    }
    await flushMessages(); assert.deepEqual(messages, []);
});

test('second manifest rename failure restores exact previous bytes and cleans only owned metadata temps', async () => {
    await seed(['a.mp3', 'b.wav']);
    const old = Buffer.from(' { "files": ["b.wav", "a.mp3"] }\n');
    await fs.writeFile(path.join(musicDirs[0], 'playlist.json'), old);
    await fs.writeFile(path.join(musicDirs[0], 'unrelated.tmp'), 'keep');
    await fs.writeFile(path.join(musicDirs[1], 'playlist.json'), old);
    await fs.writeFile(path.join(tempRoot, 'fail-rename'), '1');
    const response = await order(['a.mp3', 'b.wav']);
    assert.equal(response.status, 500);
    assert.equal(typeof (await response.json()).error, 'string');
    assert.deepEqual(await fs.readFile(path.join(musicDirs[0], 'playlist.json')), old);
    assert.deepEqual(await fs.readFile(path.join(musicDirs[1], 'playlist.json')), old);
    for (const dir of musicDirs) {
        for (const name of ['a.mp3', 'b.wav']) assert.deepEqual(await fs.readFile(path.join(dir, name)), audio);
        assert.deepEqual((await fs.readdir(dir)).sort(), (dir === musicDirs[0] ? ['a.mp3', 'b.wav', 'playlist.json', 'unrelated.tmp'] : ['a.mp3', 'b.wav', 'playlist.json']).sort());
    }
    await flushMessages(); assert.deepEqual(messages, []);
});

test('failed first save leaves no primary manifest and concurrent saves finish with identical copies', async () => {
    await seed(['a.mp3', 'b.wav']);
    await fs.writeFile(path.join(tempRoot, 'fail-rename'), '1');
    assert.equal((await order(['b.wav', 'a.mp3'])).status, 500);
    await assert.rejects(fs.access(path.join(musicDirs[0], 'playlist.json')), { code: 'ENOENT' });
    await flushMessages(); assert.deepEqual(messages, []);
    await assert.rejects(fs.access(path.join(musicDirs[1], 'playlist.json')), { code: 'ENOENT' });
    const responses = await Promise.all(Array.from({ length: 12 }, (_, i) => order(i % 2 ? ['a.mp3', 'b.wav'] : ['b.wav', 'a.mp3'])));
    for (const response of responses) assert.equal(response.status, 200);
    assert.deepEqual(await fs.readFile(path.join(musicDirs[0], 'playlist.json')), await fs.readFile(path.join(musicDirs[1], 'playlist.json')));
    await flushMessages(); assert.equal(messages.length, 12);
    assert.ok(messages.every(message => JSON.stringify(message) === '{"type":"music_order"}'));
});

test('rollback refusal returns 500, emits nothing and preserves original metadata for recovery', async () => {
    await seed(['a.mp3', 'b.wav']);
    const old = Buffer.from(' { "files": ["b.wav", "a.mp3"] }\n');
    for (const dir of musicDirs) await fs.writeFile(path.join(dir, 'playlist.json'), old);
    for (const flag of ['fail-rename', 'fail-rollback']) await fs.writeFile(path.join(tempRoot, flag), '1');
    const response = await order(['a.mp3', 'b.wav']);
    assert.equal(response.status, 500);
    assert.match((await response.json()).error, /rollback.*injected rollback refusal/);
    for (const dir of musicDirs) {
        const names = await fs.readdir(dir);
        const rollback = names.find(name => name.endsWith('.rollback'));
        assert.ok(rollback, 'original manifest must remain recoverable');
        assert.deepEqual(await fs.readFile(path.join(dir, rollback)), old);
        assert.equal(names.some(name => name.endsWith('.tmp')), false);
        for (const name of ['a.mp3', 'b.wav']) assert.deepEqual(await fs.readFile(path.join(dir, name)), audio);
    }
    await flushMessages(); assert.deepEqual(messages, []);
});
