using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BetterJoyForCemu;

class FakeReleaseHandler : HttpMessageHandler {
    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Respond;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) {
        return Respond(request, token);
    }
}
class UnseekableStream : MemoryStream {
    public UnseekableStream(byte[] bytes) : base(bytes) { }
    public override bool CanSeek { get { return false; } }
}
class UpdateTests {
    static int count;
    static void Check(bool value, string message) {
        if (!value) throw new Exception("FAIL: " + message);
        count++;
    }
    static ReleaseVersion Version(string value) {
        ReleaseVersion parsed;
        Check(ReleaseVersion.TryParse(value, out parsed), "parse " + value);
        return parsed;
    }
    static void Reject(Action action, string message) {
        bool failed = false;
        try { action(); } catch (Exception) { failed = true; }
        Check(failed, message);
    }
    public static string Fixture(string tag = "direct-v0.7.4") {
        return "{\"tag_name\":\"" + tag + "\",\"draft\":false,\"prerelease\":false,\"html_url\":\"https://malicious.example/update\",\"assets\":[{\"name\":\"BetterJoy-Direct-win-x64.zip\",\"state\":\"uploaded\",\"size\":123,\"browser_download_url\":\"" + ProjectInfo.ReleasesUrl + "/download/" + tag + "/BetterJoy-Direct-win-x64.zip\"}]}";
    }
    static async Task CheckTransport() {
        var handler = new FakeReleaseHandler();
        handler.Respond = (request, token) => {
            Check(request.RequestUri.AbsoluteUri == ProjectInfo.LatestReleaseApi && request.Method == HttpMethod.Get, "public read-only latest-release endpoint");
            Check(request.Headers.UserAgent.ToString().Contains("BetterJoy-Direct/" + ProjectInfo.Version), "GitHub User-Agent");
            Check(request.Headers.Authorization == null, "no bundled GitHub credential");
            Check(request.Headers.Contains("X-GitHub-Api-Version"), "explicit API version");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Fixture()) });
        };
        using (var client = new HttpClient(handler)) {
            var release = await new GitHubReleaseChecker(client).CheckAsync(CancellationToken.None);
            Check(release.HasPortableAsset, "HTTP response parses expected asset");
        }
        foreach (HttpStatusCode code in new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError }) {
            handler = new FakeReleaseHandler { Respond = (request, token) => Task.FromResult(new HttpResponseMessage(code)) };
            using (var client = new HttpClient(handler)) {
                bool failed = false;
                try { await new GitHubReleaseChecker(client).CheckAsync(CancellationToken.None); } catch (HttpRequestException) { failed = true; }
                Check(failed, "HTTP " + (int)code + " is failure, not up-to-date");
            }
        }
        handler = new FakeReleaseHandler { Respond = (request, token) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StreamContent(new UnseekableStream(new byte[GitHubReleaseChecker.MaxResponseBytes + 1]))
        }) };
        using (var client = new HttpClient(handler)) {
            bool failed = false;
            try { await new GitHubReleaseChecker(client).CheckAsync(CancellationToken.None); } catch (InvalidDataException) { failed = true; }
            Check(failed, "unknown-length response is bounded during streaming");
        }
        handler = new FakeReleaseHandler { Respond = async (request, token) => {
            await Task.Delay(Timeout.Infinite, token);
            throw new Exception("unreachable");
        } };
        using (var client = new HttpClient(handler))
        using (var cancel = new CancellationTokenSource()) {
            cancel.CancelAfter(25);
            bool cancelled = false;
            try { await new GitHubReleaseChecker(client).CheckAsync(cancel.Token); } catch (OperationCanceledException) { cancelled = true; }
            Check(cancelled, "caller cancellation interrupts a pending request");
        }
        handler = new FakeReleaseHandler { Respond = async (request, token) => {
            await Task.Delay(Timeout.Infinite, token);
            throw new Exception("unreachable");
        } };
        using (var client = new HttpClient(handler)) {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            bool cancelled = false;
            try { await new GitHubReleaseChecker(client).CheckAsync(CancellationToken.None); } catch (OperationCanceledException) { cancelled = true; }
            Check(cancelled && watch.Elapsed.TotalSeconds < 12, "eight-second deadline bounds a stalled request");
        }
    }
    static void Main(string[] args) {
        try {
            if (args.Length > 0 && args[0] == "--live") {
                using (var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })) {
                    var liveRelease = new GitHubReleaseChecker(client).CheckAsync(CancellationToken.None).GetAwaiter().GetResult();
                    Console.WriteLine("LIVE: tag=" + liveRelease.Tag + " portable=" + liveRelease.HasPortableAsset + " comparison=" + liveRelease.CompareToCurrent() + " page=" + liveRelease.PageUrl);
                }
                return;
            }
            Check(Version("direct-v0.7.10").CompareTo(Version("v0.7.9")) > 0, "compare numeric version components");
            Check(Version("0.7.3").CompareTo(Version("0.7.3-preview")) > 0, "stable outranks matching preview");
            Check(Version("0.7.2").CompareTo(Version(ProjectInfo.Version)) < 0, "current version is not downgraded to an older stable release");
            Check(Version("1.0.0-rc.10").CompareTo(Version("1.0.0-rc.2")) > 0, "numeric prerelease ordering");
            Check(Version("1.0.0+build.1").CompareTo(Version("1.0.0+build.2")) == 0, "build metadata does not change precedence");
            Check(Version("1.0.0-alpha.1").CompareTo(Version("1.0.0-alpha.beta")) < 0, "numeric prerelease identifiers sort before text");
            foreach (string invalid in new[] { "", "v1", "garbage1.2.3", "1.2.3.4", "01.2.3", "1.2.3-01", "1.2.3/../../", "999999999999.0.0", "1.2.-1", "1.2.3\n" }) {
                ReleaseVersion parsed;
                Check(!ReleaseVersion.TryParse(invalid, out parsed), "reject invalid tag: " + invalid);
            }
            var release = GitHubRelease.Parse(Fixture());
            Check(release.HasPortableAsset && release.CompareToCurrent() > 0, "newer release and uploaded portable archive");
            Check(release.PageUrl == ProjectInfo.ReleasesUrl + "/tag/direct-v0.7.4", "release link is pinned to the project, ignoring html_url");
            Check(!GitHubRelease.Parse(Fixture().Replace("releases/download/", "releases/elsewhere/")).HasPortableAsset, "reject unexpected asset URL");
            Check(!GitHubRelease.Parse(Fixture().Replace("\"size\":123", "\"size\":0")).HasPortableAsset, "zero-size assets are not ready");
            Check(!GitHubRelease.Parse(Fixture().Replace("\"state\":\"uploaded\"", "\"state\":\"new\"")).HasPortableAsset, "unfinished assets are not ready");
            foreach (string invalid in new[] { "null", "[]", "{}", "not JSON", Fixture().Replace("\"draft\":false", "\"draft\":true"), Fixture().Replace("\"prerelease\":false", "\"prerelease\":true"), Fixture("direct-v0.7.4-rc.1"), Fixture().Replace("direct-v0.7.4", "invalid") })
                Reject(() => GitHubRelease.Parse(invalid), "reject malformed, draft, or preview response");
            var info = typeof(ProjectInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            Check(info.InformationalVersion == ProjectInfo.Version, "file product version matches displayed version");
            Check(typeof(ProjectInfo).Assembly.GetName().Version.ToString() == ProjectInfo.AssemblyVersion, "assembly version uses the shared source");
            CheckTransport().GetAwaiter().GetResult();
            Console.WriteLine("PASS: " + count + " update regression checks");
        } catch (Exception error) {
            Console.Error.WriteLine(error.GetType().Name + ": " + error.Message);
            Environment.Exit(1);
        }
    }
}
