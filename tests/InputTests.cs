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
    static void CheckRumbleMailbox() {
        var rumbleType = typeof(Joycon).GetNestedType("Rumble", BindingFlags.NonPublic);
        var rumble = Activator.CreateInstance(rumbleType, new object[] { new float[] { 80, 160, 0 } });
        var set = rumbleType.GetMethod("set_vals");
        var take = rumbleType.GetMethod("GetData");
        take.Invoke(rumble, null);
        set.Invoke(rumble, new object[] { 80f, 160f, .2f });
        set.Invoke(rumble, new object[] { 80f, 160f, .8f });
        set.Invoke(rumble, new object[] { 80f, 160f, 0f });
        byte[] expectedStop = { 0, 1, 0x40, 0x40, 0, 1, 0x40, 0x40 };
        var stop = (byte[])take.Invoke(rumble, null);
        Check(stop != null && stop.SequenceEqual(expectedStop), "rumble burst delivers latest stop instead of stale queued vibration");
        Check(take.Invoke(rumble, null) == null, "consumed rumble state does not keep generating HID writes");
        set.Invoke(rumble, new object[] { 80f, 160f, 0f });
        Check(take.Invoke(rumble, null) == null, "unchanged rumble feedback does not generate duplicate HID writes");
        Exception concurrentError = null;
        var start = new System.Threading.ManualResetEvent(false);
        var producer = new System.Threading.Thread(() => {
            start.WaitOne();
            try {
                for (int i = 0; i < 5000; i++) set.Invoke(rumble, new object[] { 80f, 160f, (i % 10) / 10f });
            } catch (Exception e) { concurrentError = e; }
        });
        producer.Start();
        start.Set();
        for (int i = 0; i < 5000; i++) {
            try { take.Invoke(rumble, null); } catch (Exception e) { concurrentError = e; break; }
        }
        producer.Join();
        start.Dispose();
        Check(concurrentError == null, "concurrent rumble feedback and polling remain safe");
    }

    static void Main() {
        try { Run(); } catch (Exception error) {
            while (error != null) {
                Console.Error.WriteLine(error.GetType().FullName + ": " + error.Message);
                error = error.InnerException;
            }
            Environment.Exit(1);
        }
    }

    static void CheckControllerTiming() {
        var timing = new ControllerTiming();
        Check(timing.Snapshot() == "hidReports=0 maxGapMs=0.000 outputCount=0 outputMeanMs=0.000 outputMaxMs=0.000 rumbleWrites=0 rumbleMaxMs=0.000", "timing snapshot starts at zero");
        long second = System.Diagnostics.Stopwatch.Frequency;
        timing.RecordInput(0);
        timing.RecordInput(second / 4);
        timing.RecordInput(second / 8);
        timing.RecordInput(second / 2);
        timing.RecordOutput(second / 4);
        timing.RecordOutput(3 * second / 4);
        timing.RecordRumble(second / 2);
        var oldCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
        try {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
            string snapshot = timing.Snapshot();
            Check(snapshot.Contains("hidReports=4 maxGapMs=250.000"), "timing ignores backward timestamps when calculating gaps");
            Check(snapshot.Contains("outputCount=2 outputMeanMs=500.000 outputMaxMs=750.000"), "timing converts Stopwatch ticks with invariant decimal formatting");
            Check(snapshot.Contains("rumbleWrites=1 rumbleMaxMs=500.000"), "timing tracks rumble write duration independently");
        } finally { System.Threading.Thread.CurrentThread.CurrentCulture = oldCulture; }
        var concurrent = new ControllerTiming();
        var writer = new System.Threading.Thread(() => {
            for (int i = 0; i < 1000; i++) concurrent.RecordOutput(1);
        });
        writer.Start();
        for (int i = 0; i < 1000; i++) { concurrent.RecordOutput(1); concurrent.Snapshot(); }
        writer.Join();
        Check(concurrent.Snapshot().Contains("outputCount=2000 "), "timing does not lose concurrent samples");
    }

    static void Run() {
        CheckRumbleMailbox();
        CheckControllerTiming();
        Check(UsbConnectionHelper.IsUsbTransport(@"\\?\hid#vid_057e&pid_2009", UsbConnectionHelper.LegacyUsbSerial, true), "legacy USB serial");
        Check(UsbConnectionHelper.IsUsbTransport(@"\\?\hid#vid_057e&pid_2009", null, true), "Nintendo USB without serial");
        Check(!UsbConnectionHelper.IsUsbTransport(@"\\?\BTHENUM#{00001124}", null, true), "Bluetooth path without serial");
        Check(!UsbConnectionHelper.IsUsbTransport(@"\\?\BTHENUM#{00001124}", UsbConnectionHelper.LegacyUsbSerial, true), "Bluetooth path overrides legacy USB serial");
        Check(!UsbConnectionHelper.IsUsbTransport(@"\\?\hid#vid_057e&pid_2009", "A1B2C3D4E5F6", true), "Bluetooth MAC serial");
        Check(!UsbConnectionHelper.IsUsbTransport(@"\\?\hid#vid_20d6&pid_a711", null, false), "unknown serialless device stays excluded");
        Check(UsbConnectionHelper.IsUsbTransport(@"\\?\hid#vid_057e&pid_2009", "USB-PRO-01", true), "Nintendo USB with non-MAC serial");

        Check(UsbConnectionHelper.IsSuccessfulWrite(2), "payload-length USB write succeeds");
        Check(UsbConnectionHelper.IsSuccessfulWrite(64), "full-report USB write succeeds");
        Check(!UsbConnectionHelper.IsSuccessfulWrite(0), "zero-length USB write fails");
        Check(!UsbConnectionHelper.IsSuccessfulWrite(-1), "negative USB write fails");
        Check(!UsbConnectionHelper.IsEstablishedStream(2), "two input reports do not prove an established stream");
        Check(UsbConnectionHelper.IsEstablishedStream(3), "three input reports prove an established stream");

        var usbResponse = new byte[] { 0x81, 0x01, 0x00 };
        Check(UsbConnectionHelper.IsExpectedResponse(usbResponse, usbResponse.Length, 0x01), "matching USB response");
        Check(!UsbConnectionHelper.IsExpectedResponse(new byte[] { 0x30, 0x01 }, 2, 0x01), "input report is not a USB response");
        Check(!UsbConnectionHelper.IsExpectedResponse(new byte[] { 0x81, 0x02 }, 2, 0x01), "wrong USB command response");
        Check(!UsbConnectionHelper.IsExpectedResponse(usbResponse, 1, 0x01), "short USB response");
        Check(!UsbConnectionHelper.IsExpectedResponse(usbResponse, 4, 0x01), "USB response length exceeds buffer");

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
        guard = new InputReportGuard();
        var wiredNeutral = Report(4); wiredNeutral[4] = 0x80;
        Check(guard.Accept(wiredNeutral, 49, true, 0) && !guard.IsArmed, "wired status flag starts settle window");
        wiredNeutral[1]++;
        Check(guard.Accept(wiredNeutral, 49, true, 300) && guard.IsArmed, "wired status flag does not block neutral gate");
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
        var selectLayout = typeof(RuntimeOptions).GetMethod("SetButtonLayout");
        Check(selectLayout != null, "runtime Nintendo/Xbox layout selector exists");
        var mapDs4 = typeof(Joycon).GetMethod("MapToDualShock4Input", BindingFlags.Public | BindingFlags.Static);
        var secondController = new Joycon(IntPtr.Zero, true, false, .05f, true, "second", "second", 1, true);
        var faceButtons = new[] { Joycon.Button.A, Joycon.Button.B, Joycon.Button.X, Joycon.Button.Y };
        foreach (string layout in new[] { "nintendo", "xbox" }) {
            selectLayout.Invoke(null, new object[] { layout });
            foreach (var pad in new[] { controller, secondController }) {
                for (int i = 0; i < faceButtons.Length; i++) {
                    var pressed = new bool[20];
                    pressed[(int)faceButtons[i]] = true;
                    buttonsField.SetValue(pad, pressed);
                    int expected = layout == "nintendo" ? i : (i ^ 1);
                    var xboxState = (OutputControllerXbox360InputState)mapXbox.Invoke(null, new object[] { pad });
                    var xboxFace = new[] { xboxState.a, xboxState.b, xboxState.x, xboxState.y };
                    Check(xboxFace.Select((v, index) => v == (index == expected)).All(v => v), layout + " maps " + faceButtons[i] + " live on Xbox output for pad " + pad.PadId);
                    var ds4State = (OutputControllerDualShock4InputState)mapDs4.Invoke(null, new object[] { pad });
                    var ds4Face = new[] { ds4State.cross, ds4State.circle, ds4State.square, ds4State.triangle };
                    Check(ds4Face.Select((v, index) => v == (index == expected)).All(v => v), layout + " maps " + faceButtons[i] + " live on DS4 output for pad " + pad.PadId);
                }
            }
        }
        selectLayout.Invoke(null, new object[] { "xbox" });
        buttonsField.SetValue(controller, mappedButtons);
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
