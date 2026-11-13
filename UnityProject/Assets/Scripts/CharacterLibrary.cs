using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TikTokLiveGame
{
    public static class CharacterLibrary
    {
        private static readonly string[] Names =
        {
            "a", "b", "c", "d", "e", "g", "h", "j", "k",
            "mushroom_dance_01", "mushroom_dance_15", "mushroom_magic_02",
            "hanhan_video_dance"
        };

        private static readonly Dictionary<string, Sprite[]> Cache = new();
        private static readonly string[] CastNames = { "club_cat", "club_robot", "club_duck", "club_astronaut" };
        private static readonly string[][] Families = new[] { Names.Take(12).ToArray(), new[] { Names[12] } }
            .Concat(CastNames.Select(name => new[] { name })).ToArray();
        private static readonly int[] FamilyTickets = { 0, 0, 1, 1, 2, 3, 4, 5 };
        private static bool castChecked;

        public static (string name, Sprite[] frames) RandomCharacter(string except = null)
        {
            int familyCount = LoadCast() ? Families.Length : 2;
            int[] choices = FamilyTickets.Where(index => index < familyCount && !Families[index].Contains(except)).ToArray();
            string[] family = Families[choices[UnityEngine.Random.Range(0, choices.Length)]];
            string name = family[UnityEngine.Random.Range(0, family.Length)];
            return (name, Load(name));
        }

        private static bool LoadCast()
        {
            if (castChecked) return Cache.ContainsKey(CastNames[0]);
            castChecked = true;
            Texture2D texture = Resources.Load<Texture2D>("Characters/club_cast");
            if (texture == null || texture.width < 4 || texture.height < 4 || texture.width != texture.height)
            {
                Debug.LogWarning("club_cast is missing or is not a square sheet of at least 4x4 pixels; using original character families.");
                return false;
            }

            float width = texture.width / 4f;
            float height = texture.height / 4f;
            for (int row = 0; row < CastNames.Length; row++)
            {
                Sprite[] frames = new Sprite[4];
                for (int column = 0; column < frames.Length; column++)
                {
                    Rect rect = new(column * width, (3 - row) * height, width, height);
                    frames[column] = Sprite.Create(texture, rect, new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect);
                    frames[column].name = $"{CastNames[row]}_{column:D2}";
                }
                Cache[CastNames[row]] = frames;
            }
            return true;
        }

        private static Sprite[] Load(string name)
        {
            if (Cache.TryGetValue(name, out Sprite[] cached)) return cached;
            Sprite[] frames = Resources.LoadAll<Sprite>($"Characters/{name}")
                .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                .ToArray();
            Cache[name] = frames;
            return frames;
        }
    }
}
