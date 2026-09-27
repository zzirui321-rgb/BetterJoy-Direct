using System;
using System.Collections.Generic;

namespace BetterJoyForCemu {
    public sealed class UiTextCatalog {
        public static readonly string[] RequiredKeys = {
            "WindowTitle", "WaitingInitial", "DriverChecking", "Player", "Disconnected", "Locate",
            "ConnectBluetooth", "TestXbox", "ThirdParty", "Calibrate", "GuideTab",
            "LogTab", "AdvancedTab", "GuideBody", "SaveRestart", "AutoReconnect", "StartInTray",
            "Exit", "TrayHint", "WaitingStatus", "ConnectedStatus", "DriverUnavailable",
            "DriverReady", "DriverReadyNoShortcuts", "LanguageButton", "SettingSaveError",
            "SteamShortcutsToggle", "GyroModeLabel", "GyroOff",
            "GyroLeftStick", "GyroRightStick", "GyroMouse", "DriverInstallTitle",
            "DriverInstallPrompt", "DriverInstallerMissing", "DriverInstallFailed",
            "DriverRestartRequired", "DriverInstallRetryFailed"
        };

        private readonly IDictionary<string, string> values;

        public string Code { get; private set; }
        public bool IsChinese { get { return Code == "zh-CN"; } }

        public string this[string key] {
            get {
                string value;
                return values.TryGetValue(key, out value) ? value : key;
            }
        }

        private UiTextCatalog(string code, IDictionary<string, string> values) {
            Code = code;
            this.values = values;
        }

