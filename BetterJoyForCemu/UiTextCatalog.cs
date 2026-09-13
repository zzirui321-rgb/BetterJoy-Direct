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
            "GyroLeftStick", "GyroRightStick", "GyroMouse"
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
                { "GuideBody", "01  退出旧版 BetterJoy。USB 插入即连；蓝牙请长按顶部同步键后在 Windows 配对。\r\n02  重连后松开所有按键，等待输入稳定，再打开“测试 Xbox 输入”检查。\r\n03  Steam 快捷键开启时，Capture 发送 F12，Home 发送 Shift+Tab。\r\n04  陀螺仪可直接映射摇杆或鼠标；普通按键和摇杆保持 Xbox 输入，在 Windows 桌面无响应属正常。" },
                { "SaveRestart", "保存并重启" },
                { "AutoReconnect", "自动重连" },
                { "StartInTray", "启动时最小化" },
                { "Exit", "退出" },
                { "TrayHint", "双击托盘图标恢复 BetterJoy Direct。" },
                { "WaitingStatus", "等待连接手柄 · USB 或蓝牙" },
                { "ConnectedStatus", "已连接 {0} 个手柄 · 松开唤醒按键后开始使用" },
                { "DriverUnavailable", "Xbox 驱动不可用 · 请检查 ViGEmBus 安装" },
                { "DriverReady", "Xbox 驱动已就绪 · 标准手柄输入正常 · Steam 快捷键已开启" },
                { "DriverReadyNoShortcuts", "Xbox 驱动已就绪 · 标准手柄输入正常 · Steam 快捷键已关闭" },
                { "LanguageButton", "English" },
                { "SettingSaveError", "无法保存设置；本次运行仍会使用当前选择。" },
                { "SteamShortcutsToggle", "Steam 快捷键（F12 / Shift+Tab）" },
                { "GyroModeLabel", "陀螺仪：" },
                { "GyroOff", "关闭" },
                { "GyroLeftStick", "映射左摇杆" },
                { "GyroRightStick", "映射右摇杆（推荐）" },
                { "GyroMouse", "映射鼠标" }
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
                { "GuideBody", "01  Close older BetterJoy versions. Connect by USB, or hold the top sync button and pair in Windows.\r\n02  After reconnecting, release every button and wait for stable input, then open “Test Xbox input”.\r\n03  With Steam shortcuts on, Capture sends F12 and Home sends Shift+Tab.\r\n04  Gyro can directly drive a stick or mouse. Regular buttons and sticks remain Xbox input, so no response on the Windows desktop is normal." },
                { "SaveRestart", "Save and restart" },
                { "AutoReconnect", "Auto reconnect" },
                { "StartInTray", "Start minimized" },
                { "Exit", "Exit" },
                { "TrayHint", "Double-click the tray icon to restore BetterJoy Direct." },
                { "WaitingStatus", "Waiting for a controller · USB or Bluetooth" },
                { "ConnectedStatus", "{0} controller(s) connected · release wake buttons before use" },
                { "DriverUnavailable", "Xbox driver unavailable · check the ViGEmBus installation" },
                { "DriverReady", "Xbox driver ready · standard controller input active · Steam shortcuts on" },
                { "DriverReadyNoShortcuts", "Xbox driver ready · standard controller input active · Steam shortcuts off" },
                { "LanguageButton", "中文" },
                { "SettingSaveError", "Could not save the setting; the current choice remains active for this run." },
                { "SteamShortcutsToggle", "Steam shortcuts (F12 / Shift+Tab)" },
                { "GyroModeLabel", "Gyro:" },
                { "GyroOff", "Off" },
                { "GyroLeftStick", "Map to left stick" },
                { "GyroRightStick", "Map to right stick (recommended)" },
                { "GyroMouse", "Map to mouse" }
            };
        }
    }
}
