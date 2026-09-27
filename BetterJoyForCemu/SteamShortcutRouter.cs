using System;

namespace BetterJoyForCemu {
    [Flags]
    public enum SteamShortcutAction {
        None = 0,
        Screenshot = 1,
        Overlay = 2
    }

    public interface ISteamShortcutEmitter {
        void EmitScreenshot();
        void EmitOverlay();
    }

    public sealed class WindowsSteamShortcutEmitter : ISteamShortcutEmitter {
        public const int ScreenshotVirtualKey = 123;      // F12
        public const int OverlayModifierVirtualKey = 160; // Left Shift
        public const int OverlayVirtualKey = 9;            // Tab

        public void EmitScreenshot() {
            WindowsInput.Simulate.Events()
                .Click((WindowsInput.Events.KeyCode)ScreenshotVirtualKey)
                .Invoke();
        }

        public void EmitOverlay() {
            WindowsInput.Simulate.Events()
                .ClickChord(new[] {
                    (WindowsInput.Events.KeyCode)OverlayModifierVirtualKey,
                    (WindowsInput.Events.KeyCode)OverlayVirtualKey
                })
                .Invoke();
        }
    }

    public sealed class SteamShortcutRouter {
        private readonly ISteamShortcutEmitter emitter;
        private bool captureWasDown;
        private bool homeWasDown;

        public SteamShortcutRouter(ISteamShortcutEmitter emitter) {
            if (emitter == null) throw new ArgumentNullException("emitter");
            this.emitter = emitter;
        }

        public static bool IsControllerRemap(string mapping) {
            return !String.IsNullOrEmpty(mapping) && mapping.StartsWith("joy_", StringComparison.Ordinal);
        }

        public SteamShortcutAction Process(
            bool captureIsDown,
            bool homeIsDown,
            bool enabled,
            bool ownsCapture,
            bool ownsHome) {
            bool capturePressed = captureIsDown && !captureWasDown;
            bool homePressed = homeIsDown && !homeWasDown;
            captureWasDown = captureIsDown;
            homeWasDown = homeIsDown;

            if (!enabled) return SteamShortcutAction.None;

            SteamShortcutAction action = SteamShortcutAction.None;
            if (capturePressed && ownsCapture) {
                emitter.EmitScreenshot();
                action |= SteamShortcutAction.Screenshot;
            }
            if (homePressed && ownsHome) {
                emitter.EmitOverlay();
                action |= SteamShortcutAction.Overlay;
            }
            return action;
        }
    }
}
