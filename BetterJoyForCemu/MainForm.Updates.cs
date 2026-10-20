using System;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BetterJoyForCemu {
    public partial class MainForm {
        private Label projectIdentity;
        private LinkLabel projectLink;
        private Button updateButton;
        private CheckBox checkUpdatesBox;
        private GitHubRelease latestRelease;
        private bool checkingUpdate;
        private bool updateCheckFailed;
        private HttpClient updateClient;
        private readonly CancellationTokenSource updateLifetime = new CancellationTokenSource();

        private Panel CreateProjectPanel() {
            var panel = new Panel { Dock = DockStyle.Right, Width = 300 };
            projectIdentity = new Label { AutoSize = true, Location = new Point(0, 2), Font = new Font("Microsoft YaHei UI", 9) };
            projectLink = new LinkLabel { AutoSize = true, Text = "GitHub ↗", Location = new Point(0, 29), Tag = ProjectInfo.RepositoryUrl };
            projectLink.LinkClicked += (s, e) => OpenProjectPage(ProjectInfo.RepositoryUrl);
            updateButton = new Button { Width = 185, Height = 28, Location = new Point(100, 25) };
            StyleButton(updateButton);
            updateButton.Click += async (s, e) => {
                if (latestRelease != null && latestRelease.CompareToCurrent() > 0) ShowUpdateInstructions();
                else await CheckUpdatesAsync(true);
            };
            panel.Controls.Add(projectIdentity);
            panel.Controls.Add(projectLink);
            panel.Controls.Add(updateButton);
            return panel;
        }

        private void InitializeUpdateChecks(FlowLayoutPanel footer) {
            bool enabled;
            if (!Boolean.TryParse(ConfigurationManager.AppSettings["CheckForUpdates"], out enabled)) enabled = true;
            checkUpdatesBox = new CheckBox { AutoSize = true, Checked = enabled };
            checkUpdatesBox.CheckedChanged += (s, e) => SaveAppSetting("CheckForUpdates", checkUpdatesBox.Checked ? "true" : "false");
            footer.Controls.Add(checkUpdatesBox);
            Shown += async (s, e) => {
                if (!PreviewOnly && !ConnectionDiagnostics.IsRunning && checkUpdatesBox.Checked)
                    await CheckUpdatesAsync(false);
            };
            FormClosing += (s, e) => updateLifetime.Cancel();
            FormClosed += (s, e) => {
                updateLifetime.Cancel();
                if (updateClient != null) updateClient.Dispose();
                updateLifetime.Dispose();
            };
        }

        private void RefreshProjectInfo() {
            projectIdentity.Text = String.Format(uiText["ProjectIdentity"], ProjectInfo.Author, ProjectInfo.Version);
            checkUpdatesBox.Text = uiText["CheckUpdatesAtStartup"];
            updateButton.Enabled = !checkingUpdate;
            updateButton.Text = checkingUpdate ? uiText["UpdateChecking"]
                : latestRelease != null && latestRelease.CompareToCurrent() > 0
                    ? String.Format(uiText["UpdateAvailable"], latestRelease.Version)
                    : updateCheckFailed ? uiText["UpdateRetry"] : uiText["CheckUpdates"];
            updateButton.BackColor = latestRelease != null && latestRelease.CompareToCurrent() > 0
                ? Color.FromArgb(255, 222, 157) : Color.FromArgb(227, 234, 243);
        }

        private async Task CheckUpdatesAsync(bool manual) {
            if (checkingUpdate || updateLifetime.IsCancellationRequested) return;
            checkingUpdate = true;
            updateCheckFailed = false;
            RefreshProjectInfo();
            try {
                if (updateClient == null)
                    updateClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
                latestRelease = await new GitHubReleaseChecker(updateClient).CheckAsync(updateLifetime.Token);
                if (IsDisposed || Disposing || updateLifetime.IsCancellationRequested) return;
                if (latestRelease.CompareToCurrent() > 0) {
                    AppendTextBox(String.Format(uiText["UpdateAvailable"], latestRelease.Version) + "\r\n");
                    if (manual) ShowUpdateInstructions();
                    else if (!Visible || WindowState == FormWindowState.Minimized) {
                        notifyIcon.BalloonTipTitle = "BetterJoy Direct";
                        notifyIcon.BalloonTipText = String.Format(uiText["UpdateAvailable"], latestRelease.Version) + " · " + uiText["UpdateTrayHint"];
                        notifyIcon.ShowBalloonTip(5000);
                    }
                } else if (manual) {
                    string key = latestRelease.CompareToCurrent() == 0 ? "UpdateCurrent" : "UpdateLocalNewer";
                    MessageBox.Show(this, String.Format(uiText[key], ProjectInfo.Version, latestRelease.Version),
                        uiText["CheckUpdates"], MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            } catch (Exception) {
                if (IsDisposed || Disposing || updateLifetime.IsCancellationRequested) return;
                updateCheckFailed = true;
                AppendTextBox(uiText["UpdateFailed"] + "\r\n");
                if (manual) MessageBox.Show(this, uiText["UpdateFailed"], uiText["CheckUpdates"], MessageBoxButtons.OK, MessageBoxIcon.Information);
            } finally {
                checkingUpdate = false;
                if (!IsDisposed && !Disposing && !updateLifetime.IsCancellationRequested) RefreshProjectInfo();
            }
        }

        private string BuildUpdateInstructions() {
            return String.Format(uiText["UpdateVersions"], ProjectInfo.Version, latestRelease.Version) + "\r\n\r\n" +
                (latestRelease.HasPortableAsset ? uiText["UpdateInstructions"] : uiText["UpdateAssetMissing"]) +
                "\r\n\r\n" + uiText["UpdateOpenPrompt"];
        }

        private void ShowUpdateInstructions() {
            if (MessageBox.Show(this, BuildUpdateInstructions(), uiText["CheckUpdates"], MessageBoxButtons.YesNo,
                MessageBoxIcon.Information) == DialogResult.Yes) OpenProjectPage(latestRelease.PageUrl);
        }

        private void OpenProjectPage(string url) {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch (Exception) {
                MessageBox.Show(this, uiText["OpenPageFailed"] + "\r\n" + url, "BetterJoy Direct", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
