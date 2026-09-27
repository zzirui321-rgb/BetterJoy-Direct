using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using BetterJoyForCemu;

class FeatureRegressionTests {
    static void Fail(string message) {
        Console.Error.WriteLine("FAIL: " + message);
        Environment.Exit(1);
    }

    [STAThread]
    static void Main() {
        var profile = LocateRumbleProfile.FromConfiguration();
        if (profile.Strength > 0.25f)
            Fail("Locate rumble is too strong: " + profile.Strength);
        if (profile.DurationMs > 180)
            Fail("Locate rumble lasts too long: " + profile.DurationMs + "ms");
        if (Math.Abs(profile.Strength - 0.18f) > 0.001f || profile.DurationMs != 120)
            Fail("Packaged locate defaults changed from the gentle 0.18 / 120ms profile");
        if (profile.LowFrequency < 40.875885f || profile.HighFrequency < 81.75177f)
            Fail("Locate rumble frequencies are outside the controller range");

        var chinese = UiTextCatalog.ForCode("zh-CN");
        var english = UiTextCatalog.ForCode("en-US");
        foreach (string key in UiTextCatalog.RequiredKeys) {
            if (!chinese.Contains(key) || String.IsNullOrWhiteSpace(chinese[key]))
                Fail("Missing Chinese UI text: " + key);
            if (!english.Contains(key) || String.IsNullOrWhiteSpace(english[key]))
                Fail("Missing English UI text: " + key);
        }
        if (chinese.Toggle().Code != "en-US" || english.Toggle().Code != "zh-CN")
            Fail("Language toggle does not switch both ways");
        if (UiTextCatalog.ForCode(null).Code != "zh-CN" || UiTextCatalog.ForCode("en-GB").Code != "en-US")
            Fail("Language normalization is inconsistent");
        if (chinese["LanguageButton"] == english["LanguageButton"])
            Fail("Language button does not expose the target language");

        Type installerType = typeof(UiTextCatalog).Assembly.GetType("BetterJoyForCemu.ViGEmBusInstaller");
        if (installerType == null)
            Fail("ViGEmBus installer helper was not included in the application");
        MethodInfo getInstallerPath = installerType.GetMethod("GetInstallerPath", BindingFlags.Static | BindingFlags.NonPublic);
        if (getInstallerPath == null)
            Fail("ViGEmBus installer path resolver was not found");
        string fakeBase = Path.Combine(Path.GetTempPath(), "BetterJoy-Direct-driver-test");
        string resolvedInstallerPath = (string)getInstallerPath.Invoke(null, new object[] { fakeBase });
        string expectedInstallerPath = Path.Combine(fakeBase, "Drivers", "ViGEmBusSetup_x64.msi");
        if (!String.Equals(resolvedInstallerPath, expectedInstallerPath, StringComparison.OrdinalIgnoreCase))
            Fail("ViGEmBus installer path resolved incorrectly: " + resolvedInstallerPath);

        MainForm.PreviewOnly = true;
        MainForm.PreviewLanguage = "zh-CN";
        using (var form = new MainForm()) {
            form.ShowInTaskbar = false;
            form.Opacity = 0;
            form.Show();
            Application.DoEvents();
            var field = typeof(MainForm).GetField("languageButton", BindingFlags.Instance | BindingFlags.NonPublic);
            var button = field == null ? null : field.GetValue(form) as Button;
            if (button == null) Fail("Language button was not created");
            string before = form.GetUiText("Disconnected");
            button.PerformClick();
            string after = form.GetUiText("Disconnected");
            if (before == after || after != english["Disconnected"])
                Fail("Language button did not refresh the live interface");

            var showFromTray = typeof(MainForm).GetMethod("ShowFromTray", BindingFlags.Instance | BindingFlags.NonPublic);
            if (showFromTray == null) Fail("Tray restore method was not found");
            form.Hide();
            form.WindowState = FormWindowState.Minimized;
            form.ShowInTaskbar = false;
            bool sawRestoreVisible = false;
            bool restoreWasReadyBeforeVisible = false;
            EventHandler visibleChanged = (s, e) => {
                if (!form.Visible) return;
                sawRestoreVisible = true;
                restoreWasReadyBeforeVisible = form.WindowState == FormWindowState.Normal && form.ShowInTaskbar;
            };
            form.VisibleChanged += visibleChanged;
            showFromTray.Invoke(form, null);
            Application.DoEvents();
            form.VisibleChanged -= visibleChanged;
            if (!sawRestoreVisible || !restoreWasReadyBeforeVisible)
                Fail("Tray restore exposed the window before its normal/taskbar state was ready");

            Size initialClientSize = form.ClientSize;
            if (form.AutoSize || form.AutoSizeMode != AutoSizeMode.GrowOnly || form.FormBorderStyle != FormBorderStyle.Sizable || !form.MaximizeBox)
                Fail("Window chrome or autosizing prevents normal resizing");
            if (initialClientSize != new Size(900, 700))
                Fail("Default window size changed unexpectedly: " + initialClientSize);
            var rootLayout = form.Controls.Count == 0 ? null : form.Controls[0] as TableLayoutPanel;
            if (rootLayout == null || rootLayout.RowStyles.Count != 7)
                Fail("Modern root layout was not created");
            double headingToStatus = rootLayout.RowStyles[0].Height / rootLayout.RowStyles[1].Height;
            double devicesToActions = rootLayout.RowStyles[2].Height / rootLayout.RowStyles[3].Height;
            double actionsToOptions = rootLayout.RowStyles[3].Height / rootLayout.RowStyles[4].Height;
            if (Math.Abs(headingToStatus - 96.0 / 84.0) > 0.03 ||
                Math.Abs(devicesToActions - 204.0 / 56.0) > 0.08 ||
                Math.Abs(actionsToOptions - 56.0 / 52.0) > 0.03)
                Fail("Internal section proportions drifted from the original interface");
            if (initialClientSize.Width - form.MinimumSize.Width < 100 || initialClientSize.Height - form.MinimumSize.Height < 80)
                Fail("Minimum window size leaves too little resize range");
            Size smallerClientSize = new Size(initialClientSize.Width - 100, initialClientSize.Height - 80);
            form.ClientSize = smallerClientSize;
            Application.DoEvents();
            if (form.ClientSize != smallerClientSize)
                Fail("Window rejected a valid smaller client size");

            var desktopField = typeof(MainForm).GetField("desktopMappingsBox", BindingFlags.Instance | BindingFlags.NonPublic);
            var mappingButtonField = typeof(MainForm).GetField("btn_reassign_open", BindingFlags.Instance | BindingFlags.NonPublic);
            var shortcutsField = typeof(MainForm).GetField("steamShortcutsBox", BindingFlags.Instance | BindingFlags.NonPublic);
            var gyroField = typeof(MainForm).GetField("gyroModeBox", BindingFlags.Instance | BindingFlags.NonPublic);
            var mappingButton = mappingButtonField == null ? null : mappingButtonField.GetValue(form) as Button;
            var shortcutsBox = shortcutsField == null ? null : shortcutsField.GetValue(form) as CheckBox;
            var gyroBox = gyroField == null ? null : gyroField.GetValue(form) as ComboBox;
            if (desktopField != null || mappingButton == null || mappingButton.Parent != null)
                Fail("Removed desktop mapping controls are still exposed");
            if (shortcutsBox == null || gyroBox == null)
                Fail("Quick input controls were not created");
            if (!shortcutsBox.Checked || gyroBox.SelectedIndex != 0)
                Fail("Quick input controls do not match safe packaged defaults");

            shortcutsBox.Checked = !shortcutsBox.Checked;
            gyroBox.SelectedIndex = 2;
            Application.DoEvents();
            if (RuntimeOptions.SteamShortcutsEnabled || RuntimeOptions.GyroMode != RuntimeOptions.GyroRightStick)
                Fail("Quick input controls did not update runtime behavior");
        }

        Console.WriteLine("PASS: gentle locate profile " + profile.Strength + " for " + profile.DurationMs + "ms");
        Console.WriteLine("PASS: " + UiTextCatalog.RequiredKeys.Length + " bilingual UI strings and two-way toggle");
        Console.WriteLine("PASS: bundled ViGEmBus installer path resolves under Drivers");
        Console.WriteLine("PASS: live language button refresh and preference save");
        Console.WriteLine("PASS: desktop mapping UI removed; Steam shortcut and gyro controls remain live");
        Console.WriteLine("PASS: resize range and paint-ready tray restore");
    }
}
