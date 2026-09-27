using System;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BetterJoyForCemu {
    public partial class MainForm {
        private Label connectionStatus;
        private Label driverStatus;
        private Label guideText;
        private Label[] playerLabels;
        private Button[] slotButtons;
        private Button[] locateButtons;
        private Button bluetoothButton;
        private Button xboxTestButton;
        private Button languageButton;
        private CheckBox steamShortcutsBox;
        private Label gyroModeLabel;
        private ComboBox gyroModeBox;
        private TabPage guideTab;
        private TabPage logTab;
        private TabPage advancedTab;
        private Timer statusTimer;
        private UiTextCatalog uiText;
        private bool updatingQuickOptions;

        public static bool PreviewOnly;
        public static string PreviewLanguage;

        private void BuildModernInterface() {
            uiText = UiTextCatalog.ForCode(PreviewLanguage ?? ConfigurationManager.AppSettings["UiLanguage"]);

            SuspendLayout();
            AutoSize = false;
            AutoSizeMode = AutoSizeMode.GrowOnly;
            AutoScaleMode = AutoScaleMode.Dpi;
            DoubleBuffered = true;
            Font = new Font("Microsoft YaHei UI", 10);
            BackColor = Color.FromArgb(242, 245, 249);
            ForeColor = Color.FromArgb(27, 38, 57);
            ClientSize = new Size(900, 700);
            MinimumSize = new Size(760, 620);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            Controls.Clear();

            var root = new TableLayoutPanel {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                ColumnCount = 1,
                RowCount = 7
            };
            // Keep the original 980x820 composition while using a smaller default window.
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 174));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 29));
            Controls.Add(root);

            var heading = new Panel { Dock = DockStyle.Fill };
            heading.Controls.Add(new Label {
                Text = "BetterJoy Direct",
                Font = new Font("Segoe UI", 25, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 0)
            });
            heading.Controls.Add(new Label {
                Text = "SWITCH PRO  →  USB / BLUETOOTH  →  XBOX 360",
                AutoSize = true,
                ForeColor = Color.FromArgb(89, 105, 128),
                Location = new Point(2, 57)
            });
            root.Controls.Add(heading, 0, 0);

            var status = new Panel {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(224, 241, 237),
                Padding = new Padding(18)
            };
            connectionStatus = new Label {
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 12, FontStyle.Bold),
                Location = new Point(18, 12)
            };
            driverStatus = new Label { AutoSize = true, Location = new Point(18, 43) };
            status.Controls.Add(connectionStatus);
            status.Controls.Add(driverStatus);
            root.Controls.Add(status, 0, 1);

            var devices = new TableLayoutPanel {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                Padding = new Padding(0, 14, 0, 7)
            };
            slotButtons = new[] { con1, con2, con3, con4 };
            locateButtons = new[] { loc1, loc2, loc3, loc4 };
            playerLabels = new Label[4];
            for (int i = 0; i < 4; i++) {
                devices.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
                var card = new TableLayoutPanel {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    Margin = new Padding(0, 0, i == 3 ? 0 : 12, 0),
                    RowCount = 3
                };
                card.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
                card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                card.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
                playerLabels[i] = new Label {
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.FromArgb(89, 105, 128)
                };
                card.Controls.Add(playerLabels[i], 0, 0);

                slotButtons[i].Dock = DockStyle.Fill;
                slotButtons[i].Margin = new Padding(12, 2, 12, 2);
                slotButtons[i].BackgroundImageLayout = ImageLayout.Zoom;
                slotButtons[i].BackgroundImage = null;
                slotButtons[i].Font = new Font("Microsoft YaHei UI", 11);
                slotButtons[i].FlatStyle = FlatStyle.Flat;
                slotButtons[i].FlatAppearance.BorderSize = 0;
                slotButtons[i].BackColor = Color.White;
                card.Controls.Add(slotButtons[i], 0, 1);

                locateButtons[i].Dock = DockStyle.Fill;
                locateButtons[i].Margin = new Padding(12, 2, 12, 6);
                StyleButton(locateButtons[i]);
                card.Controls.Add(locateButtons[i], 0, 2);
                devices.Controls.Add(card, i, 0);
            }
            root.Controls.Add(devices, 0, 2);

            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
            bluetoothButton = new Button { Width = 147, Height = 35 };
            bluetoothButton.Click += (s, e) => Process.Start(new ProcessStartInfo("ms-settings:bluetooth") { UseShellExecute = true });
            xboxTestButton = new Button { Width = 138, Height = 35 };
            xboxTestButton.Click += (s, e) => Process.Start("joy.cpl");
            btn_open3rdP.Size = new Size(140, 35);
            AutoCalibrate.Size = new Size(128, 35);
            languageButton = new Button { Width = 96, Height = 35 };
            languageButton.Click += (s, e) => ToggleLanguage();
            foreach (var button in new[] { bluetoothButton, xboxTestButton, btn_open3rdP, AutoCalibrate, languageButton }) {
                StyleButton(button);
                actions.Controls.Add(button);
            }
            AutoCalibrate.Visible = allowCalibration;
            root.Controls.Add(actions, 0, 3);

            var quickOptions = new FlowLayoutPanel {
                Dock = DockStyle.Fill,
                WrapContents = true,
                BackColor = Color.White,
                Padding = new Padding(11, 7, 11, 4)
            };
            steamShortcutsBox = new CheckBox { AutoSize = true, Checked = RuntimeOptions.SteamShortcutsEnabled, Margin = new Padding(0, 2, 22, 0) };
            gyroModeLabel = new Label { AutoSize = true, Margin = new Padding(0, 4, 8, 0) };
            gyroModeBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 185, Margin = new Padding(0) };
            quickOptions.Controls.Add(steamShortcutsBox);
            quickOptions.Controls.Add(gyroModeLabel);
            quickOptions.Controls.Add(gyroModeBox);
            root.Controls.Add(quickOptions, 0, 4);

            steamShortcutsBox.CheckedChanged += (s, e) => {
                if (updatingQuickOptions) return;
                RuntimeOptions.SetSteamShortcuts(steamShortcutsBox.Checked);
                SaveAppSetting("EnableSteamShortcuts", steamShortcutsBox.Checked ? "true" : "false");
                RefreshConnectionStatus();
            };
            gyroModeBox.SelectedIndexChanged += (s, e) => {
                if (updatingQuickOptions || gyroModeBox.SelectedIndex < 0) return;
                string mode = GyroModeFromIndex(gyroModeBox.SelectedIndex);
                RuntimeOptions.SetGyroMode(mode);
                SaveAppSetting("GyroToJoyOrMouse", mode);
                RefreshConnectionStatus();
            };

            var tabs = new TabControl { Dock = DockStyle.Fill };
            guideTab = new TabPage { BackColor = Color.White, Padding = new Padding(16) };
            guideText = new Label { Dock = DockStyle.Fill };
            guideTab.Controls.Add(guideText);
            tabs.TabPages.Add(guideTab);

            logTab = new TabPage { Padding = new Padding(8), BackColor = Color.White };
            console.Dock = DockStyle.Fill;
            console.Font = new Font("Consolas", 10);
            console.BackColor = Color.FromArgb(24, 34, 49);
            console.ForeColor = Color.FromArgb(206, 224, 237);
            console.BorderStyle = BorderStyle.None;
            logTab.Controls.Add(console);
            tabs.TabPages.Add(logTab);

            advancedTab = new TabPage { Padding = new Padding(8), BackColor = Color.White };
            settingsTable.Dock = DockStyle.Top;
            settingsTable.AutoSize = true;
            settingsTable.ColumnStyles.Clear();
            settingsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
            settingsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            scroll.Controls.Add(settingsTable);
            settingsApply.Dock = DockStyle.Bottom;
            settingsApply.Height = 36;
            StyleButton(settingsApply);
            advancedTab.Controls.Add(scroll);
            advancedTab.Controls.Add(settingsApply);
            tabs.TabPages.Add(advancedTab);
            root.Controls.Add(tabs, 0, 5);

            var footer = new FlowLayoutPanel { Dock = DockStyle.Fill };
            passiveScanBox.AutoSize = true;
            startInTrayBox.AutoSize = true;
            footer.Controls.Add(passiveScanBox);
            footer.Controls.Add(startInTrayBox);
            root.Controls.Add(footer, 0, 6);

            ApplyLanguage();
            statusTimer = new Timer { Interval = 800 };
            statusTimer.Tick += (s, e) => RefreshConnectionStatus();
            statusTimer.Start();
            FormClosed += (s, e) => statusTimer.Dispose();
            ResumeLayout(true);
        }

        private static void StyleButton(Button button) {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = Color.FromArgb(227, 234, 243);
            button.ForeColor = Color.FromArgb(29, 58, 92);
            button.Font = new Font("Microsoft YaHei UI", 10);
            button.Cursor = Cursors.Hand;
        }

        private void ToggleLanguage() {
            uiText = uiText.Toggle();
            ApplyLanguage();
            SaveLanguagePreference();
        }

        private void ApplyLanguage() {
            Text = uiText["WindowTitle"];
            connectionStatus.Text = uiText["WaitingInitial"];
            driverStatus.Text = uiText["DriverChecking"];
            for (int i = 0; i < playerLabels.Length; i++) {
                playerLabels[i].Text = String.Format(uiText["Player"], i + 1);
                locateButtons[i].Text = uiText["Locate"];
                if (!slotButtons[i].Enabled) slotButtons[i].Text = uiText["Disconnected"];
            }
            bluetoothButton.Text = uiText["ConnectBluetooth"];
            xboxTestButton.Text = uiText["TestXbox"];
            btn_open3rdP.Text = uiText["ThirdParty"];
            AutoCalibrate.Text = uiText["Calibrate"];
            languageButton.Text = uiText["LanguageButton"];
            guideTab.Text = uiText["GuideTab"];
            logTab.Text = uiText["LogTab"];
            advancedTab.Text = uiText["AdvancedTab"];
            guideText.Text = uiText["GuideBody"];
            settingsApply.Text = uiText["SaveRestart"];
            passiveScanBox.Text = uiText["AutoReconnect"];
            startInTrayBox.Text = uiText["StartInTray"];
            steamShortcutsBox.Text = uiText["SteamShortcutsToggle"];
            gyroModeLabel.Text = uiText["GyroModeLabel"];
            RefreshGyroModeItems();
            exitToolStripMenuItem.Text = uiText["Exit"];
            notifyIcon.BalloonTipText = uiText["TrayHint"];
            RefreshConnectionStatus();
        }

        private void SaveLanguagePreference() {
            SaveAppSetting("UiLanguage", uiText.Code);
        }

        private bool SaveAppSetting(string key, string value) {
            try {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var setting = configFile.AppSettings.Settings[key];
                if (setting == null)
                    configFile.AppSettings.Settings.Add(key, value);
                else
                    setting.Value = value;
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                UpdateSettingsEditorValue(key, value);
                return true;
            } catch (ConfigurationErrorsException) {
                AppendTextBox(uiText["SettingSaveError"] + "\r\n");
            } catch (UnauthorizedAccessException) {
                AppendTextBox(uiText["SettingSaveError"] + "\r\n");
            } catch (IOException) {
                AppendTextBox(uiText["SettingSaveError"] + "\r\n");
            }
            return false;
        }

        private void UpdateSettingsEditorValue(string key, string value) {
            for (int row = 0; row < settingsTable.RowCount; row++) {
                Control keyControl = settingsTable.GetControlFromPosition(0, row);
                Control valueControl = settingsTable.GetControlFromPosition(1, row);
                if (keyControl == null || valueControl == null || keyControl.Text != key) continue;
                if (valueControl is TextBox) {
                    valueControl.Text = value;
                    return;
                }
                if (valueControl is CheckBox) {
                    bool enabled;
                    if (Boolean.TryParse(value, out enabled)) ((CheckBox)valueControl).Checked = enabled;
                    return;
                }
            }
        }

        private void RefreshGyroModeItems() {
            updatingQuickOptions = true;
            try {
                gyroModeBox.Items.Clear();
                gyroModeBox.Items.Add(uiText["GyroOff"]);
                gyroModeBox.Items.Add(uiText["GyroLeftStick"]);
                gyroModeBox.Items.Add(uiText["GyroRightStick"]);
                gyroModeBox.Items.Add(uiText["GyroMouse"]);
                gyroModeBox.SelectedIndex = GyroModeToIndex(RuntimeOptions.GyroMode);
            } finally {
                updatingQuickOptions = false;
            }
        }

        private static int GyroModeToIndex(string mode) {
            if (mode == RuntimeOptions.GyroLeftStick) return 1;
            if (mode == RuntimeOptions.GyroRightStick) return 2;
            if (mode == RuntimeOptions.GyroMouse) return 3;
            return 0;
        }

        private static string GyroModeFromIndex(int index) {
            if (index == 1) return RuntimeOptions.GyroLeftStick;
            if (index == 2) return RuntimeOptions.GyroRightStick;
            if (index == 3) return RuntimeOptions.GyroMouse;
            return RuntimeOptions.GyroOff;
        }

        public string GetUiText(string key) {
            return uiText == null ? key : uiText[key];
        }

        public void SetSlotDisconnected(Button button) {
            if (button != null) button.Text = GetUiText("Disconnected");
        }

        private void RefreshConnectionStatus() {
            int count = Program.mgr == null ? 0 : Program.mgr.j.Count(j => j.state > Joycon.state_.DROPPED);
            connectionStatus.Text = count == 0
                ? uiText["WaitingStatus"]
                : String.Format(uiText["ConnectedStatus"], count);

            driverStatus.Text = Program.emClient == null
                ? uiText["DriverUnavailable"]
                : (RuntimeOptions.SteamShortcutsEnabled ? uiText["DriverReady"] : uiText["DriverReadyNoShortcuts"]);
        }
    }
}
