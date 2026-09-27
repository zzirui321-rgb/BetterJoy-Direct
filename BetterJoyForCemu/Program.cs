using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using BetterJoyForCemu.Collections;
using Nefarius.ViGEm.Client;
using static BetterJoyForCemu._3rdPartyControllers;
using static BetterJoyForCemu.HIDapi;

namespace BetterJoyForCemu {
    public class JoyconManager {
        public bool EnableIMU = true;
        public bool EnableLocalize = false;

        private const ushort vendor_id = 0x57e;
        private const ushort product_l = 0x2006;
        private const ushort product_r = 0x2007;
        private const ushort product_pro = 0x2009;
        private const ushort product_snes = 0x2017;
        private const ushort product_n64 = 0x2019;

        public ConcurrentList<Joycon> j { get; private set; } // Array of all connected Joy-Cons
        static JoyconManager instance;

        public MainForm form;

        System.Timers.Timer controllerCheck;

        public static JoyconManager Instance {
            get { return instance; }
        }

        public void Awake() {
            instance = this;
            j = new ConcurrentList<Joycon>();
            HIDapi.hid_init();
        }

        public void Start() {
            controllerCheck = new System.Timers.Timer(2000); // check for new controllers every 2 seconds
            controllerCheck.Elapsed += CheckForNewControllersTime;
            controllerCheck.Start();
        }

        bool ControllerAlreadyAdded(string path) {
            foreach (Joycon v in j)
                if (v.path == path)
                    return true;
            return false;
        }

        void CleanUp() { // removes dropped controllers from list
            List<Joycon> rem = new List<Joycon>();
            foreach (Joycon joycon in j) {
                if (joycon.state == Joycon.state_.DROPPED) {
                    if (joycon.other != null)
                        joycon.other.other = null; // The other of the other is the joycon itself

                    joycon.Detach(true);
                    rem.Add(joycon);

                    foreach (Button b in form.con) {
                        if (b.Enabled & b.Tag == joycon) {
                            b.Invoke(new MethodInvoker(delegate {
                                b.BackColor = System.Drawing.Color.FromArgb(0x00, System.Drawing.SystemColors.Control);
                                b.Enabled = false;
                                b.BackgroundImage = null;
                                form.SetSlotDisconnected(b);
                            }));
                            break;
                        }
                    }

                    form.AppendTextBox("Removed dropped controller. Can be reconnected.\r\n");
                }
            }

            foreach (Joycon v in rem)
                j.Remove(v);
        }

        void CheckForNewControllersTime(Object source, ElapsedEventArgs e) {
            if (!form.IsHandleCreated || form.IsDisposed) return;
            form.BeginInvoke(new Action(() => {
            CleanUp();
            if (Config.IntValue("ProgressiveScan") == 1) {
                CheckForNewControllers();
            }
            }));
        }

        private ushort TypeToProdId(byte type) {
            switch (type) {
                case 1:
                    return product_pro;
                case 2:
                    return product_l;
                case 3:
                    return product_r;
            }
            return 0;
        }

