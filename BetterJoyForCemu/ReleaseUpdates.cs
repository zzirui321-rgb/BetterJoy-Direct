using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace BetterJoyForCemu {
    public static class ProjectInfo {
        public const string Version = "0.7.4";
        public const string AssemblyVersion = "0.7.4.0";
        public const string Author = "蓝大丶";
        public const string VideoUrl = "https://www.bilibili.com/video/BV1XUaY6hE5w";
        public const string RepositoryUrl = "https://github.com/zzirui321-rgb/BetterJoy-Direct";
        public const string ReleasesUrl = RepositoryUrl + "/releases";
        public const string LatestReleaseApi = "https://api.github.com/repos/zzirui321-rgb/BetterJoy-Direct/releases/latest";
        public const string PortableAssetName = "BetterJoy-Direct-win-x64.zip";
    }

    public sealed class ReleaseVersion : IComparable<ReleaseVersion> {
        private static readonly Regex Format = new Regex(@"^(?:direct-)?v?(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?\z", RegexOptions.CultureInvariant);
        private readonly int major, minor, patch;
        private readonly string[] prerelease;
        public bool IsPrerelease { get { return prerelease.Length != 0; } }
        private ReleaseVersion(int major, int minor, int patch, string[] prerelease) {
            this.major = major; this.minor = minor; this.patch = patch; this.prerelease = prerelease;
        }
        public static bool TryParse(string text, out ReleaseVersion version) {
            version = null;
            if (String.IsNullOrEmpty(text) || text.Length > 100) return false;
            Match match = Format.Match(text);
            int a, b, c;
            if (!match.Success || !Int32.TryParse(match.Groups[1].Value, out a) ||
                !Int32.TryParse(match.Groups[2].Value, out b) || !Int32.TryParse(match.Groups[3].Value, out c)) return false;
            string[] pre = match.Groups[4].Success ? match.Groups[4].Value.Split('.') : new string[0];
            foreach (string part in pre)
                if (IsNumeric(part) && part.Length > 1 && part[0] == '0') return false;
            version = new ReleaseVersion(a, b, c, pre);
            return true;
        }
        private static bool IsNumeric(string value) {
            foreach (char c in value) if (c < '0' || c > '9') return false;
            return value.Length != 0;
        }
        public int CompareTo(ReleaseVersion other) {
            if (other == null) return 1;
            int result = major.CompareTo(other.major);
            if (result == 0) result = minor.CompareTo(other.minor);
            if (result == 0) result = patch.CompareTo(other.patch);
            if (result != 0) return result;
            if (!IsPrerelease || !other.IsPrerelease) return other.IsPrerelease.CompareTo(IsPrerelease);
            for (int i = 0; i < Math.Min(prerelease.Length, other.prerelease.Length); i++) {
                string a = prerelease[i], b = other.prerelease[i];
                bool na = IsNumeric(a), nb = IsNumeric(b);
                result = na && nb ? (a.Length == b.Length ? String.CompareOrdinal(a, b) : a.Length.CompareTo(b.Length))
                    : na != nb ? (na ? -1 : 1) : String.CompareOrdinal(a, b);
                if (result != 0) return result;
            }
            return prerelease.Length.CompareTo(other.prerelease.Length);
        }
        public override string ToString() {
            string core = String.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}", major, minor, patch);
            return IsPrerelease ? core + "-" + String.Join(".", prerelease) : core;
        }
    }

    public sealed class GitHubRelease {
        public string Tag { get; private set; }
        public ReleaseVersion Version { get; private set; }
        public string PageUrl { get; private set; }
        public bool HasPortableAsset { get; private set; }
        public int CompareToCurrent() {
            ReleaseVersion current;
            if (!ReleaseVersion.TryParse(ProjectInfo.Version, out current)) throw new InvalidOperationException("Invalid application version.");
            return Version.CompareTo(current);
        }
        public static GitHubRelease Parse(string json) {
            if (String.IsNullOrEmpty(json) || json.Length > GitHubReleaseChecker.MaxResponseBytes)
                throw new InvalidDataException("Invalid release response size.");
            var serializer = new JavaScriptSerializer { MaxJsonLength = GitHubReleaseChecker.MaxResponseBytes, RecursionLimit = 32 };
            var data = serializer.DeserializeObject(json) as Dictionary<string, object>;
            object tag, draft, pre;
            ReleaseVersion version;
            if (data == null || !data.TryGetValue("tag_name", out tag) || !(tag is string) ||
                !data.TryGetValue("draft", out draft) || !(draft is bool) || (bool)draft ||
                !data.TryGetValue("prerelease", out pre) || !(pre is bool) || (bool)pre ||
                !ReleaseVersion.TryParse((string)tag, out version) || version.IsPrerelease)
                throw new InvalidDataException("Expected a published stable release.");
            var release = new GitHubRelease {
                Tag = (string)tag, Version = version,
                PageUrl = ProjectInfo.ReleasesUrl + "/tag/" + Uri.EscapeDataString((string)tag)
            };
            string expectedDownload = ProjectInfo.ReleasesUrl + "/download/" + Uri.EscapeDataString(release.Tag) + "/" + ProjectInfo.PortableAssetName;
            object assets;
            if (data.TryGetValue("assets", out assets) && assets is object[]) {
                foreach (object item in (object[])assets) {
                    var asset = item as Dictionary<string, object>;
                    object name, state, url, size;
                    if (asset != null && asset.TryGetValue("name", out name) && (name as string) == ProjectInfo.PortableAssetName &&
                        asset.TryGetValue("state", out state) && (state as string) == "uploaded" &&
                        asset.TryGetValue("browser_download_url", out url) && (url as string) == expectedDownload &&
                        asset.TryGetValue("size", out size) && (size is int || size is long || size is decimal) && Convert.ToDecimal(size) > 0)
                        release.HasPortableAsset = true;
                }
            }
            return release;
        }
    }

    public sealed class GitHubReleaseChecker {
        public const int MaxResponseBytes = 1024 * 1024;
        private readonly HttpClient client;
        public GitHubReleaseChecker(HttpClient client) { this.client = client; }
        public async Task<GitHubRelease> CheckAsync(CancellationToken cancellationToken) {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)) {
                timeout.CancelAfter(TimeSpan.FromSeconds(8));
                using (var request = new HttpRequestMessage(HttpMethod.Get, ProjectInfo.LatestReleaseApi)) {
                    request.Headers.UserAgent.ParseAdd("BetterJoy-Direct/" + ProjectInfo.Version);
                    request.Headers.Accept.ParseAdd("application/vnd.github+json");
                    request.Headers.Add("X-GitHub-Api-Version", "2026-03-10");
                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false)) {
                        response.EnsureSuccessStatusCode();
                        if (response.Content.Headers.ContentLength > MaxResponseBytes) throw new InvalidDataException("Release response is too large.");
                        using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        using (var buffer = new MemoryStream()) {
                            var chunk = new byte[8192];
                            int length;
                            while ((length = await stream.ReadAsync(chunk, 0, chunk.Length, timeout.Token).ConfigureAwait(false)) != 0) {
                                if (buffer.Length + length > MaxResponseBytes) throw new InvalidDataException("Release response is too large.");
                                buffer.Write(chunk, 0, length);
                            }
                            timeout.Token.ThrowIfCancellationRequested();
                            buffer.Position = 0;
                            using (var reader = new StreamReader(buffer)) return GitHubRelease.Parse(reader.ReadToEnd());
                        }
                    }
                }
            }
        }
    }
}
