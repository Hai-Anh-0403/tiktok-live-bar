#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TikTokLiveGame.Editor
{
    public static class CharacterMotionChecks
    {
        [MenuItem("TikTok Live Game/Verify Character Motion")]
        public static void Verify()
        {
            Type motion = typeof(TikTokGameController).Assembly.GetType("TikTokLiveGame.CharacterMotion", true);
            MethodInfo sample = motion.GetMethod("Sample", BindingFlags.NonPublic | BindingFlags.Static);
            if (sample == null) throw new InvalidOperationException("Character motion sampler is missing");
            string[] names = { "Sway", "Lift", "Lean", "Yaw", "Stretch" };
            FieldInfo[] fields = sample.ReturnType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            float[] limits = { 0.18f, 0.2f, 9f, 10f, 0.05f };
            float[] Read(float beat, float phase, float intensity)
            {
                object pose = sample.Invoke(null, new object[] { beat, phase, intensity });
                return names.Select(name => (float)fields.Single(field => field.Name == name).GetValue(pose)).ToArray();
            }
            void Check(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException(message);
            }

            for (int round = 0; round < 2; round++)
            {
                System.Random random = new(917 + round);
                for (int index = 0; index < 6000; index++)
                {
                    float beat = (float)(random.NextDouble() * 20000.0 - 10000.0);
                    float phase = (float)(random.NextDouble() * 16.0 - 8.0);
                    float intensity = (float)(random.NextDouble() * 2.0 - 0.5);
                    float[] pose = Read(beat, phase, intensity);
                    for (int field = 0; field < pose.Length; field++)
                        Check(!float.IsNaN(pose[field]) && !float.IsInfinity(pose[field]) && Math.Abs(pose[field]) <= limits[field] + 0.00001f, "Motion exceeds finite pose bounds");
                    Check(pose[1] >= 0f && 1f - pose[4] * 0.7f > 0f, "Motion crosses floor or inverts the sprite");
                    Check(pose.SequenceEqual(Read(beat, phase, intensity)), "Motion sampling is not deterministic");
                    Check(Read(beat, phase, 0f).All(value => value == 0f), "Zero intensity must be neutral");
                }
            }

            var motifs = Enumerable.Range(0, 6).Select(index => Read(index * 16f + 0.75f, 0f, 1f)).ToArray();
            for (int first = 0; first < motifs.Length; first++)
            for (int second = first + 1; second < motifs.Length; second++)
                Check(motifs[first].Where((value, field) => Math.Abs(value - motifs[second][field]) > 0.0001f).Any(), "Motion motifs are indistinguishable");
            for (int boundary = 16; boundary <= 96; boundary += 16)
            {
                float[] before = Read(boundary - 0.0001f, 0f, 1f);
                float[] after = Read(boundary + 0.0001f, 0f, 1f);
                Check(before.Where((value, field) => Math.Abs(value - after[field]) > 0.05f).Count() == 0, "Motion jumps at a motif boundary");
            }
            foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                Check(Read(invalid, 0f, 1f).All(value => value == 0f), "Invalid beat must be neutral");
                Check(Read(0f, invalid, 1f).All(value => value == 0f), "Invalid phase must be neutral");
                Check(Read(0f, 0f, invalid).All(value => value == 0f), "Invalid intensity must be neutral");
            }
            Check(Read(0.75f, 0f, 0.4f).Any(value => value != 0f), "Idle pose must keep moving");
            Debug.Log("Character motion checks passed: 12,000 samples, six motifs, smooth transitions, finite bounds and neutral invalid inputs.");
        }
    }
}
#endif