        public void CheckForNewControllers() {
            // move all code for initializing devices here and well as the initial code from Start()
            bool isLeft = false;
            IntPtr ptr = HIDapi.hid_enumerate(0x0, 0x0);
            IntPtr top_ptr = ptr;

            hid_device_info enumerate; // Add device to list
            bool foundNew = false;
            while (ptr != IntPtr.Zero) {
                SController thirdParty = null;
                enumerate = (hid_device_info)Marshal.PtrToStructure(ptr, typeof(hid_device_info));

                if (enumerate.serial_number == null) {
                    ptr = enumerate.next; // can't believe it took me this long to figure out why USB connections used up so much CPU.
                                          // it was getting stuck in an inf loop here!
                    continue;
                }

                bool validController = (enumerate.product_id == product_l || enumerate.product_id == product_r ||
                                        enumerate.product_id == product_pro || enumerate.product_id == product_snes || enumerate.product_id == product_n64) && enumerate.vendor_id == vendor_id;
                // check list of custom controllers specified
                foreach (SController v in Program.thirdPartyCons) {
                    if (enumerate.vendor_id == v.vendor_id && enumerate.product_id == v.product_id && enumerate.serial_number == v.serial_number) {
                        validController = true;
                        thirdParty = v;
                        break;
                    }
                }

                ushort prod_id = thirdParty == null ? enumerate.product_id : TypeToProdId(thirdParty.type);
                if (prod_id == 0) {
                    ptr = enumerate.next; // controller was not assigned a type, but advance ptr anyway
                    continue;
                }

                if (validController && !ControllerAlreadyAdded(enumerate.path)) {
                    switch (prod_id) {
                        case product_l:
                            isLeft = true;
                            form.AppendTextBox("Left Joy-Con connected.\r\n"); break;
                        case product_r:
                            isLeft = false;
                            form.AppendTextBox("Right Joy-Con connected.\r\n"); break;
                        case product_pro:
                            isLeft = true;
                            form.AppendTextBox("Pro controller connected.\r\n"); break;
                        case product_snes:
                            isLeft = true;
                            form.AppendTextBox("SNES controller connected.\r\n"); break;
                        case product_n64:
                            isLeft = true;
                            form.AppendTextBox("N64 controller connected.\r\n"); break;
                        default:
                            form.AppendTextBox("Non Joy-Con Nintendo input device skipped.\r\n"); break;
                    }

                    // Add controller to block-list for HidGuardian
                    if (Program.useHIDG) {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(@"http://localhost:26762/api/v1/hidguardian/affected/add/");
                        string postData = @"hwids=HID\" + enumerate.path.Split('#')[1].ToUpper();
                        var data = Encoding.UTF8.GetBytes(postData);

                        request.Method = "POST";
                        request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                        request.ContentLength = data.Length;

                        using (var stream = request.GetRequestStream())
                            stream.Write(data, 0, data.Length);

                        try {
                            var response = (HttpWebResponse)request.GetResponse();
                            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                        } catch {
                            form.AppendTextBox("Unable to add controller to block-list.\r\n");
                        }
                    }
                    // -------------------- //

                    IntPtr handle = HIDapi.hid_open_path(enumerate.path);
                    if (handle == IntPtr.Zero) {
                        form.AppendTextBox("Controller is busy or unavailable; retrying on next scan.\r\n");
                        ptr = enumerate.next;
                        continue;
                    }
                    try {
                        HIDapi.hid_set_nonblocking(handle, 1);
                    } catch {
                        form.AppendTextBox("Unable to open path to device - are you using the correct (64 vs 32-bit) version for your PC?\r\n");
                        break;
                    }

                    bool isPro = prod_id == product_pro;
                    bool isSnes = prod_id == product_snes;
                    bool is64 = prod_id == product_n64;
                    j.Add(new Joycon(handle, EnableIMU, EnableLocalize & EnableIMU, 0.05f, isLeft, enumerate.path, enumerate.serial_number, j.Count, isPro, isSnes, is64,thirdParty != null));

                    foundNew = true;
                    j.Last().form = form;

                    if (j.Count < 5) {
                        int ii = -1;
                        foreach (Button v in form.con) {
                            ii++;
                            if (!v.Enabled) {
                                System.Drawing.Bitmap temp;
                                switch (prod_id) {
                                    case (product_l):
                                        temp = Properties.Resources.jc_left_s; break;
                                    case (product_r):
                                        temp = Properties.Resources.jc_right_s; break;
                                    case (product_pro):
                                        temp = Properties.Resources.pro; break;
                                    case (product_snes):
                                        temp = Properties.Resources.snes; break;
                                    case (product_n64):
                                        temp = Properties.Resources.ultra; break;
                                    default:
                                        temp = Properties.Resources.cross; break;
                                }

                                v.Invoke(new MethodInvoker(delegate {
                                    v.Tag = j.Last(); // assign controller to button
                                    v.Enabled = true;
                                    v.Text = "";
                                    v.Click -= new EventHandler(form.conBtnClick);
                                    v.Click += new EventHandler(form.conBtnClick);
                                    v.BackgroundImage = temp;
                                }));

                                form.loc[ii].Invoke(new MethodInvoker(delegate {
                                    form.loc[ii].Tag = v;
                                    form.loc[ii].Click -= new EventHandler(form.locBtnClickAsync);
                                    form.loc[ii].Click += new EventHandler(form.locBtnClickAsync);
                                }));

                                break;
                            }
                        }
                    }

                    byte[] mac = new byte[6];
                    try {
                        for (int n = 0; n < 6; n++)
                            mac[n] = byte.Parse(enumerate.serial_number.Substring(n * 2, 2), System.Globalization.NumberStyles.HexNumber);
                    } catch (Exception e) {
                        // could not parse mac address
                    }
                    j[j.Count - 1].PadMacAddress = new PhysicalAddress(mac);
                }

                ptr = enumerate.next;
            }

            if (foundNew) { // attempt to auto join-up joycons on connection
                Joycon temp = null;
                foreach (Joycon v in j) {
                    // Do not attach two controllers if they are either:
                    // - Not a Joycon
                    // - Already attached to another Joycon (that isn't itself)
                    if (v.isPro || (v.other != null && v.other != v)) {
                        continue;
                    }

                    // Otherwise, iterate through and find the Joycon with the lowest
                    // id that has not been attached already (Does not include self)
                    if (temp == null)
                        temp = v;
                    else if (temp.isLeft != v.isLeft && v.other == null) {
                        temp.other = v;
                        v.other = temp;

                        if (temp.out_xbox != null) {
                            try {
                                temp.out_xbox.Disconnect();
                            } catch (Exception e) {
                                // it wasn't connected in the first place, go figure
                            }
                        }
                        if (temp.out_ds4 != null) {
                            try {
                                temp.out_ds4.Disconnect();
                            } catch (Exception e) {
                                // it wasn't connected in the first place, go figure
                            }
                        }
                        temp.out_xbox = null;
                        temp.out_ds4 = null;

                        foreach (Button b in form.con)
                            if (b.Tag == v || b.Tag == temp) {
                                Joycon tt = (b.Tag == v) ? v : (b.Tag == temp) ? temp : v;
                                b.BackgroundImage = tt.isLeft ? Properties.Resources.jc_left : Properties.Resources.jc_right;
                            }

                        temp = null;    // repeat
                    }
                }
            }

            HIDapi.hid_free_enumeration(top_ptr);

            bool on = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).AppSettings.Settings["HomeLEDOn"].Value.ToLower() == "true";
            foreach (Joycon jc in j) { // Connect device straight away
                if (jc.state == Joycon.state_.NOT_ATTACHED) {
                    try {
                    if (jc.out_xbox != null)
                        jc.out_xbox.Connect();
                    if (jc.out_ds4 != null)
                        jc.out_ds4.Connect();

                        jc.Attach();
                    } catch (Exception e) {
                        form.AppendTextBox("Controller initialization failed: " + e.Message + "\r\n");
                        jc.state = Joycon.state_.DROPPED;
                        continue;
                    }

                    jc.SetHomeLight(on);

                    jc.Begin();
                    if (form.allowCalibration) {
                        jc.getActiveData();
                    }
                }
            }
        }

