/* ─────────────────────────────────────────────────────────
   ÔNG CHÚ MMO — Control Panel JS
   © 2025 ÔNG CHÚ MMO — ongchummo.com
   Zalo: 0977.896.644 | Website: https://ongchummo.com
───────────────────────────────────────────────────────── */

/* ═══ TAB NAVIGATION ════════════════════════════════════ */
document.querySelectorAll('.tab-btn').forEach(btn => {
    btn.addEventListener('click', () => {
        document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
        document.querySelectorAll('.tab-panel').forEach(p => p.classList.remove('active'));
        btn.classList.add('active');
        document.getElementById('tab-' + btn.dataset.tab)?.classList.add('active');
    });
});

/* ═══ UI ELEMENT REFS ════════════════════════════════════ */
const ui = {
    // Navbar status
    statusDot:      document.getElementById('status-dot'),
    statusTitle:    document.getElementById('status-title'),
    // Card status (tab live)
    statusCardDot:  document.getElementById('status-card-dot'),
    statusTitleCard:document.getElementById('status-title-card'),
    statusMessage:  document.getElementById('status-message'),
    // Live tab
    username:       document.getElementById('username'),
    connect:        document.getElementById('connect'),
    disconnect:     document.getElementById('disconnect'),
    // Test lab
    stopDemo:       document.getElementById('stop-demo'),
    // Session
    reset:          document.getElementById('reset'),
    // Events
    userIndex:      document.getElementById('user-index'),
    // Rules
    joinMode:       document.getElementById('join-mode'),
    giftAlwaysJoins:document.getElementById('gift-always-joins'),
    masterRules:    document.getElementById('master-rules'),
    masterRuleTemplate: document.getElementById('master-rule-template'),
    addMasterRule:  document.getElementById('add-master-rule'),
    saveMaster:     document.getElementById('save-master'),
    masterMessage:  document.getElementById('master-message'),
    recentGifts:    document.getElementById('recent-gifts'),
    metrics: {
        events:   document.getElementById('metric-events'),
        members:  document.getElementById('metric-members'),
        chats:    document.getElementById('metric-chats'),
        gifts:    document.getElementById('metric-gifts'),
        diamonds: document.getElementById('metric-diamonds'),
        likes:    document.getElementById('metric-likes'),
    },
};

/* ═══ STATE ══════════════════════════════════════════════ */
let socket;
let reconnectTimer;
let masterConfig = { joinMode: 'keyword_only', giftAlwaysJoins: true, rules: [] };
const recentGifts = new Map();

/* ═══ WEBSOCKET ══════════════════════════════════════════ */
function send(message) {
    if (socket?.readyState === WebSocket.OPEN) {
        socket.send(JSON.stringify(message));
    }
}

function setStatus(data) {
    const state = data.state || '';
    ui.statusDot.className = 'status-dot ' + state;
    if (ui.statusCardDot) ui.statusCardDot.className = 'status-card-dot ' + state;

    const names = {
        idle:         'Sẵn sàng',
        connecting:   'Đang kết nối…',
        connected:    '🔴 LIVE',
        demo:         '🧪 DEMO',
        disconnected: 'Mất kết nối',
        ended:        'Live kết thúc',
        error:        'Lỗi kết nối',
    };
    const label = names[state] || state;
    ui.statusTitle.textContent = label;
    if (ui.statusTitleCard) ui.statusTitleCard.textContent = label;
    if (ui.statusMessage)   ui.statusMessage.textContent = data.message || '';
    ui.connect.disabled = state === 'connecting';

    // Badge đỏ nhấp nháy khi đang LIVE thật
    const liveBadge = document.getElementById('live-badge');
    if (liveBadge) liveBadge.classList.toggle('is-live', state === 'connected');
}

function setMetrics(data) {
    for (const [key, el] of Object.entries(ui.metrics)) {
        el.textContent = Number(data[key] || 0).toLocaleString('vi-VN');
    }
}

