using System;
using System.Runtime.InteropServices;
using System.Threading;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets.Xbox360;
class VirtualProbe {
    [StructLayout(LayoutKind.Sequential)]
    public struct Pad { public ushort buttons; public byte lt,rt; public short lx,ly,rx,ry; }
    [StructLayout(LayoutKind.Sequential)]
    public struct State { public uint packet; public Pad pad; }
    [DllImport("xinput1_4.dll", EntryPoint="XInputGetState")]
    static extern uint Read4(uint slot, out State state);
    [DllImport("xinput9_1_0.dll", EntryPoint="XInputGetState")]
    static extern uint Read9(uint slot, out State state);
    static void Read(string phase) {
        for(uint i=0;i<4;i++) {
            State s; uint r=Read4(i,out s);
            Console.WriteLine(phase+" 1_4 slot="+i+" result="+r+" packet="+s.packet+" lx="+s.pad.lx);
            r=Read9(i,out s);
            Console.WriteLine(phase+" 9_1 slot="+i+" result="+r+" packet="+s.packet+" lx="+s.pad.lx);
        }
    }
    static void Main() {
        Read("before");
        using(var client = new ViGEmClient()) {
            var target=client.CreateXbox360Controller(); target.AutoSubmitReport=false;
            target.Connect(); Thread.Sleep(1000);
            Console.WriteLine("ViGEm target UserIndex="+target.UserIndex);
            for(int step=0;step<3;step++) {
                target.SetAxisValue(Xbox360Axis.LeftThumbX,(short)(step==1?12345:0));
                target.SubmitReport(); Thread.Sleep(500); Read("step"+step);
            }
            target.Disconnect();
        }
        Thread.Sleep(500); Read("after");
    }
}
