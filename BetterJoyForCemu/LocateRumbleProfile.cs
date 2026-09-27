using System;
using System.Configuration;
using System.Globalization;

namespace BetterJoyForCemu {
    public sealed class LocateRumbleProfile {
        public const float DefaultLowFrequency = 80.0f;
        public const float DefaultHighFrequency = 160.0f;
        public const float DefaultStrength = 0.18f;
        public const int DefaultDurationMs = 120;

        public const float MinimumLowFrequency = 40.875885f;
        public const float MaximumLowFrequency = 626.286133f;
        public const float MinimumHighFrequency = 81.75177f;
        public const float MaximumHighFrequency = 1252.572266f;
        public const float MinimumStrength = 0.05f;
        public const float MaximumStrength = 0.25f;
        public const int MinimumDurationMs = 60;
        public const int MaximumDurationMs = 180;

        public float LowFrequency { get; private set; }
        public float HighFrequency { get; private set; }
        public float Strength { get; private set; }
        public int DurationMs { get; private set; }

        private LocateRumbleProfile(float lowFrequency, float highFrequency, float strength, int durationMs) {
            LowFrequency = lowFrequency;
            HighFrequency = highFrequency;
            Strength = strength;
            DurationMs = durationMs;
        }

        public static LocateRumbleProfile FromConfiguration() {
            return new LocateRumbleProfile(
                Clamp(ReadFloat("LocateRumbleLowFrequency", DefaultLowFrequency), MinimumLowFrequency, MaximumLowFrequency),
                Clamp(ReadFloat("LocateRumbleHighFrequency", DefaultHighFrequency), MinimumHighFrequency, MaximumHighFrequency),
                Clamp(ReadFloat("LocateRumbleStrength", DefaultStrength), MinimumStrength, MaximumStrength),
                Clamp(ReadInt("LocateRumbleDurationMs", DefaultDurationMs), MinimumDurationMs, MaximumDurationMs));
        }

        private static float Clamp(float value, float minimum, float maximum) {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static int Clamp(int value, int minimum, int maximum) {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static float ReadFloat(string key, float fallback) {
            float value;
            if (!Single.TryParse(ConfigurationManager.AppSettings[key], NumberStyles.Float,
                    CultureInfo.InvariantCulture, out value) || Single.IsNaN(value) || Single.IsInfinity(value))
                return fallback;
            return value;
        }

        private static int ReadInt(string key, int fallback) {
            int value;
            return Int32.TryParse(ConfigurationManager.AppSettings[key], NumberStyles.Integer,
                CultureInfo.InvariantCulture, out value) ? value : fallback;
        }
    }
}