/* ═══ MASTER RULES ═══════════════════════════════════════ */
function makeRuleId() {
    return `rule-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
}

function emptyRule(overrides = {}) {
    return {
        id: makeRuleId(), enabled: true, source: 'gift', trigger: '',
        giftId: '', match: 'exact', action: 'dance', displayDiamonds: 0,
        durationMs: 3000, label: '', variant: '', fireworkBursts: 0, ...overrides,
    };
}

function setMasterMessage(msg, error = false) {
    ui.masterMessage.textContent = msg || '';
    ui.masterMessage.classList.toggle('error', error);
}

function updateRuleSource(row) {
    const source  = row.querySelector('[data-field="source"]').value;
    const giftId  = row.querySelector('[data-field="giftId"]');
    giftId.disabled     = source !== 'gift';
    giftId.placeholder  = source === 'gift' ? 'ID (nếu có)' : 'Không dùng cho chat';

    // Cập nhật border + badge màu theo loại
    row.dataset.src = source;
    const badge = row.querySelector('[data-src-label]');
    if (badge) {
        badge.textContent = source === 'gift' ? 'GIFT' : 'CHAT';
        badge.className   = `src-badge ${source}`;
    }
}

function createRuleRow(rule) {
    const row = ui.masterRuleTemplate.content.firstElementChild.cloneNode(true);
    row.dataset.ruleId = rule.id || makeRuleId();
    for (const input of row.querySelectorAll('[data-field]')) {
        const field = input.dataset.field;
        if (field === 'durationSeconds') input.value = (Number(rule.durationMs) || 0) / 1000;
        else if (input.type === 'checkbox') input.checked = rule[field] !== false;
        else input.value = rule[field] ?? '';
    }
    row.querySelector('[data-field="source"]').addEventListener('change', () => updateRuleSource(row));
    row.querySelector('[data-command="delete"]').addEventListener('click', () => row.remove());
    row.querySelector('[data-command="test"]').addEventListener('click', () => {
        const config = readMasterFromUi();
        send({ type: 'master_save', master: config });
        send({ type: 'master_test', ruleId: row.dataset.ruleId, diamonds: 100 });
        setMasterMessage('Đang test luật trên Unity…');
    });
    updateRuleSource(row);
    return row;
}

function renderMaster(config) {
    masterConfig = config || masterConfig;
    ui.joinMode.value        = masterConfig.joinMode || 'keyword_only';
    ui.giftAlwaysJoins.checked = masterConfig.giftAlwaysJoins !== false;
    ui.masterRules.replaceChildren(...(masterConfig.rules || []).map(createRuleRow));
}

function readMasterFromUi() {
    const rules = [...ui.masterRules.querySelectorAll('.master-rule-row')].map(row => ({
        id:              row.dataset.ruleId,
        enabled:         row.querySelector('[data-field="enabled"]').checked,
        source:          row.querySelector('[data-field="source"]').value,
        trigger:         row.querySelector('[data-field="trigger"]').value.trim(),
        giftId:          row.querySelector('[data-field="giftId"]').value.trim(),
        match:           row.querySelector('[data-field="match"]').value,
        action:          row.querySelector('[data-field="action"]').value,
        displayDiamonds: Number(row.querySelector('[data-field="displayDiamonds"]').value) || 0,
        durationMs:      Math.round((Number(row.querySelector('[data-field="durationSeconds"]').value) || 0) * 1000),
        label:           row.querySelector('[data-field="label"]').value.trim(),
        variant:         row.querySelector('[data-field="variant"]').value,
        fireworkBursts:  Number(row.querySelector('[data-field="fireworkBursts"]').value) || 0,
    }));
    return { joinMode: ui.joinMode.value, giftAlwaysJoins: ui.giftAlwaysJoins.checked, rules };
}

/* ═══ GIFT CATALOG ═══════════════════════════════════════ */
function observeGift(data) {
    const key = String(data.giftId || data.giftName || 'gift');
    recentGifts.set(key, data);
    while (recentGifts.size > 60) recentGifts.delete(recentGifts.keys().next().value);
    renderGiftCatalog();
}

function renderGiftCatalog() {
    ui.recentGifts.replaceChildren(
        ...[...recentGifts.values()].reverse().map(gift => {
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'gift-catalog-item';
            if (gift.giftPictureUrl) {
                const img = document.createElement('img');
                img.src = gift.giftPictureUrl;
                img.alt = '';
                img.loading = 'lazy';
                btn.append(img);
            }
            const span = document.createElement('span');
            span.textContent = `${gift.giftName || 'Gift'} · ID ${gift.giftId || '?'} · ${gift.diamondCount || 0}💎`;
            btn.append(span);
            btn.addEventListener('click', () => {
                ui.masterRules.append(createRuleRow(emptyRule({
                    source: 'gift', trigger: gift.giftName || '',
                    giftId: String(gift.giftId || ''),
                    displayDiamonds: Number(gift.diamondCount) || 0, action: 'dance',
                })));
                setMasterMessage('Đã thêm gift vào cuối bảng. Chọn hành động rồi bấm Lưu & áp dụng.');
            });
            return btn;
        })
    );
}

/* ═══ SOCKET ═════════════════════════════════════════════ */
function connectSocket() {
    clearTimeout(reconnectTimer);
    const protocol = location.protocol === 'https:' ? 'wss:' : 'ws:';
    socket = new WebSocket(`${protocol}//${location.host}`);

    socket.addEventListener('open', () => {
        send({ type: 'register', role: 'control' });
    });

    socket.addEventListener('message', event => {
        let data;
        try { data = JSON.parse(event.data); } catch { return; }
        if (data.type === 'status' || data.type === 'error') setStatus(data);
        if (data.type === 'metrics')        setMetrics(data);
        if (data.type === 'master_config')  renderMaster(data.master);
        if (data.type === 'master_saved')   setMasterMessage(data.message || 'Đã lưu Master.');
        if (data.type === 'gift_observed')  observeGift(data);
        if (data.type === 'gift_catalog') {
            recentGifts.clear();
            for (const g of data.gifts || [])
                recentGifts.set(String(g.giftId || g.giftName || 'gift'), g);
            renderGiftCatalog();
        }
        if (data.type === 'error') setMasterMessage(data.message || 'Có lỗi.', true);
    });

    socket.addEventListener('close', () => {
        setStatus({ state: 'disconnected', message: 'Đang kết nối lại máy chủ…' });
        reconnectTimer = setTimeout(connectSocket, 2000);
    });

    socket.addEventListener('error', () => socket.close());
}

