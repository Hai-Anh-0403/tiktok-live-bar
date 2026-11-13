using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TikTokLiveGame
{
    public sealed class TikTokGameController : MonoBehaviour
    {
        private TikTokWebSocketClient client;
        private PlayerManager playerManager;
        private GiftEffectManager giftEffects;
        private ClubCameraController clubCamera;
        private readonly Dictionary<string, Donor> donors = new();
        private readonly List<FeedEntry> feed = new();
        private readonly Queue<(string Text, float Time)> welcomeQueue = new();
        private string welcomeText;
        private float welcomeStartedAt;
        private string username = "";
        private string connectionStatus = "Đang chờ Node server...";
        private int events;
        private int diamonds;
        private float partyEnergy;
        private bool controlsVisible;
        private bool hudVisible;
        private bool giftBoardVisible = true;
        private bool showGiftBoard = true;
        private bool showTopBoard = true;
        private bool chromaMode;
        private int cinematicVersion;
        private bool restoreControlsAfterCinematic;
        private readonly List<Renderer> environmentRenderers = new();
        private readonly List<Light> environmentLights = new();
        private bool stylesReady;
        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle smallStyle;
        private GUIStyle buttonStyle;
        private GUIStyle inputStyle;
        private GUIStyle rankStyle;
        private GUIStyle bannerStyle;
        private Texture2D energyBack;
        private Texture2D energyFill;
        private Texture2D buttonHover;
        private Texture2D giftBoardBg;
        private Texture2D giftAccent;
        private Texture2D scoreBadgeBg;
        private Texture2D scoreBadgeBorder;
        private Texture2D coinIcon;
        private Texture2D[] giftIcons;
        private GUIStyle giftTitleStyle;
        private GUIStyle giftSubStyle;
        private GUIStyle giftRowLeftStyle;
        private GUIStyle giftCoinStyle;
        private GUIStyle giftRowRightStyle;
        private GUIStyle welcomeStyle;

        private int controlTab = 0;
        private string manualUsername = "Khách VIP";

        public void Initialize(TikTokWebSocketClient socketClient, PlayerManager manager, GiftEffectManager effects)
        {
            client = socketClient;
            playerManager = manager;
            giftEffects = effects;
            clubCamera = Camera.main?.GetComponent<ClubCameraController>();
            client.EventReceived += HandleEvent;
            username = PlayerPrefs.GetString("TikTokUsername", string.Empty);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) controlsVisible = !controlsVisible;
            if (Input.GetKeyDown(KeyCode.F2)) ToggleChroma();
            if (Input.GetKeyDown(KeyCode.F3))
            {
                giftBoardVisible = !giftBoardVisible;
                hudVisible = giftBoardVisible;
            }
            if (Input.GetKeyDown(KeyCode.F4)) showGiftBoard = !showGiftBoard;
            if (Input.GetKeyDown(KeyCode.F5)) showTopBoard = !showTopBoard;
            if (Input.GetKeyDown(KeyCode.F11)) Screen.fullScreen = !Screen.fullScreen;
            for (int index = feed.Count - 1; index >= 0; index--)
                if (Time.unscaledTime - feed[index].Time > 9f) feed.RemoveAt(index);
            AdvanceWelcomeTicker();
        }

        private void HandleEvent(TikTokEvent liveEvent)
        {
            if (liveEvent.type == "status") connectionStatus = liveEvent.message;
            if (liveEvent.type is "member" or "chat" or "gift" or "like" or "follow" or "share") events++;

            if (liveEvent.type == "snapshot")
            {
                donors.Clear();
                diamonds = 0;
                foreach (TikTokPlayerData donor in liveEvent.vipScores ?? System.Array.Empty<TikTokPlayerData>())
                {
                    donors[donor.userId] = new Donor(donor.userId, donor.nickname, donor.score);
                    diamonds += donor.score;
                }
            }
            else if (liveEvent.type == "gift")
            {
                int diamondsToAdd = liveEvent.deltaDiamondCount > 0 ? liveEvent.deltaDiamondCount : liveEvent.diamondCount;
                diamonds += diamondsToAdd;
                donors.TryGetValue(liveEvent.userId, out Donor current);
                donors[liveEvent.userId] = new Donor(liveEvent.userId, liveEvent.nickname, current.Score + diamondsToAdd);
                AddEnergy(diamondsToAdd * 2f);
                AddFeed($"{liveEvent.nickname} tặng {liveEvent.giftName} (+{diamondsToAdd})", new Color(1f, 0.55f, 0.84f));
            }
            else if (liveEvent.type == "chat")
            {
                AddEnergy(2f);
                AddFeed($"{liveEvent.nickname}: {liveEvent.comment}", Color.white);
            }
            else if (liveEvent.type == "like")
            {
                AddEnergy(liveEvent.likeCount * 0.08f);
            }
            else if (liveEvent.type == "member")
            {
                AddEnergy(1f);
                QueueWelcomeTicker(liveEvent);
            }
            else if (liveEvent.type is "follow" or "share")
            {
                AddFeed($"{liveEvent.nickname} đã {liveEvent.type}", new Color(1f, 0.85f, 0.3f));
            }
            else if (liveEvent.type == "music_order")
            {
                MusicPlaylistPlayer.Instance?.RefreshPlaylistOrder();
            }
            else if (liveEvent.type == "music_reload")
            {
                MusicPlaylistPlayer.Instance?.ReloadPlaylist(liveEvent.message);
            }
            else if (liveEvent.type == "reset")
            {
                events = 0;
                diamonds = 0;
                partyEnergy = 0f;
                donors.Clear();
                feed.Clear();
                welcomeQueue.Clear();
                welcomeText = null;
                welcomeStartedAt = 0f;
            }

            playerManager.Handle(liveEvent);
            if (liveEvent.type is "gift" or "snapshot") UpdateTopPlayers();
            if (liveEvent.type == "gift")
            {
                PlayerActor actor = playerManager.Find(liveEvent.userId);
                giftEffects.Play(liveEvent, actor);
            }

            bool joinFocus = liveEvent.action == "join" && liveEvent.joinedNow;
            bool socialFocus = liveEvent.type is "follow" or "share";
            bool requestsFocus = joinFocus || socialFocus || liveEvent.action is "camera" or "walk" or "vip" or "topdj" or "fireworks" or "medal";
            if (requestsFocus && (liveEvent.type is "gift" or "chat" or "follow" or "share"))
            {
                PlayerActor actor = playerManager.Find(liveEvent.userId);
                if (actor == null) return;
                float focusSeconds = liveEvent.durationMs > 0 ? liveEvent.durationMs / 1000f : (socialFocus ? 2f : joinFocus ? 2.5f : 3f);
                bool wideWalkFocus = liveEvent.action == "walk";
                if (joinFocus || socialFocus)
                {
                    clubCamera?.QueueWelcome(actor, focusSeconds);
                }
                else if (liveEvent.action == "fireworks")
                    clubCamera?.FocusFireworks(actor, Mathf.Max(6f, focusSeconds));
                else
                    clubCamera?.Focus(
                        actor,
                        focusSeconds,
                        liveEvent.action is "vip" or "topdj" || liveEvent.diamondCount >= 100,
                        wideWalkFocus);
                // A first-time "hey" is a clean welcome shot. Gift focuses keep
                // the crowd dimming and VIP decoration handled by PlayerManager.
                if (!joinFocus && !socialFocus)
                {
                    playerManager.FocusPlayer(liveEvent.userId, focusSeconds);
                    StartCoroutine(CinematicGift(focusSeconds));
                }
            }
        }

        private System.Collections.IEnumerator CinematicGift(float seconds)
        {
            int version = ++cinematicVersion;
            restoreControlsAfterCinematic |= controlsVisible;
            controlsVisible = false;
            yield return new WaitForSeconds(Mathf.Clamp(seconds, 2.5f, 12f));
            if (version == cinematicVersion)
            {
                controlsVisible = restoreControlsAfterCinematic;
                restoreControlsAfterCinematic = false;
            }
        }

        private void UpdateTopPlayers()
        {
            playerManager.UpdateTopRanks(donors.Values.OrderByDescending(donor => donor.Score).Take(3).Select(donor => donor.UserId));
        }

        private void AddEnergy(float amount)
        {
            partyEnergy += Mathf.Max(0f, amount);
            if (partyEnergy < 1000f) return;
            partyEnergy %= 1000f;
            playerManager.DanceAll(7f);
            giftEffects.PartyBurst();
        }

        private void AddFeed(string text, Color color)
        {
            feed.Add(new FeedEntry(text, color, Time.unscaledTime));
            if (feed.Count > 7) feed.RemoveAt(0);
        }

        private static string CleanWelcomeName(string name)
        {
            return string.Concat((name ?? string.Empty).Select(character =>
                char.IsControl(character) || char.IsWhiteSpace(character) ? ' ' : character)).Trim();
        }

        private void QueueWelcomeTicker(TikTokEvent liveEvent)
        {
            string name = CleanWelcomeName(liveEvent.nickname);
            if (string.IsNullOrEmpty(name)) name = CleanWelcomeName(liveEvent.uniqueId);
            if (string.IsNullOrEmpty(name)) name = "Khách mới";
            welcomeQueue.Enqueue(($"{name} đã vào bar", Time.unscaledTime));
            if (welcomeQueue.Count > 8) welcomeQueue.Dequeue();
            AdvanceWelcomeTicker();
        }

        private void AdvanceWelcomeTicker()
        {
            float now = Time.unscaledTime;
            while (welcomeQueue.Count > 0 && now - welcomeQueue.Peek().Time > 24f)
                welcomeQueue.Dequeue();
            if (!string.IsNullOrEmpty(welcomeText) && now - welcomeStartedAt < 8f) return;
            welcomeText = null;
            if (welcomeQueue.Count == 0) return;
            welcomeText = welcomeQueue.Dequeue().Text;
            welcomeStartedAt = now;
        }

        private void DrawWelcomeTicker(float width)
        {
            if (string.IsNullOrEmpty(welcomeText)) return;
            float panelWidth = Mathf.Min(width - 24f, 680f);
            Rect panel = new((width - panelWidth) * 0.5f, 12f, panelWidth, 40f);
            GUI.DrawTexture(panel, giftBoardBg);
            GUI.DrawTexture(new Rect(panel.x, panel.y, panelWidth, 3f), giftAccent);
            GUI.BeginGroup(panel);
            float textWidth = welcomeStyle.CalcSize(new GUIContent(welcomeText)).x;
            float progress = Mathf.Clamp01((Time.unscaledTime - welcomeStartedAt) / 8f);
            float textX = Mathf.Lerp(panelWidth, -textWidth, progress);
            GUI.Label(new Rect(textX, 0f, textWidth, 40f), welcomeText, welcomeStyle);
            GUI.EndGroup();
        }

        private void OnGUI()
        {
            EnsureStyles();
            float scale = Mathf.Min(Screen.width / 540f, Screen.height / 960f);
            Matrix4x4 originalMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;

            if (giftBoardVisible && !controlsVisible)
            {
                DrawWelcomeTicker(width);
                if (showGiftBoard) DrawGiftBoard();
                if (showTopBoard) DrawCompactTopDonors(width);
            }
            if (hudVisible && !giftBoardVisible)
            {
                DrawHeader(width);
                DrawEnergy(width);
                DrawTopDonors(width);
            }
            DrawFeed(width, height);
            if (controlsVisible) DrawControls();

            if (giftEffects != null && Time.unscaledTime < giftEffects.BannerUntil && !string.IsNullOrEmpty(giftEffects.BannerText))
            {
                string text = giftEffects.BannerText;
                bannerStyle.fontSize = text.Length > 54 ? 15 : text.Length > 40 ? 17 : 19;
                float bannerWidth = Mathf.Clamp(width - 24f, 260f, 680f);
                float bannerX = (width - bannerWidth) * 0.5f;
                float contentHeight = bannerStyle.CalcHeight(new GUIContent(text), bannerWidth);
                float bannerHeight = Mathf.Max(46f, contentHeight + 10f);
                float bannerY = Mathf.Max(420f, height - 250f - bannerHeight);
                GUI.Box(new Rect(bannerX, bannerY, bannerWidth, bannerHeight), text, bannerStyle);
                if (giftAccent != null)
                    GUI.DrawTexture(new Rect(bannerX, bannerY, bannerWidth, 3f), giftAccent);
            }

            GUI.matrix = originalMatrix;
        }

        private void DrawGiftBoard()
        {
            const float x = 12f;
            const float y = 72f;
            const float w = 238f;
            const float h = 234f;

            GUI.DrawTexture(new Rect(x, y, w, h), giftBoardBg);
            GUI.DrawTexture(new Rect(x, y, w, 3.5f), giftAccent);

            GUI.Label(new Rect(x + 12f, y + 8f, w - 20f, 24f), "TƯƠNG TÁC TRÊN SÀN", giftTitleStyle);
            GUI.Label(new Rect(x + 12f, y + 30f, w - 20f, 20f), "Hiệu ứng theo quà nhận được", giftSubStyle);

            DrawGiftRow(x, y + 54f, w, 0, "1", "Vào sàn + Zoom");
            DrawGiftRow(x, y + 83f, w, 1, "1", "Vào sàn + Đổi NV");
            DrawGiftRow(x, y + 112f, w, 2, "5", "Đi dạo quanh sàn");
            DrawGiftRow(x, y + 141f, w, 3, "10", "Cánh VIP");
            DrawGiftRow(x, y + 170f, w, 4, "199", "Hóa Khổng Lồ");
            DrawGiftRow(x, y + 199f, w, 5, "349+", "Lên Ngôi TOP DJ");
        }

        private static void DrawShadowLabel(Rect rect, string text, GUIStyle style, Color color)
        {
            Color previous = GUI.color;
            Color prevNormal = style.normal.textColor;
            style.normal.textColor = Color.white;
            GUI.color = new Color(0f, 0f, 0f, 0.92f);
            GUI.Label(new Rect(rect.x + 1.2f, rect.y + 1.2f, rect.width, rect.height), text, style);
            GUI.color = color;
            GUI.Label(rect, text, style);
            style.normal.textColor = prevNormal;
            GUI.color = previous;
        }

        private void DrawGiftRow(float x, float y, float w, int iconIndex, string cost, string effect)
        {
            Color old = GUI.color;
            GUI.color = Color.white;
            if (giftIcons != null && iconIndex >= 0 && iconIndex < giftIcons.Length && giftIcons[iconIndex] != null)
                GUI.DrawTexture(new Rect(x + 14f, y, 25f, 25f), giftIcons[iconIndex], ScaleMode.ScaleToFit, true);
            if (coinIcon != null)
                GUI.DrawTexture(new Rect(x + 46f, y + 4f, 16f, 16f), coinIcon, ScaleMode.ScaleToFit, true);
            DrawShadowLabel(new Rect(x + 65f, y + 1f, 48f, 24f), cost, giftCoinStyle, new Color(1f, 0.93f, 0.25f));
            DrawShadowLabel(new Rect(x + 108f, y + 1f, w - 118f, 24f), effect, giftRowRightStyle, new Color(0.94f, 0.96f, 1f));
            GUI.color = old;
        }

        private void DrawCompactTopDonors(float width)
        {
            Donor[] top = donors.Values.OrderByDescending(donor => donor.Score).Take(3).ToArray();
            const float w = 248f;
            const float h = 140f;
            float x = width - w - 12f;
            const float y = 72f;

            GUI.DrawTexture(new Rect(x, y, w, h), giftBoardBg);
            GUI.DrawTexture(new Rect(x, y, w, 3.5f), giftAccent);
            DrawShadowLabel(new Rect(x + 12f, y + 8f, w - 24f, 24f), "TOP NGƯỜI ỦNG HỘ", giftTitleStyle, new Color(0.92f, 0.97f, 1f));

            if (top.Length == 0)
            {
                DrawShadowLabel(new Rect(x + 12f, y + 44f, w - 24f, 24f), "Chưa có quà trong phiên", giftRowLeftStyle, Color.white);
                DrawShadowLabel(new Rect(x + 12f, y + 70f, w - 24f, 24f), "Cảm ơn anh em đã ghé chơi", giftSubStyle, new Color(0.38f, 0.98f, 0.86f));
                return;
            }

            for (int index = 0; index < top.Length; index++)
            {
                float rowY = y + 38f + index * 31f;
                Color rankColor = index == 0
                    ? new Color(1f, 0.88f, 0.25f)
                    : index == 1
                        ? new Color(0.55f, 0.93f, 1f)
                        : new Color(1f, 0.66f, 0.38f);
                string donorName = top[index].Name.Length > 12 ? top[index].Name[..11] + "…" : top[index].Name;
                DrawShadowLabel(new Rect(x + 10f, rowY, 134f, 25f), $"#{index + 1} {donorName}", giftRowLeftStyle, rankColor);

                Rect badgeRect = new(x + w - 98f, rowY + 1f, 88f, 24f);
                if (scoreBadgeBg != null) GUI.DrawTexture(badgeRect, scoreBadgeBg);
                if (scoreBadgeBorder != null) GUI.DrawTexture(new Rect(badgeRect.x, badgeRect.y, badgeRect.width, 1.5f), scoreBadgeBorder);
                if (coinIcon != null)
                    GUI.DrawTexture(new Rect(badgeRect.x + 6f, badgeRect.y + 4f, 16f, 16f), coinIcon, ScaleMode.ScaleToFit, true);
                string scoreText = top[index].Score >= 10000
                    ? $"{top[index].Score:N0}"
                    : $"{top[index].Score:N0} xu";
                DrawShadowLabel(new Rect(badgeRect.x + 25f, badgeRect.y, badgeRect.width - 28f, 24f), scoreText, giftCoinStyle, new Color(1f, 0.95f, 0.28f));
            }
        }

        private void DrawHeader(float width)
        {
            GUI.Box(new Rect(18, 16, width - 36, 58), GUIContent.none, panelStyle);
            GUI.Label(new Rect(34, 23, 245, 28), "ÔNG CHÚ MMO", titleStyle);
            string node = client != null && client.IsConnected ? "NODE ONLINE" : "NODE OFFLINE";
            GUI.Label(new Rect(275, 29, 95, 22), node, smallStyle);
            if (width > 900f) GUI.Label(new Rect(width - 250, 24, 230, 24), "F1 CONTROL   F2 CHROMA", smallStyle);
        }

        private void DrawEnergy(float width)
        {
            float barWidth = width > 900f ? Mathf.Min(380f, width * 0.34f) : Mathf.Max(160f, width - 410f);
            float x = width > 900f ? width * 0.5f - barWidth * 0.5f : 390f;
            GUI.Label(new Rect(x, 20, barWidth, 22), "PARTY ENERGY", smallStyle);
            GUI.DrawTexture(new Rect(x, 48, barWidth, 13), energyBack);
            GUI.DrawTexture(new Rect(x, 48, barWidth * Mathf.Clamp01(partyEnergy / 1000f), 13), energyFill);
        }

        private void DrawControls()
        {
            GUI.Box(new Rect(18, 84, 365, 400), GUIContent.none, panelStyle);
            
            GUIStyle activeTab = new GUIStyle(buttonStyle) { normal = { background = buttonHover, textColor = Color.yellow } };
            if (GUI.Button(new Rect(34, 96, 155, 32), "KẾT NỐI & DEMO", controlTab == 0 ? activeTab : buttonStyle)) controlTab = 0;
            if (GUI.Button(new Rect(195, 96, 169, 32), "THÊM KHÁCH VIP", controlTab == 1 ? activeTab : buttonStyle)) controlTab = 1;

            if (controlTab == 0)
            {
                GUI.Label(new Rect(34, 136, 330, 28), connectionStatus, labelStyle);
                GUI.Label(new Rect(34, 166, 330, 24), $"Người chơi {playerManager?.Count ?? 0}   Event {events}   Diamond {diamonds}", smallStyle);
                username = GUI.TextField(new Rect(34, 196, 205, 38), username, inputStyle);
                if (GUI.Button(new Rect(247, 196, 118, 38), "KẾT NỐI", buttonStyle))
                {
                    PlayerPrefs.SetString("TikTokUsername", username);
                    client.ConnectTikTok(username);
                    controlsVisible = false;
                    restoreControlsAfterCinematic = false;
                }
                if (GUI.Button(new Rect(34, 246, 100, 40), "DEMO 30", buttonStyle)) client.StartDemo(30);
                if (GUI.Button(new Rect(142, 246, 100, 40), "DEMO 400", buttonStyle)) client.StartDemo(400);
                if (GUI.Button(new Rect(250, 246, 115, 40), "GIFT 100", buttonStyle)) client.DemoGift(100);
                
                if (GUI.Button(new Rect(34, 296, 100, 36), "RESET", buttonStyle)) client.ResetGame();
                if (GUI.Button(new Rect(142, 296, 100, 36), "ẨN (F1)", buttonStyle)) controlsVisible = false;
                if (GUI.Button(new Rect(250, 296, 115, 36), "GIFT 1000", buttonStyle)) client.DemoGift(1000);
                
                if (GUI.Button(new Rect(34, 344, 100, 36), "NGẮT LIVE", buttonStyle)) client.DisconnectTikTok();
                if (GUI.Button(new Rect(142, 344, 100, 36), "DỪNG DEMO", buttonStyle)) client.StopDemo();
                if (GUI.Button(new Rect(250, 344, 115, 36), chromaMode ? "HIỆN SÂN" : "CHROMA", buttonStyle)) ToggleChroma();
            }
            else
            {
                GUI.Label(new Rect(34, 140, 330, 28), "1. Tên nhân vật muốn thêm:", labelStyle);
                manualUsername = GUI.TextField(new Rect(34, 172, 205, 38), manualUsername, inputStyle);
                if (GUI.Button(new Rect(247, 172, 118, 38), "THÊM VÀO", buttonStyle))
                {
                    HandleEvent(new TikTokEvent { type = "gift", userId = manualUsername, nickname = manualUsername, action = "join", giftName = "Vé Vào Sàn", diamondCount = 1, joinedNow = true, durationMs = 3000 });
                }
                
                GUI.Label(new Rect(34, 226, 330, 28), "2. Tặng quà cho nhân vật này:", labelStyle);
                if (GUI.Button(new Rect(34, 260, 100, 40), "1 Hoa", buttonStyle))
                {
                    HandleEvent(new TikTokEvent { type = "gift", userId = manualUsername, nickname = manualUsername, action = "gift", giftName = "Hoa hồng", diamondCount = 1, durationMs = 3000 });
                }
                if (GUI.Button(new Rect(142, 260, 100, 40), "100 Xu", buttonStyle))
                {
                    HandleEvent(new TikTokEvent { type = "gift", userId = manualUsername, nickname = manualUsername, action = "gift", giftName = "Mũ", diamondCount = 100, durationMs = 5000 });
                }
                if (GUI.Button(new Rect(250, 260, 115, 40), "1000 Xu", buttonStyle))
                {
                    HandleEvent(new TikTokEvent { type = "gift", userId = manualUsername, nickname = manualUsername, action = "gift", giftName = "Siêu xe", diamondCount = 1000, durationMs = 8000 });
                }
                
                if (GUI.Button(new Rect(34, 320, 100, 36), "ẨN (F1)", buttonStyle)) controlsVisible = false;
                if (GUI.Button(new Rect(142, 320, 223, 36), chromaMode ? "HIỆN SÂN" : "CHROMA", buttonStyle)) ToggleChroma();
            }
            if (GUI.Button(new Rect(34, 392, 158, 36), showGiftBoard ? "ẨN QUÀ (F4)" : "HIỆN QUÀ (F4)", buttonStyle)) showGiftBoard = !showGiftBoard;
            if (GUI.Button(new Rect(202, 392, 163, 36), showTopBoard ? "ẨN TOP (F5)" : "HIỆN TOP (F5)", buttonStyle)) showTopBoard = !showTopBoard;
            GUI.Label(new Rect(34, 434, 330, 24), "F3: bật/tắt chung · F4: quà · F5: Top", smallStyle);
            GUI.Label(new Rect(34, 458, 330, 20), "F6: góc quay tiếp theo", smallStyle);
        }

        private void ToggleChroma()
        {
            chromaMode = !chromaMode;
            if (environmentRenderers.Count == 0)
            {
                foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                    if (!renderer.transform.IsChildOf(playerManager.transform)) environmentRenderers.Add(renderer);
                environmentLights.AddRange(FindObjectsByType<Light>(FindObjectsSortMode.None));
            }
            foreach (Renderer renderer in environmentRenderers) if (renderer != null) renderer.enabled = !chromaMode;
            foreach (Light light in environmentLights) if (light != null) light.enabled = !chromaMode;
            RenderSettings.fog = !chromaMode;
            if (Camera.main != null) Camera.main.backgroundColor = chromaMode ? new Color(0f, 1f, 0f) : new Color(0.01f, 0.005f, 0.025f);
        }

        private void DrawTopDonors(float width)
        {
            Donor[] top = donors.Values.OrderByDescending(donor => donor.Score).Take(3).ToArray();
            float x = width - 358f;
            GUI.Box(new Rect(x, 84, 340, 150), GUIContent.none, panelStyle);
            GUI.Label(new Rect(x + 18, 94, 304, 28), "TOP 3 TẶNG QUÀ", titleStyle);
            for (int index = 0; index < top.Length; index++)
            {
                Color old = GUI.color;
                GUI.color = index == 0 ? new Color(1f, 0.82f, 0.25f) : index == 1 ? new Color(0.72f, 0.9f, 1f) : new Color(1f, 0.55f, 0.32f);
                GUI.Label(new Rect(x + 18, 126 + index * 32, 304, 28), $"#{index + 1}  {top[index].Name}    {top[index].Score}", rankStyle);
                GUI.color = old;
            }
        }

        private void DrawFeed(float width, float height)
        {
            float feedWidth = Mathf.Min(680f, width - 36f);
            float feedX = (width - feedWidth) * 0.5f;
            float y = height - 36f;
            for (int i = feed.Count - 1; i >= 0; i--)
            {
                FeedEntry item = feed[i];
                float itemHeight = Mathf.Max(26f, smallStyle.CalcHeight(new GUIContent(item.Text), feedWidth) + 2f);
                y -= itemHeight + 3f;
                if (y < Mathf.Max(420f, height - 220f)) break;
                Color old = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.85f);
                GUI.Label(new Rect(feedX + 1f, y + 1f, feedWidth, itemHeight), item.Text, smallStyle);
                GUI.color = item.Color;
                GUI.Label(new Rect(feedX, y, feedWidth, itemHeight), item.Text, smallStyle);
                GUI.color = old;
            }
        }

        private void EnsureStyles()
        {
            if (stylesReady) return;
            stylesReady = true;
            Texture2D panel = Solid(new Color(0.018f, 0.018f, 0.055f, 0.91f));
            Texture2D button = Solid(new Color(0.14f, 0.08f, 0.28f, 0.98f));
            buttonHover = Solid(new Color(0.08f, 0.45f, 0.62f, 1f));
            Texture2D input = Solid(new Color(0.01f, 0.01f, 0.025f, 0.98f));
            energyBack = Solid(new Color(0.05f, 0.05f, 0.12f, 0.95f));
            energyFill = Solid(new Color(0.1f, 0.9f, 1f, 1f));
            giftBoardBg = Solid(new Color(0.016f, 0.03f, 0.062f, 0.96f));
            giftAccent = Solid(new Color(0.26f, 0.91f, 0.88f, 0.95f));
            scoreBadgeBg = Solid(new Color(0.20f, 0.14f, 0.02f, 0.95f));
            scoreBadgeBorder = Solid(new Color(1f, 0.84f, 0.22f, 0.88f));
            coinIcon = CreateCoinIcon();
            giftIcons = LoadGiftIcons();
            panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = panel }, border = new RectOffset(8, 8, 8, 8) };
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, normal = { textColor = new Color(0.75f, 0.93f, 1f) }, clipping = TextClipping.Clip };
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, wordWrap = true, normal = { textColor = new Color(0.82f, 0.86f, 0.94f) } };
            rankStyle = new GUIStyle(smallStyle) { fontSize = 18, fontStyle = FontStyle.Bold };
            giftTitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.9f, 0.96f, 1f) } };
            welcomeStyle = new GUIStyle(giftTitleStyle) { fontSize = 18, richText = false, wordWrap = false, clipping = TextClipping.Clip, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(0, 0, 0, 0) };
            giftSubStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.38f, 0.98f, 0.86f) } };
            giftRowLeftStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip, normal = { textColor = Color.white } };
            giftCoinStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(1f, 0.92f, 0.25f) } };
            giftRowRightStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight, normal = { textColor = new Color(0.92f, 0.94f, 1f) } };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle = FontStyle.Bold, normal = { background = button, textColor = Color.white }, hover = { background = buttonHover, textColor = Color.white }, active = { background = buttonHover, textColor = Color.white } };
            inputStyle = new GUIStyle(GUI.skin.textField) { fontSize = 18, normal = { background = input, textColor = Color.white }, padding = new RectOffset(12, 8, 8, 6) };
            bannerStyle = new GUIStyle(panelStyle)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(14, 14, 8, 8),
                normal = { background = Solid(new Color(0.04f, 0.1f, 0.18f, 0.96f)), textColor = Color.white }
            };
        }

        private static Texture2D Solid(Color color)
        {
            Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixels(new[] { color, color, color, color });
            texture.Apply();
            return texture;
        }

        private static Texture2D[] LoadGiftIcons()
        {
            string[] names = { "rose.png", "tiktok.png", "finger_heart.png", "rosa.png", "heart.png", "electro.png" };
            Texture2D[] result = new Texture2D[names.Length];
            string buildRoot = System.IO.Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            string projectRoot = System.IO.Directory.GetParent(buildRoot)?.FullName ?? buildRoot;
            string[] folders =
            {
                System.IO.Path.Combine(buildRoot, "LiveAssets", "GIFT_ICONS"),
                System.IO.Path.Combine(projectRoot, "LiveAssets", "GIFT_ICONS"),
                @"D:\Work\LiveTiktok\tiktok-live-bar\LiveAssets\GIFT_ICONS"
            };
            string folder = null;
            foreach (string candidate in folders)
            {
                if (System.IO.Directory.Exists(candidate))
                {
                    folder = candidate;
                    break;
                }
            }
            if (folder == null) return result;

            for (int i = 0; i < names.Length; i++)
            {
                string file = System.IO.Path.Combine(folder, names[i]);
                if (!System.IO.File.Exists(file)) continue;
                try
                {
                    Texture2D tex = new(2, 2, TextureFormat.RGBA32, false)
                    {
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp
                    };
                    tex.LoadImage(System.IO.File.ReadAllBytes(file));
                    tex.Apply();
                    result[i] = tex;
                }
                catch { }
            }
            return result;
        }

        private static Texture2D CreateCoinIcon()
        {
            const int size = 32;
            Texture2D tex = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            float center = (size - 1) * 0.5f;
            float maxR = size * 0.46f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r > maxR + 0.8f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }
                float edgeAlpha = Mathf.Clamp01(maxR + 0.8f - r);
                float norm = r / maxR;
                // Nen xu vang kim + vien sang + hinh thoi lap lanh o giua
                Color gold = Color.Lerp(new Color(1f, 0.92f, 0.38f), new Color(0.95f, 0.58f, 0.08f), norm * 0.85f);
                if (norm > 0.76f && norm < 0.92f)
                    gold = new Color(1f, 0.95f, 0.55f);
                float diamond = Mathf.Abs(dx) + Mathf.Abs(dy);
                if (diamond < maxR * 0.42f)
                    gold = Color.Lerp(gold, new Color(1f, 0.98f, 0.78f), 0.75f);
                if (dx < -1f && dy > 1f && norm < 0.72f)
                    gold = Color.Lerp(gold, Color.white, 0.28f);
                gold.a = edgeAlpha;
                tex.SetPixel(x, y, gold);
            }
            tex.Apply();
            return tex;
        }

        private readonly struct Donor
        {
            public readonly string UserId;
            public readonly string Name;
            public readonly int Score;
            public Donor(string userId, string name, int score)
            {
                UserId = userId;
                Name = string.IsNullOrWhiteSpace(name) ? "TikTok user" : name;
                Score = score;
            }
        }

        private readonly struct FeedEntry
        {
            public readonly string Text;
            public readonly Color Color;
            public readonly float Time;
            public FeedEntry(string text, Color color, float time) { Text = text; Color = color; Time = time; }
        }

        private void OnDestroy()
        {
            if (client != null) client.EventReceived -= HandleEvent;
        }
    }
}