        public static UiTextCatalog ForCode(string code) {
            if (!String.IsNullOrWhiteSpace(code) && code.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                return new UiTextCatalog("en-US", English());
            return new UiTextCatalog("zh-CN", Chinese());
        }

        public UiTextCatalog Toggle() {
            return ForCode(IsChinese ? "en-US" : "zh-CN");
        }

        public bool Contains(string key) {
            return values.ContainsKey(key);
        }

        private static IDictionary<string, string> Chinese() {
            return new Dictionary<string, string>(StringComparer.Ordinal) {
                { "WindowTitle", "BetterJoy Direct · Switch → Xbox" },
                { "WaitingInitial", "等待连接手柄" },
                { "DriverChecking", "正在检查 Xbox 虚拟手柄驱动…" },
                { "Player", "玩家 {0}" },
                { "Disconnected", "未连接" },
                { "Locate", "轻震定位" },
                { "ConnectBluetooth", "连接蓝牙手柄" },
                { "TestXbox", "测试 Xbox 输入" },
                { "ThirdParty", "第三方手柄" },
                { "Calibrate", "陀螺仪校准" },
                { "GuideTab", "直连指南" },
                { "LogTab", "连接日志" },
                { "AdvancedTab", "高级设置" },
                { "GuideBody", "01  首次启动若缺少 ViGEmBus，请确认安装提示并通过 Windows UAC；安装过程不会静默执行。\r\n02  退出旧版 BetterJoy。USB 插入即连；蓝牙请长按顶部同步键后在 Windows 配对。\r\n03  重连后松开所有按键，等待输入稳定，再打开“测试 Xbox 输入”检查。\r\n04  Steam 快捷键开启时，Capture 发送 F12，Home 发送 Shift+Tab；陀螺仪可直接映射摇杆或鼠标。" },
                { "SaveRestart", "保存并重启" },
                { "AutoReconnect", "自动重连" },
                { "StartInTray", "启动时最小化" },
                { "Exit", "退出" },
                { "TrayHint", "双击托盘图标恢复 BetterJoy Direct。" },
                { "WaitingStatus", "等待连接手柄 · USB 或蓝牙" },
                { "ConnectedStatus", "已连接 {0} 个手柄 · 松开唤醒按键后开始使用" },
                { "DriverUnavailable", "Xbox 驱动不可用 · 请安装 ViGEmBus 后重启" },
                { "DriverReady", "Xbox 驱动已就绪 · 标准手柄输入正常 · Steam 快捷键已开启" },
                { "DriverReadyNoShortcuts", "Xbox 驱动已就绪 · 标准手柄输入正常 · Steam 快捷键已关闭" },
                { "LanguageButton", "English" },
                { "SettingSaveError", "无法保存设置；本次运行仍会使用当前选择。" },
                { "SteamShortcutsToggle", "Steam 快捷键（F12 / Shift+Tab）" },
                { "GyroModeLabel", "陀螺仪：" },
                { "GyroOff", "关闭" },
                { "GyroLeftStick", "映射左摇杆" },
                { "GyroRightStick", "映射右摇杆（推荐）" },
                { "GyroMouse", "映射鼠标" },
                { "DriverInstallTitle", "安装 Xbox 虚拟手柄驱动" },
                { "DriverInstallPrompt", "BetterJoy Direct 需要 ViGEmBus 才能创建虚拟 Xbox 手柄。\r\n\r\n完整发布包内含由 Nefarius 签名的 ViGEmBus 1.17.333.0。该项目已停止维护。是否现在启动安装程序？\r\n\r\nWindows 将显示管理员权限提示；驱动不会静默安装。" },
                { "DriverInstallerMissing", "未找到随包驱动安装程序：\r\n{0}\r\n\r\n请重新下载并完整解压 BetterJoy Direct。" },
                { "DriverInstallFailed", "ViGEmBus 安装程序未成功完成（退出代码：{0}）。请打开 Drivers 文件夹手动运行安装程序。" },
                { "DriverRestartRequired", "ViGEmBus 安装已经完成，但 Windows 要求重新启动。请重启 Windows 后再次运行 BetterJoy Direct。" },
                { "DriverInstallRetryFailed", "安装程序已结束，但 ViGEmBus 仍不可用。请重启 BetterJoy Direct；若仍失败，请重启 Windows。" }
            };
        }

        private static IDictionary<string, string> English() {
            return new Dictionary<string, string>(StringComparer.Ordinal) {
                { "WindowTitle", "BetterJoy Direct · Switch → Xbox" },
                { "WaitingInitial", "Waiting for a controller" },
                { "DriverChecking", "Checking the Xbox virtual controller driver…" },
                { "Player", "PLAYER {0}" },
                { "Disconnected", "Disconnected" },
                { "Locate", "Gentle locate" },
                { "ConnectBluetooth", "Connect Bluetooth" },
                { "TestXbox", "Test Xbox input" },
                { "ThirdParty", "Other controllers" },
                { "Calibrate", "Calibrate gyro" },
                { "GuideTab", "Direct connection" },
                { "LogTab", "Connection log" },
                { "AdvancedTab", "Advanced" },
                { "GuideBody", "01  If ViGEmBus is missing on first launch, confirm the installer prompt and Windows UAC; installation is never silent.\r\n02  Close older BetterJoy versions. Connect by USB, or hold the top sync button and pair in Windows.\r\n03  After reconnecting, release every button and wait for stable input, then open “Test Xbox input”.\r\n04  Capture sends F12 and Home sends Shift+Tab when Steam shortcuts are on; gyro can drive a stick or mouse." },
                { "SaveRestart", "Save and restart" },
                { "AutoReconnect", "Auto reconnect" },
                { "StartInTray", "Start minimized" },
                { "Exit", "Exit" },
                { "TrayHint", "Double-click the tray icon to restore BetterJoy Direct." },
                { "WaitingStatus", "Waiting for a controller · USB or Bluetooth" },
                { "ConnectedStatus", "{0} controller(s) connected · release wake buttons before use" },
                { "DriverUnavailable", "Xbox driver unavailable · install ViGEmBus and restart" },
                { "DriverReady", "Xbox driver ready · standard controller input active · Steam shortcuts on" },
                { "DriverReadyNoShortcuts", "Xbox driver ready · standard controller input active · Steam shortcuts off" },
                { "LanguageButton", "中文" },
                { "SettingSaveError", "Could not save the setting; the current choice remains active for this run." },
                { "SteamShortcutsToggle", "Steam shortcuts (F12 / Shift+Tab)" },
                { "GyroModeLabel", "Gyro:" },
                { "GyroOff", "Off" },
                { "GyroLeftStick", "Map to left stick" },
                { "GyroRightStick", "Map to right stick (recommended)" },
                { "GyroMouse", "Map to mouse" },
                { "DriverInstallTitle", "Install the Xbox virtual controller driver" },
                { "DriverInstallPrompt", "BetterJoy Direct requires ViGEmBus to create a virtual Xbox controller.\r\n\r\nThe complete release includes ViGEmBus 1.17.333.0 signed by Nefarius. The project has been retired. Start the installer now?\r\n\r\nWindows will request administrator approval; the driver is never installed silently." },
                { "DriverInstallerMissing", "The bundled driver installer was not found:\r\n{0}\r\n\r\nDownload BetterJoy Direct again and extract the complete archive." },
                { "DriverInstallFailed", "The ViGEmBus installer did not complete successfully (exit code: {0}). Open the Drivers folder and run the installer manually." },
                { "DriverRestartRequired", "ViGEmBus installation completed, but Windows requires a restart. Restart Windows, then run BetterJoy Direct again." },
                { "DriverInstallRetryFailed", "The installer finished, but ViGEmBus is still unavailable. Restart BetterJoy Direct; if that fails, restart Windows." }
            };
        }
    }
}