/* ═══ EVENT LISTENERS ════════════════════════════════════ */
ui.connect.addEventListener('click', () => {
    const username = ui.username.value.trim();
    if (username) send({ type: 'set_username', username });
});
ui.username.addEventListener('keydown', e => { if (e.key === 'Enter') ui.connect.click(); });
ui.disconnect.addEventListener('click', () => send({ type: 'disconnect_tiktok' }));
ui.stopDemo.addEventListener('click',   () => send({ type: 'demo_stop' }));
ui.reset.addEventListener('click',     () => send({ type: 'reset_game' }));
ui.addMasterRule.addEventListener('click', () => ui.masterRules.append(createRuleRow(emptyRule())));
ui.saveMaster.addEventListener('click', () => {
    send({ type: 'master_save', master: readMasterFromUi() });
    setMasterMessage('Đang lưu và áp dụng…');
});

document.querySelectorAll('[data-demo-count]').forEach(btn => {
    btn.addEventListener('click', () =>
        send({ type: 'demo_start', count: Number(btn.dataset.demoCount) })
    );
});

document.querySelectorAll('[data-action]').forEach(btn => {
    btn.addEventListener('click', () =>
        send({
            type:      'demo_event',
            action:    btn.dataset.action,
            value:     Number(btn.dataset.value) || 1,
            giftName:  btn.dataset.giftName || '',
            userIndex: Number(ui.userIndex.value) || 1,
        })
    );
});

/* ═══ MUSIC MANAGEMENT ═════════════════════════════════ */
let musicUploadInProgress = false;

function setMusicUploadInProgress(busy) {
    musicUploadInProgress = busy;
    document.querySelectorAll('#music-file-input, #btn-upload-music, #btn-play-music-list, #music-file-list button')
        .forEach(element => { element.disabled = busy; });
}

