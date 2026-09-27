using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace BetterJoyForCemu {
    internal static class ViGEmBusInstaller {
        internal const string InstallerRelativePath = @"Drivers\ViGEmBusSetup_x64.msi";

        internal static string GetInstallerPath(string baseDirectory) {
            return Path.Combine(baseDirectory, InstallerRelativePath);
        }

        internal static bool PromptAndInstall(MainForm form) {
            string installerPath = GetInstallerPath(AppDomain.CurrentDomain.BaseDirectory);
            string title = form.GetUiText("DriverInstallTitle");
            if (!File.Exists(installerPath)) {
                MessageBox.Show(form,
                    String.Format(form.GetUiText("DriverInstallerMissing"), installerPath),
                    title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (MessageBox.Show(form, form.GetUiText("DriverInstallPrompt"), title,
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return false;

            try {
                var startInfo = new ProcessStartInfo {
                    FileName = "msiexec.exe",
                    Arguments = "/i \"" + installerPath + "\" /norestart",
                    WorkingDirectory = Path.GetDirectoryName(installerPath),
                    UseShellExecute = true,
                    Verb = "runas"
                };
                int exitCode;
                using (Process installer = Process.Start(startInfo)) {
                    if (installer == null) return false;
                    installer.WaitForExit();
                    exitCode = installer.ExitCode;
                }

                if (exitCode == 0) return true;
                if (exitCode == 1641 || exitCode == 3010) {
                    MessageBox.Show(form, form.GetUiText("DriverRestartRequired"), title,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }

                MessageBox.Show(form,
                    String.Format(form.GetUiText("DriverInstallFailed"), exitCode),
                    title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            } catch (Win32Exception e) {
                // Windows returns 1223 when the user cancels the UAC prompt.
                if (e.NativeErrorCode != 1223)
                    MessageBox.Show(form,
                        String.Format(form.GetUiText("DriverInstallFailed"), e.NativeErrorCode),
                        title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            } catch (Exception) {
                MessageBox.Show(form,
                    String.Format(form.GetUiText("DriverInstallFailed"), "unknown"),
                    title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return false;
        }
    }
}
