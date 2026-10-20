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
        private Label projectVersion;
        private LinkLabel projectLink;
        private LinkLabel projectVideoLink;
        private Button updateButton;
        private CheckBox checkUpdatesBox;
        private LinkLabel updateGuideLink;
        private GitHubRelease latestRelease;
        private bool checkingUpdate;
        private bool updateCheckFailed;
        private HttpClient updateClient;
        private readonly CancellationTokenSource updateLifetime = new CancellationTokenSource();

        private Panel CreateProjectPanel() {
            var panel = new Panel { Dock = DockStyle.Right, Width = 300, BackColor = Color.White };
            panel.Paint += (sender, args) => ControlPaint.DrawBorder(args.Graphics, panel.ClientRectangle,
                Color.FromArgb(220, 227, 236), ButtonBorderStyle.Solid);
            projectIdentity = new Label {
                AutoSize = true, Location = new Point(12, 8), Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold)
            };
            projectVersion = new Label {
                AutoSize = false, Location = new Point(216, 8), Size = new Size(70, 22),
                TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(234, 240, 247),
                ForeColor = Color.FromArgb(29, 58, 92), Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            projectLink = new LinkLabel {
                AutoSize = true, Text = "GitHub ↗", Location = new Point(12, 33), Tag = ProjectInfo.RepositoryUrl,
                Font = new Font("Microsoft YaHei UI", 9), LinkBehavior = LinkBehavior.HoverUnderline,
                LinkColor = Color.FromArgb(29, 80, 145)
            };
            projectLink.LinkClicked += (sender, args) => OpenProjectPage(ProjectInfo.RepositoryUrl);
            projectVideoLink = new LinkLabel {
                AutoSize = true, Location = new Point(12, 54), Tag = ProjectInfo.VideoUrl,
                Font = new Font("Microsoft YaHei UI", 9), LinkBehavior = LinkBehavior.HoverUnderline,
                LinkColor = Color.FromArgb(29, 80, 145)
            };
            projectVideoLink.LinkClicked += (sender, args) => OpenProjectPage(ProjectInfo.VideoUrl);
            updateButton = new Button { Width = 160, Height = 30, Location = new Point(127, 38) };
            StyleButton(updateButton);
            updateButton.Font = new Font("Microsoft YaHei UI", 9);
            updateButton.Click += async (s, e) => {
                if (latestRelease != null && latestRelease.CompareToCurrent() > 0) ShowUpdateInstructions();
                else await CheckUpdatesAsync(true);
            };
            panel.Controls.Add(projectIdentity);
            panel.Controls.Add(projectVersion);
            panel.Controls.Add(projectLink);
            panel.Controls.Add(projectVideoLink);
            panel.Controls.Add(updateButton);
            return panel;
        }

        private void InitializeUpdateChecks(FlowLayoutPanel footer) {
            bool enabled;
            if (!Boolean.TryParse(ConfigurationManager.AppSettings["CheckForUpdates"], out enabled)) enabled = true;
            checkUpdatesBox = new CheckBox { AutoSize = true, Checked = enabled };
            checkUpdatesBox.CheckedChanged += (s, e) => SaveAppSetting("CheckForUpdates", checkUpdatesBox.Checked ? "true" : "false");
            footer.Controls.Add(checkUpdatesBox);
            updateGuideLink = new LinkLabel {
                AutoSize = true, Margin = new Padding(12, 1, 0, 0), Font = new Font("Microsoft YaHei UI", 9),
                LinkBehavior = LinkBehavior.HoverUnderline, LinkColor = Color.FromArgb(29, 80, 145)
            };
            updateGuideLink.LinkClicked += (sender, args) => MessageBox.Show(this, uiText["UpdateInstructions"],
                uiText["UpdateGuide"], MessageBoxButtons.OK, MessageBoxIcon.Information);
            footer.Controls.Add(updateGuideLink);
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
            projectIdentity.Text = String.Format(uiText["ProjectIdentity"], ProjectInfo.Author);
            projectVersion.Text = "v" + ProjectInfo.Version;
            projectVideoLink.Text = uiText["VideoLink"];
            updateGuideLink.Text = uiText["UpdateGuide"];
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
