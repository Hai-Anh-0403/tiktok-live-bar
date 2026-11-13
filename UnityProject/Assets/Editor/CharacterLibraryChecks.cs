#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TikTokLiveGame.Editor
{
    public static class CharacterLibraryChecks
    {
        [MenuItem("TikTok Live Game/Verify Rank Wings")]
        public static void VerifyRankWings()
        {
            UnityEngine.Random.State state = UnityEngine.Random.state;
            GameObject actorObject = new("Rank Wing Check");
            try
            {
                void Check(bool condition, string message)
                {
                    if (!condition) throw new InvalidOperationException(message);
                }
                PlayerActor actor = actorObject.AddComponent<PlayerActor>();
                actor.Initialize(new TikTokPlayerData { userId = "demo-wing-check", nickname = "Wing Check" }, Vector3.zero, Color.white);
                actor.SetCrowdSlot(Vector3.zero, 0.6f);
                SpriteRenderer wings = (SpriteRenderer)typeof(PlayerActor).GetField("wingsRenderer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(actor);
                SpriteRenderer character = (SpriteRenderer)typeof(PlayerActor).GetField("characterRenderer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(actor);
                Sprite[] tiers = { Resources.Load<Sprite>("Banners/wings-fire"), Resources.Load<Sprite>("Banners/wings-ice"), Resources.Load<Sprite>("Banners/wings-neon") };
                Check(tiers.All(sprite => sprite != null) && tiers.Distinct().Count() == 3, "Rank wings need three distinct fire/ice/neon sprites.");
                Check(!wings.enabled, "An ordinary viewer must not start with wings.");
                foreach (int gifts in new[] { 0, 10, 50, 200, 1000, 1000000 })
                {
                    actor.AddGiftPower(gifts - actor.GiftPower);
                    foreach (bool focused in new[] { false, true })
                    {
                        actor.SetGiftFocus(focused);
                        float previousWidth = float.PositiveInfinity;
                        for (int rank = 1; rank <= 3; rank++)
                        {
                            actor.SetTopRank(rank);
                            Check(wings.enabled && wings.sprite == tiers[rank - 1], $"Top {rank} wings changed with {gifts} gifts, focus={focused}.");
                            float width = wings.sprite.bounds.size.x * wings.transform.localScale.x;
                            Check(width > 0f && width <= 2.8001f && width < previousWidth, "Rank wing widths must stay bounded and decrease by tier.");
                            Check(wings.sortingOrder < character.sortingOrder, "Wings must stay behind the character.");
                            Check(Math.Abs(wings.color.a - character.color.a) < 0.0001f, "Wings must inherit character alpha.");
                            previousWidth = width;
                        }
                    }
                }
                foreach (int rank in new[] { 3, 1, 2, 3, 2, 1 })
                {
                    actor.SetTopRank(rank);
                    actor.ChangeCharacter();
                    Check(wings.sprite == tiers[rank - 1], "Changing character must preserve rank wings.");
                }
                actor.SetTopRank(0);
                Check(wings.enabled && wings.sprite == Resources.Load<Sprite>("Banners/energy-wings-v2"), "Demotion during gift focus must restore VIP energy wings.");
                actor.SetGiftFocus(false);
                Check(wings.enabled, "Demotion must preserve high-gift VIP wings.");
                Debug.Log("Rank wing checks passed: distinct Top 1/2/3 sprites, gift/focus priority, bounded tier widths, sorting, alpha, swaps, character changes and VIP demotion.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(actorObject);
                UnityEngine.Random.state = state;
            }
        }

        [MenuItem("TikTok Live Game/Verify Character Diversity")]
        public static void Verify()
        {
            UnityEngine.Random.State state = UnityEngine.Random.state;
            try
            {
                string[] originals =
                {
                    "a", "b", "c", "d", "e", "g", "h", "j", "k",
                    "mushroom_dance_01", "mushroom_dance_15", "mushroom_magic_02", "hanhan_video_dance"
                };
                string[] cast = { "club_cat", "club_robot", "club_duck", "club_astronaut" };
                string[] allNames = originals.Concat(cast).ToArray();
                int[] weights = { 2, 2, 1, 1, 1, 1 };
                int[] expected = { 3000, 3000, 1500, 1500, 1500, 1500 };
                MethodInfo load = typeof(CharacterLibrary).GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Static);
                void Check(bool condition, string message)
                {
                    if (!condition) throw new InvalidOperationException(message);
                }
                int Family(string name)
                {
                    int row = Array.IndexOf(cast, name);
                    if (row >= 0) return row + 2;
                    Check(originals.Contains(name), $"Unknown character: {name}");
                    return name == "hanhan_video_dance" ? 1 : 0;
                }
                void Frames(Sprite[] frames, string name)
                {
                    Check(frames != null && frames.Length > 0 && frames.All(frame => frame != null), $"Missing frames: {name}");
                }

                Texture2D texture = Resources.Load<Texture2D>("Characters/club_cast");
                Check(texture != null, "Missing Characters/club_cast texture; install the cast sheet before verification.");
                Check(texture.width >= 4 && texture.height >= 4 && texture.width == texture.height, "Cast sheet must be square and at least 4x4 pixels.");
                Check(load != null, "Character loader is missing.");
                foreach (string name in originals)
                {
                    Sprite[] actual = (Sprite[])load.Invoke(null, new object[] { name });
                    Frames(actual, name);
                    Sprite[] source = Resources.LoadAll<Sprite>($"Characters/{name}").OrderBy(sprite => sprite.name, StringComparer.Ordinal).ToArray();
                    Check(actual.SequenceEqual(source), $"Original clip changed: {name}");
                    Check(ReferenceEquals(actual, load.Invoke(null, new object[] { name })), $"Original cache changed: {name}");
                }

                Dictionary<string, Sprite[]> cached = new();
                for (int round = 0; round < 2; round++)
                {
                    UnityEngine.Random.InitState(917 + round);
                    int[] counts = new int[6];
                    HashSet<string> seen = new();
                    for (int index = 0; index < 12000; index++)
                    {
                        (string name, Sprite[] frames) = CharacterLibrary.RandomCharacter();
                        counts[Family(name)]++;
                        seen.Add(name);
                        Frames(frames, name);
                        if (cached.TryGetValue(name, out Sprite[] previous))
                            Check(ReferenceEquals(frames, previous), $"Frames are not cached: {name}");
                        else cached.Add(name, frames);
                    }
                    for (int family = 0; family < counts.Length; family++)
                        Check(Math.Abs(counts[family] - expected[family]) <= expected[family] * 0.15f, $"Family distribution must give legacy/new 50/50: {string.Join(", ", counts)}");
                    Check(allNames.All(seen.Contains), "Some original clips or new families were never selected.");
                    foreach (string previous in allNames)
                    {
                        int previousFamily = Family(previous);
                        int[] changes = new int[6];
                        for (int index = 0; index < 4096; index++)
                        {
                            (string name, Sprite[] frames) = CharacterLibrary.RandomCharacter(previous);
                            changes[Family(name)]++;
                            Check(Family(name) != previousFamily, $"Change kept the same family: {previous} -> {name}");
                            Check(ReferenceEquals(frames, cached[name]), $"Change bypassed frame cache: {name}");
                        }
                        for (int family = 0; family < changes.Length; family++)
                        {
                            if (family == previousFamily) continue;
                            double expectedChanges = 4096d * weights[family] / (8 - weights[previousFamily]);
                            Check(Math.Abs(changes[family] - expectedChanges) <= expectedChanges * 0.15d, $"Change ignored conditional family weights: {previous} [{string.Join(", ", changes)}]");
                        }
                    }
                    Debug.Log($"Character diversity round {round + 1}: 12,000 weighted picks [{string.Join(", ", counts)}], 69,632 different-family changes with conditional weights.");
                }

                for (int row = 0; row < cast.Length; row++)
                {
                    Sprite[] frames = cached[cast[row]];
                    Check(frames.Length == 4, $"Expected four frames: {cast[row]}");
                    for (int column = 0; column < frames.Length; column++)
                    {
                        Sprite frame = frames[column];
                        Rect rect = frame.rect;
                        Check(frame.texture == texture, "Cast sprites must share the loaded texture.");
                        Check(rect.x == column * (texture.width / 4f) && rect.y == (3 - row) * (texture.height / 4f)
                            && rect.width == texture.width / 4f && rect.height == texture.height / 4f, $"Wrong cast frame rect: {cast[row]} [{column}]");
                        Check(rect.x >= 0f && rect.y >= 0f && rect.x + rect.width <= texture.width && rect.y + rect.height <= texture.height, "Cast rect exceeds texture bounds.");
                        Check(frame.pivot.x == rect.width * 0.5f && frame.pivot.y == 0f && frame.pixelsPerUnit == 100f, "Cast sprites must use bottom-center pivots and 100 PPU.");
                        Check(Math.Abs(frame.bounds.size.x - rect.width / 100f) < 0.00001f
                            && Math.Abs(frame.bounds.size.y - rect.height / 100f) < 0.00001f, "Cast sprites must retain full-cell bounds for character scaling.");
                    }
                }
                Debug.Log("Character diversity checks passed: legacy/new 50/50, conditional weighted different-family changes, all 13 original clips, shared texture and cached 4x4 frames.");
            }
            finally
            {
                UnityEngine.Random.state = state;
            }
        }
    }
}
#endif
