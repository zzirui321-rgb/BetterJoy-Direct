using System;
using System.Linq;
using System.Reflection;
using BetterJoyForCemu;
using BetterJoyForCemu.Controller;

class FakeSteamShortcutEmitter : ISteamShortcutEmitter {
    public int Screenshots;
    public int Overlays;
    public void EmitScreenshot() { Screenshots++; }
    public void EmitOverlay() { Overlays++; }
}

class InputTests {
    private static int count;
    static void Check(bool condition, string message) {
        if (!condition) {
            Console.Error.WriteLine("FAIL: " + message);
            Environment.Exit(1);
        }
        count++;
    }
    static byte[] Report(byte timestamp = 1) {
        var report = new byte[49]; report[0] = 0x30; report[1] = timestamp; return report;
    }
    static void Main() {
        Check(!InputReportGuard.IsInputReport(null, 49, true), "null report");
        for (int length = 0; length < 49; length++)
            Check(!InputReportGuard.IsInputReport(Report(), length, true), "short report " + length);
        for (int id = 0; id < 256; id++) {
            var report = Report(); report[0] = (byte)id;
            Check(InputReportGuard.IsInputReport(report, 49, true) == (id == 0x30), "report id " + id);
        }
        Check(!InputReportGuard.IsInputReport(Report(), 50, true), "length exceeds buffer");
        var guard = new InputReportGuard();
        var heldCapture = Report(); heldCapture[4] = 0x20;
        Check(guard.Accept(heldCapture, 49, true, 0) && !guard.IsArmed, "wake capture must not arm");
        heldCapture[1]++;
        Check(guard.Accept(heldCapture, 49, true, 1000) && !guard.IsArmed, "held wake button");
        Check(guard.Accept(Report(3), 49, true, 1015) && guard.IsArmed, "release arms input");
        Check(!guard.Accept(Report(3), 49, true, 1030), "duplicate timestamp");
        Check(guard.Accept(Report(255), 49, true, 1045), "timestamp high");
        Check(guard.Accept(Report(0), 49, true, 1060), "timestamp wrap");
        guard = new InputReportGuard();
        Check(guard.Accept(Report(1), 49, true, 0) && !guard.IsArmed, "first neutral frame");
        Check(guard.Accept(Report(2), 49, true, 299) && !guard.IsArmed, "settle before threshold");
        Check(guard.Accept(Report(3), 49, true, 300) && guard.IsArmed, "settle complete");
        Check(Config.GetDefaultValue("capture") == "0", "legacy Capture mapping defaults to none");
        Check(RuntimeOptions.SteamShortcutsEnabled, "Steam shortcuts default on");
        Check(typeof(RuntimeOptions).GetProperty("DesktopMappingsEnabled") == null, "desktop mapping runtime option was removed");
        Check(RuntimeOptions.NormalizeGyroMode("JOY_RIGHT") == RuntimeOptions.GyroRightStick, "gyro mode normalization");
        Check(RuntimeOptions.NormalizeGyroMode("unexpected") == RuntimeOptions.GyroOff, "invalid gyro mode is safe");
        var gyroY = typeof(Joycon).GetMethod("CalculateGyroStickY", BindingFlags.NonPublic | BindingFlags.Static);
        Check(
            (float)gyroY.Invoke(null, new object[] { 0.1f, 10.0f, false }) > 0 &&
            (float)gyroY.Invoke(null, new object[] { 0.1f, 10.0f, true }) < 0,
            "gyro stick Y follows normal direction and supports explicit inversion");
        Check(WindowsSteamShortcutEmitter.ScreenshotVirtualKey == 123, "Steam screenshot uses F12");
        Check(WindowsSteamShortcutEmitter.OverlayModifierVirtualKey == 160 && WindowsSteamShortcutEmitter.OverlayVirtualKey == 9,
            "Steam overlay uses left Shift plus Tab");
        Check(!SteamShortcutRouter.IsControllerRemap("key_65") &&
            !SteamShortcutRouter.IsControllerRemap("mse_1") &&
            SteamShortcutRouter.IsControllerRemap("joy_1"),
            "only legacy controller-to-controller remaps override Steam shortcuts");

        var shortcutEmitter = new FakeSteamShortcutEmitter();
        var shortcutRouter = new SteamShortcutRouter(shortcutEmitter);
        Check(shortcutRouter.Process(true, false, true, true, true) == SteamShortcutAction.Screenshot, "Capture routes to F12");
        Check(shortcutEmitter.Screenshots == 1, "Capture emits once on press");
        Check(shortcutRouter.Process(true, false, true, true, true) == SteamShortcutAction.None && shortcutEmitter.Screenshots == 1, "held Capture does not repeat");
        Check(shortcutRouter.Process(false, false, true, true, true) == SteamShortcutAction.None, "Capture release resets edge");
        Check(shortcutRouter.Process(true, false, false, true, true) == SteamShortcutAction.None && shortcutEmitter.Screenshots == 1, "disabled shortcut stays silent");
        Check(shortcutRouter.Process(true, false, true, true, true) == SteamShortcutAction.None && shortcutEmitter.Screenshots == 1, "enabling while held does not fire");
        shortcutRouter.Process(false, false, false, true, true);
        Check(shortcutRouter.Process(false, true, true, true, true) == SteamShortcutAction.Overlay, "Home routes to Shift+Tab");
        Check(shortcutEmitter.Overlays == 1, "Home emits once on press");
        shortcutRouter.Process(false, false, true, true, true);
        Check(shortcutRouter.Process(true, false, true, false, true) == SteamShortcutAction.None && shortcutEmitter.Screenshots == 1, "paired right Joy-Con cannot duplicate Capture");
        shortcutRouter.Process(false, false, true, false, true);
        Check(shortcutRouter.Process(false, true, true, true, false) == SteamShortcutAction.None && shortcutEmitter.Overlays == 1, "paired left Joy-Con cannot duplicate Home");
        shortcutRouter.Process(false, false, true, true, false);
        Check(shortcutRouter.Process(true, false, true, true, false) == SteamShortcutAction.Screenshot && shortcutEmitter.Screenshots == 2, "paired left Joy-Con owns Capture");
        shortcutRouter.Process(false, false, true, true, false);
        Check(shortcutRouter.Process(false, true, true, false, true) == SteamShortcutAction.Overlay && shortcutEmitter.Overlays == 2, "paired right Joy-Con owns Home");
        var controller = new Joycon(IntPtr.Zero, true, false, .05f, true, "test", "test", 0, true);
        var buttonsField = typeof(Joycon).GetField("buttons", BindingFlags.NonPublic | BindingFlags.Instance);
        var mappedButtons = new bool[20];
        mappedButtons[(int)Joycon.Button.HOME] = true;
        buttonsField.SetValue(controller, mappedButtons);
        var mapXbox = typeof(Joycon).GetMethod("MapToXbox360Input", BindingFlags.NonPublic | BindingFlags.Static);
        RuntimeOptions.SetSteamShortcuts(true);
        var steamHome = (OutputControllerXbox360InputState)mapXbox.Invoke(null, new object[] { controller });
        Check(!steamHome.guide, "Steam Home shortcut suppresses duplicate Xbox Guide");
        RuntimeOptions.SetSteamShortcuts(false);
        var xboxHome = (OutputControllerXbox360InputState)mapXbox.Invoke(null, new object[] { controller });
        Check(xboxHome.guide, "disabling Steam shortcut restores Xbox Guide");
        RuntimeOptions.SetSteamShortcuts(true);
        var center = typeof(Joycon).GetMethod("CenterSticks", BindingFlags.NonPublic | BindingFlags.Instance);
        var bad = (float[])center.Invoke(controller, new object[] { new ushort[] { 2048, 2048 }, new ushort[6], (ushort)200, 1f });
        Check(bad.All(x => x == 0), "invalid calibration stays neutral");
        var valid = new ushort[] { 1800, 1800, 2048, 2048, 1800, 1800 };
        var neutral = (float[])center.Invoke(controller, new object[] { new ushort[] { 2060, 2030 }, valid, (ushort)200, 1f });
        Check(neutral.All(x => x == 0), "normal deadzone");
        var moved = (float[])center.Invoke(controller, new object[] { new ushort[] { 2948, 1148 }, valid, (ushort)200, 1f });
        Check(Math.Abs(moved[0] - .5f) < .001 && Math.Abs(moved[1] + .5f) < .001, "real stick movement survives");
        var programType = typeof(Joycon).Assembly.GetType("BetterJoyForCemu.Program");
        Check(typeof(Joycon).GetMethod("Simulate", BindingFlags.NonPublic | BindingFlags.Instance) == null &&
            programType.GetField("keyboard", BindingFlags.NonPublic | BindingFlags.Static) == null &&
            programType.GetField("mouse", BindingFlags.NonPublic | BindingFlags.Static) == null &&
            typeof(Reassign).GetField("keyboard", BindingFlags.NonPublic | BindingFlags.Instance) == null &&
            typeof(Reassign).GetField("mouse", BindingFlags.NonPublic | BindingFlags.Instance) == null,
            "legacy keyboard and mouse emission and global capture were removed");
        Console.WriteLine("PASS: " + count + " input regression checks");
    }
}
