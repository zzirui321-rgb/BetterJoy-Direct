using System;
using System.Configuration;

namespace BetterJoyForCemu {
    public static class RuntimeOptions {
        public const string GyroOff = "none";
        public const string GyroLeftStick = "joy_left";
        public const string GyroRightStick = "joy_right";
        public const string GyroMouse = "mouse";

        private static volatile bool steamShortcutsEnabled;
        private static volatile string gyroMode;

        static RuntimeOptions() {
            ReloadFromConfiguration();
        }

        public static bool SteamShortcutsEnabled { get { return steamShortcutsEnabled; } }
        public static string GyroMode { get { return gyroMode; } }

        public static void ReloadFromConfiguration() {
            steamShortcutsEnabled = ReadBoolean("EnableSteamShortcuts", true);
            gyroMode = NormalizeGyroMode(ConfigurationManager.AppSettings["GyroToJoyOrMouse"]);
        }

        public static void SetSteamShortcuts(bool enabled) {
            steamShortcutsEnabled = enabled;
        }

        public static void SetGyroMode(string mode) {
            gyroMode = NormalizeGyroMode(mode);
        }

        public static string NormalizeGyroMode(string mode) {
            if (String.Equals(mode, GyroLeftStick, StringComparison.OrdinalIgnoreCase)) return GyroLeftStick;
            if (String.Equals(mode, GyroRightStick, StringComparison.OrdinalIgnoreCase)) return GyroRightStick;
            if (String.Equals(mode, GyroMouse, StringComparison.OrdinalIgnoreCase)) return GyroMouse;
            return GyroOff;
        }

        private static bool ReadBoolean(string key, bool fallback) {
            bool value;
            return Boolean.TryParse(ConfigurationManager.AppSettings[key], out value) ? value : fallback;
        }
    }
}
