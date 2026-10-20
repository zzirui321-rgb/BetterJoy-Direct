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
        private static volatile int buttonSwapFlags;
        public const string LayoutXbox = "xbox";
        public const string LayoutNintendo = "nintendo";
        public const string LayoutCustom = "custom";

        static RuntimeOptions() {
            ReloadFromConfiguration();
        }

        public static bool SteamShortcutsEnabled { get { return steamShortcutsEnabled; } }
        public static string GyroMode { get { return gyroMode; } }
        public static string ButtonLayout {
            get {
                int flags = buttonSwapFlags;
                return flags == 0 ? LayoutXbox : flags == 3 ? LayoutNintendo : LayoutCustom;
            }
        }

        public static void SetButtonLayout(string layout) {
            if (String.Equals(layout, LayoutNintendo, StringComparison.OrdinalIgnoreCase))
                buttonSwapFlags = 3;
            else if (String.Equals(layout, LayoutXbox, StringComparison.OrdinalIgnoreCase))
                buttonSwapFlags = 0;
            else
                throw new ArgumentException("Unknown button layout.", "layout");
        }

        public static void GetButtonSwaps(out bool swapAB, out bool swapXY) {
            int flags = buttonSwapFlags;
            swapAB = (flags & 1) != 0;
            swapXY = (flags & 2) != 0;
        }

        public static void ReloadFromConfiguration() {
            steamShortcutsEnabled = ReadBoolean("EnableSteamShortcuts", true);
            gyroMode = NormalizeGyroMode(ConfigurationManager.AppSettings["GyroToJoyOrMouse"]);
            buttonSwapFlags = (ReadBoolean("SwapAB", false) ? 1 : 0) |
                              (ReadBoolean("SwapXY", false) ? 2 : 0);
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
