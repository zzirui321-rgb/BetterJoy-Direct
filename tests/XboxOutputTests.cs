using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using BetterJoyForCemu.Controller;
using Nefarius.ViGEm.Client;

class XboxOutputTests {
    [StructLayout(LayoutKind.Sequential)]
    struct Pad { public ushort Buttons; public byte LT, RT; public short LX, LY, RX, RY; }
    [StructLayout(LayoutKind.Sequential)]
    struct State { public uint Packet; public Pad Pad; }
    [DllImport("xinput1_4.dll")]
    static extern uint XInputGetState(uint slot, out State state);

    static void Expect(int slot, short x, string phase) {
        for (int attempt = 0; attempt < 20; attempt++) {
            State state;
            if (XInputGetState((uint)slot, out state) == 0 && state.Pad.LX == x &&
                state.Pad.LY == 0 && state.Pad.RX == 0 && state.Pad.RY == 0 && state.Pad.Buttons == 0)
                return;
            Thread.Sleep(50);
        }
        throw new Exception("XInput did not match " + phase);
    }

    static int WaitForSlot(OutputControllerXbox360 output) {
        for (int i = 0; i < 30; i++) {
            try { return output.UserIndex; }
            catch (Nefarius.ViGEm.Client.Targets.Xbox360.Exceptions.Xbox360UserIndexNotReportedException) {
                Thread.Sleep(100);
            }
        }
        throw new Exception("No XInput slot notification within 3 seconds");
    }

    static void Main() {
        try { Run(); }
        catch (Exception error) {
            Console.Error.WriteLine(error.GetType().FullName + ": " + error.Message);
            Environment.ExitCode = 1;
        }
    }

    static void Run() {
        using (var client = new ViGEmClient()) {
            typeof(OutputControllerXbox360).Assembly.GetType("BetterJoyForCemu.Program")
                .GetField("emClient", BindingFlags.Public | BindingFlags.Static).SetValue(null, client);
            for (int cycle = 0; cycle < 3; cycle++) {
                var output = new OutputControllerXbox360();
                output.Connect();
                try {
                    Thread.Sleep(300);
                    int slot = WaitForSlot(output);
                    Expect(slot, 0, "initial neutral, cycle " + cycle);
                    output.UpdateInput(new OutputControllerXbox360InputState { axis_left_x = 12345 });
                    Expect(slot, 12345, "axis movement");
                    output.UpdateInput(new OutputControllerXbox360InputState());
                    Expect(slot, 0, "released neutral");
                } finally { output.Disconnect(); }
                Thread.Sleep(500);
                Console.WriteLine("PASS Xbox attach/move/release/detach cycle " + (cycle + 1));
            }
        }
    }
}
