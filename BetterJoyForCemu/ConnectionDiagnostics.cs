using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace BetterJoyForCemu {
    internal static class ConnectionDiagnostics {
        [StructLayout(LayoutKind.Sequential)]
        private struct Gamepad {
            public ushort Buttons;
            public byte LeftTrigger, RightTrigger;
            public short LX, LY, RX, RY;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct State { public uint Packet; public Gamepad Pad; }
        [DllImport("xinput1_4.dll")]
        private static extern uint XInputGetState(uint index, out State state);

        public static void Run(MainForm form, string destination, int seconds = 10) {
            var log = new StringBuilder();
            File.WriteAllText(destination + ".live", "Diagnostic starting\n");
            form.console.TextChanged += (s, e) => File.WriteAllText(destination + ".live", form.console.Text);
            Application.ThreadException += (s, e) => {
                File.WriteAllText(destination, e.Exception.ToString());
                Environment.Exit(1);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) => File.WriteAllText(destination, e.ExceptionObject.ToString());
            log.AppendLine("BetterJoy Direct hardware diagnostic " + DateTimeOffset.Now.ToString("O"));
            var timer = new Timer { Interval = 250 };
            int samples = 0;
            form.Load += (s, e) => timer.Start();
            timer.Tick += (s, e) => {
                samples++;
                if (Program.mgr != null) {
                    foreach (var device in Program.mgr.j) {
                        log.AppendLine(String.Format("sample={0} pro={1} state={2} transport={3} packets={4} left={5} right={6}",
                            samples, device.isPro, device.state, device.connection, device.packetCounter,
                            String.Join(",", device.GetStick()), String.Join(",", device.GetStick2())));
                        log.AppendLine("ready=" + device.InputReady + " outputError=" + device.LastOutputError);
                        if (device.out_xbox != null) log.AppendLine("virtualSlot=" + device.out_xbox.UserIndex);
                    }
                }
                for (uint slot = 0; slot < 4; slot++) {
                    State state;
                    if (XInputGetState(slot, out state) == 0)
                        log.AppendLine(String.Format("xinput={0} packet={1} buttons={2} axes={3},{4},{5},{6}",
                            slot, state.Packet, state.Pad.Buttons, state.Pad.LX, state.Pad.LY, state.Pad.RX, state.Pad.RY));
                }
                if (samples % 4 == 0) File.WriteAllText(destination, log.ToString(), Encoding.UTF8);
                if (samples < seconds * 4) return;
                timer.Stop();
                log.AppendLine("ViGEm connected: " + (Program.emClient != null));
                log.AppendLine("Steam running: " + System.Diagnostics.Process.GetProcessesByName("steam").Any());
                log.AppendLine("App log:\n" + form.console.Text);
                File.WriteAllText(destination, log.ToString(), Encoding.UTF8);
                using (var bitmap = new System.Drawing.Bitmap(form.Width, form.Height)) {
                    form.DrawToBitmap(bitmap, new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height));
                    bitmap.Save(Path.ChangeExtension(destination, ".png"), System.Drawing.Imaging.ImageFormat.Png);
                }
                timer.Dispose();
                form.Close();
            };
        }
    }
}