async function loadMusicList() {
    const listEl = document.getElementById('music-file-list');
    if (!listEl) return;
    try {
        const res = await fetch('/api/music');
        const data = await res.json();
        if (!res.ok) throw new Error(data.error || `HTTP ${res.status}`);
        if (!data.files || data.files.length === 0) {
            listEl.textContent = 'Thư mục trống. Hãy tải lên file .mp3, .wav, .ogg.';
            return;
        }
        listEl.replaceChildren();
        data.files.forEach((file, index) => {
            const row = document.createElement('div');
            row.className = 'shortcut-row';
            const label = document.createElement('span');
            label.style.minWidth = '0';
            label.style.overflowWrap = 'anywhere';
            label.textContent = `${index + 1}. ${file}`;
            const button = document.createElement('button');
            button.className = 'btn btn-secondary btn-sm';
            button.style.flexShrink = '0';
            button.style.marginLeft = '8px';
            button.textContent = 'Phát bài này';
            button.disabled = musicUploadInProgress;
            button.addEventListener('click', () => window.playTrack(encodeURIComponent(file)));
            row.append(label, button);
            listEl.appendChild(row);
        });
    } catch (e) {
        listEl.textContent = `Lỗi tải danh sách: ${e.message}`;
    }
}

async function requestMusicPlay(fileName) {
    const res = await fetch('/api/music/play', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ fileName })
    });
    const result = await res.json();
    if (!res.ok || !result.success) throw new Error(result.error || `Không thể phát nhạc (HTTP ${res.status})`);
}

window.playTrack = async function(encodedFileName) {
    if (musicUploadInProgress) return;
    const statusEl = document.getElementById('music-upload-status');
    try {
        const fileName = decodeURIComponent(encodedFileName);
        await requestMusicPlay(fileName);
        if (statusEl) statusEl.textContent = fileName
            ? `▶ Đã gửi yêu cầu phát: ${fileName}`
            : '▶ Đã gửi yêu cầu phát từ đầu danh sách.';
    } catch (e) {
        if (statusEl) statusEl.textContent = `Lỗi: ${e.message}`;
    }
};

document.getElementById('btn-refresh-music')?.addEventListener('click', loadMusicList);
document.getElementById('btn-play-music-list')?.addEventListener('click', () => window.playTrack(''));

document.getElementById('btn-upload-music')?.addEventListener('click', async () => {
    if (musicUploadInProgress) return;
    const fileInput = document.getElementById('music-file-input');
    const statusEl = document.getElementById('music-upload-status');
    const files = Array.from(fileInput?.files || []);
    if (files.length === 0) {
        if (statusEl) statusEl.textContent = 'Vui lòng chọn file nhạc (.mp3, .wav, .ogg) từ máy tính!';
        return;
    }
    const names = new Set();
    for (const file of files) {
        const normalizedName = file.name.replace(/[^a-zA-Z0-9._\-]/g, '_').toLowerCase();
        if (!/\.(mp3|wav|ogg)$/i.test(file.name)) {
            if (statusEl) statusEl.textContent = `Lỗi: Chỉ hỗ trợ .mp3, .wav, .ogg: ${file.name}`;
            return;
        }
        if (names.has(normalizedName)) {
            if (statusEl) statusEl.textContent = `Lỗi: Tên file trùng sau chuẩn hóa (không phân biệt hoa/thường): ${file.name}`;
            return;
        }
        names.add(normalizedName);
    }
    let uploadedCount = 0;
    let uploadedFileName = '';
    setMusicUploadInProgress(true);
    try {
        for (const file of files) {
            if (statusEl) statusEl.textContent = `Đang tải ${uploadedCount + 1}/${files.length}: ${file.name}`;
            const res = await fetch('/api/music/upload?autoplay=false', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/octet-stream',
                    'X-FileName': encodeURIComponent(file.name)
                },
                body: file
            });
            const result = await res.json();
            if (!res.ok || !result.success) throw new Error(result.error || `Upload thất bại (HTTP ${res.status})`);
            uploadedCount += 1;
            uploadedFileName = result.fileName;
        }
        await requestMusicPlay(files.length === 1 ? uploadedFileName : '');
        if (statusEl) statusEl.textContent = `✔ Đã tải lên ${uploadedCount}/${files.length}. Đã gửi yêu cầu phát ${files.length === 1 ? uploadedFileName : 'từ đầu danh sách'}.`;
        fileInput.value = '';
    } catch (e) {
        if (statusEl) statusEl.textContent = `Đã tải lên ${uploadedCount}/${files.length}. Lỗi: ${e.message}`;
    } finally {
        setMusicUploadInProgress(false);
        await loadMusicList();
    }
});

document.querySelector('.tab-btn[data-tab="music"]')?.addEventListener('click', loadMusicList);

/* ═══ INIT ═══════════════════════════════════════════════ */
connectSocket();
loadMusicList();
