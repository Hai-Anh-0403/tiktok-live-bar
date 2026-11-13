using System;

namespace TikTokLiveGame
{
    internal static class CharacterMotion
    {
        internal readonly struct Pose
        {
            public readonly float Sway;
            public readonly float Lift;
            public readonly float Lean;
            public readonly float Yaw;
            public readonly float Stretch;

            internal Pose(float sway, float lift, float lean, float yaw, float stretch)
            {
                Sway = sway;
                Lift = lift;
                Lean = lean;
                Yaw = yaw;
                Stretch = stretch;
            }
        }

        internal static Pose Sample(float beat, float phase, float intensity)
        {
            if (float.IsNaN(beat) || float.IsInfinity(beat) ||
                float.IsNaN(phase) || float.IsInfinity(phase) ||
                float.IsNaN(intensity) || float.IsInfinity(intensity)) return default;

            float strength = Math.Max(0f, Math.Min(1f, intensity));
            double position = (beat % 96.0 + (phase % 8.0) * 12.0) % 96.0;
            if (position < 0.0) position += 96.0;
            int style = (int)(position / 16.0);
            double progress = position - style * 16.0;
            float blend = (float)Math.Max(0.0, Math.Min(1.0, (progress - 13.0) / 3.0));
            blend = blend * blend * (3f - 2f * blend);
            Pose current = SampleStyle(style, position);
            Pose next = blend > 0f ? SampleStyle((style + 1) % 6, position) : current;
            return new Pose(
                (current.Sway + (next.Sway - current.Sway) * blend) * strength,
                (current.Lift + (next.Lift - current.Lift) * blend) * strength,
                (current.Lean + (next.Lean - current.Lean) * blend) * strength,
                (current.Yaw + (next.Yaw - current.Yaw) * blend) * strength,
                (current.Stretch + (next.Stretch - current.Stretch) * blend) * strength
            );
        }

        private static Pose SampleStyle(int style, double beat)
        {
            double radians = beat * Math.PI;
            float step = (float)Math.Sin(radians);
            float slow = (float)Math.Sin(radians * 0.5);
            float swing = (float)Math.Sin(radians * 0.25);
            float bounce = step * step;
            return style switch
            {
                0 => new Pose(0.1f * slow, 0.075f * bounce, 4.5f * slow, 4f * swing, 0.018f * step),
                1 => new Pose(0.16f * step, 0.05f * bounce, -6f * step, 7f * slow, 0.025f * bounce),
                2 => new Pose(0.13f * swing, 0.025f * (1f + (float)Math.Cos(radians * 0.5)), 8f * swing, -6f * swing, 0.012f * slow),
                3 => new Pose(0.06f * slow, 0.18f * bounce, -3f * slow, 4f * step, -0.035f * step),
                4 => new Pose(0.09f * step, 0.065f * bounce, 7f * step, 9f * slow, 0.03f * slow),
                _ => new Pose(0.12f * slow, 0.1f * bounce, 5f * slow, 6f * step, 0.04f * step)
            };
        }
    }
}