        public void OnApplicationQuit() {
            controllerCheck?.Stop();
            foreach (Joycon v in j) {
                if (Boolean.Parse(ConfigurationManager.AppSettings["AutoPowerOff"]))
                    v.PowerOff();

                v.Detach();

            }

            controllerCheck?.Dispose();
            HIDapi.hid_exit();
        }
    }

    class Program {
        public static PhysicalAddress btMAC = new PhysicalAddress(new byte[] { 0, 0, 0, 0, 0, 0 });
        public static UdpServer server;

        public static ViGEmClient emClient;

        private static readonly HttpClient client = new HttpClient();

        public static JoyconManager mgr;
        static string pid;

        static MainForm form;

        static public bool useHIDG = Boolean.Parse(ConfigurationManager.AppSettings["UseHIDG"]);

        public static List<SController> thirdPartyCons = new List<SController>();

        public static void Start() {
            pid = Process.GetCurrentProcess().Id.ToString(); // get current process id for HidCerberus.Srv

            if (useHIDG) {
                form.console.AppendText("HidGuardian is enabled.\r\n");
                try {
                    var HidCerberusService = new ServiceController("HidCerberus Service");
                    if (HidCerberusService.Status == ServiceControllerStatus.Stopped) {
                        form.console.AppendText("HidGuardian was stopped. Starting...\r\n");

                        try {
                            HidCerberusService.Start();
                        } catch (Exception e) {
                            form.console.AppendText("Unable to start HidGuardian - everything should work fine without it, but if you need it, run the app again as an admin.\r\n");
                            useHIDG = false;
                        }
                    }
                } catch (Exception e) {
                    form.console.AppendText("Unable to start HidGuardian - everything should work fine without it, but if you need it, install it properly as admin.\r\n");
                    useHIDG = false;
                }

                HttpWebResponse response;
                if (Boolean.Parse(ConfigurationManager.AppSettings["PurgeWhitelist"])) {
                    try {
                        response = (HttpWebResponse)WebRequest.Create(@"http://localhost:26762/api/v1/hidguardian/whitelist/purge/").GetResponse(); // remove all programs allowed to see controller
                    } catch (Exception e) {
                        form.console.AppendText("Unable to purge whitelist.\r\n");
                        useHIDG = false;
                    }
                }

                try {
                    response = (HttpWebResponse)WebRequest.Create(@"http://localhost:26762/api/v1/hidguardian/whitelist/add/" + pid).GetResponse(); // add BetterJoyForCemu to allowed processes 
                } catch (Exception e) {
                    form.console.AppendText("Unable to add program to whitelist.\r\n");
                    useHIDG = false;
                }
            }

            if (Boolean.Parse(ConfigurationManager.AppSettings["ShowAsXInput"]) || Boolean.Parse(ConfigurationManager.AppSettings["ShowAsDS4"])) {
                form.console.AppendText("Connecting Xbox virtual driver...\r\n");
                try {
                    emClient = new ViGEmClient(); // Manages emulated XInput
                } catch (Nefarius.ViGEm.Client.Exceptions.VigemBusNotFoundException) {
                    form.console.AppendText("Could not start VigemBus. Make sure drivers are installed correctly.\r\n");
                    if (ViGEmBusInstaller.PromptAndInstall(form)) {
                        try {
                            emClient = new ViGEmClient();
                            form.console.AppendText("ViGEmBus installed and connected.\r\n");
                        } catch (Nefarius.ViGEm.Client.Exceptions.VigemBusNotFoundException) {
                            form.console.AppendText(form.GetUiText("DriverInstallRetryFailed") + "\r\n");
                            MessageBox.Show(form, form.GetUiText("DriverInstallRetryFailed"),
                                form.GetUiText("DriverInstallTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }

            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces()) {
                // Get local BT host MAC
                if (nic.NetworkInterfaceType != NetworkInterfaceType.FastEthernetFx && nic.NetworkInterfaceType != NetworkInterfaceType.Wireless80211) {
                    if (nic.Name.Split()[0] == "Bluetooth") {
                        btMAC = nic.GetPhysicalAddress();
                    }
                }
            }

            // a bit hacky
            form.console.AppendText("Enumerating HID controllers...\r\n");
            _3rdPartyControllers partyForm = new _3rdPartyControllers();
            partyForm.CopyCustomControllers();

            mgr = new JoyconManager();
            mgr.form = form;
            mgr.Awake();
            mgr.CheckForNewControllers();
            mgr.Start();

            server = new UdpServer(mgr.j);
            server.form = form;

            server.Start(IPAddress.Parse(ConfigurationManager.AppSettings["IP"]), Int32.Parse(ConfigurationManager.AppSettings["Port"]));

            form.console.AppendText("All systems go\r\n");
        }

        public static void Stop() {
            if (Program.useHIDG) {
                try {
                    HttpWebResponse response = (HttpWebResponse)WebRequest.Create(@"http://localhost:26762/api/v1/hidguardian/whitelist/remove/" + pid).GetResponse();
                } catch (Exception e) {
                    form.console.AppendText("Unable to remove program from whitelist.\r\n");
                }
            }

            if (Boolean.Parse(ConfigurationManager.AppSettings["PurgeAffectedDevices"]) && Program.useHIDG) {
                try {
                    HttpWebResponse r1 = (HttpWebResponse)WebRequest.Create(@"http://localhost:26762/api/v1/hidguardian/affected/purge/").GetResponse();
                } catch { }
            }

            server?.Stop();
            mgr?.OnApplicationQuit();
        }

        private static string appGuid = "1bf709e9-c133-41df-933a-c9ff3f664c7b"; // randomly-generated
        [STAThread]
        static void Main(string[] args) {

            // Setting the culturesettings so float gets parsed correctly
            CultureInfo.CurrentCulture = new CultureInfo("en-US", false);

            if (args.Length >= 2 && args[0] == "--render-preview") {
                MainForm.PreviewOnly = true;
                MainForm.PreviewLanguage = args.Length >= 3 ? args[2] : null;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                using (var preview = new MainForm()) {
                    preview.ShowInTaskbar = false;
                    preview.Opacity = 0;
                    preview.Show();
                    preview.CreateControl();
                    preview.PerformLayout();
                    using (var bitmap = new System.Drawing.Bitmap(preview.Width, preview.Height)) {
                        preview.DrawToBitmap(bitmap, new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height));
                        bitmap.Save(args[1], System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
                return;
            }

            // Set the correct DLL for the current OS
            SetupDlls();

            using (Mutex mutex = new Mutex(false, "Global\\" + appGuid)) {
                if (!mutex.WaitOne(0, false)) {
                    MessageBox.Show("Instance already running.", "BetterJoy");
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                form = new MainForm();
                if (args.Length >= 2 && args[0] == "--diagnose") {
                    form.ShowInTaskbar = false;
                    form.Opacity = 0;
                    ConnectionDiagnostics.Run(form, args[1], args.Length > 2 ? Int32.Parse(args[2]) : 10);
                }
                Application.Run(form);
            }
        }

        static void SetupDlls() {
            string archPath = $"{AppDomain.CurrentDomain.BaseDirectory}{(Environment.Is64BitProcess ? "x64" : "x86")}\\";
            string pathVariable = Environment.GetEnvironmentVariable("PATH");
            pathVariable = $"{archPath};{pathVariable}";
            Environment.SetEnvironmentVariable("PATH", pathVariable);
        }

    }
}
