// SPDX-License-Identifier: GPL-3.0-or-later
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;
using TrojanPlusApp;
using TrojanPlusApp.Models;
using TrojanPlusApp.Resx;
using TrojanPlusApp.Services;
using TrojanPlusApp.ViewModels;
using TrojanPlusApp.Views;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

// No SynchronizationContext: completed fake platform tasks run synchronously, including
// continuations released by SetResult. Calls below enter the actual async-void handlers.
Check(SynchronizationContext.Current == null, "Run as console app without UI synchronization context.");
string hostName = "log-ui-regression-" + Guid.NewGuid().ToString("N");
string logPath = DebugLogFiles.PathForHost(hostName);
string cache = Path.Combine(Path.GetTempPath(), hostName);
FileSystem.CacheDirectory = cache;
var model = new HostsViewModel();
var page = new HostEditPage(model, new HostModel { HostName = hostName, EnableDebugLog = true });
int checks = 0;
void NoTransfer()
{
    Check(Clipboard.Default.Copies.Count == 0 && Share.Default.Requests.Count == 0, "Unexpected transfer.");
    Check(page.IsDebugLogExportEnabled, "Busy flag not restored.");
    checks++;
}
void Blocked(Action set, Action reset)
{
    set();
    Check(!page.CanExportDebugLog, "Export gate open while blocked.");
    int prompts = page.PrivacyPrompts;
    page.Copy();
    page.ShareLog();
    Check(page.PrivacyPrompts == prompts, "Blocked export prompted.");
    NoTransfer();
    reset();
}
try
{
    // Invisible pages must not export, even with otherwise eligible state.
    page.Copy();
    Check(page.PrivacyPrompts == 0, "Hidden page prompted.");
    NoTransfer();
    page.Appear();
    Check(page.CanExportDebugLog, "Stopped + option enabled should allow export.");
    Blocked(() => page.Item.EnableDebugLog = false, () => page.Item.EnableDebugLog = true);
    Blocked(() => App.Instance.IsVpnServiceRunning = true, () => App.Instance.IsVpnServiceRunning = false);
    Blocked(() => App.Instance.IsStartBtnEnabled = false, () => App.Instance.IsStartBtnEnabled = true);
    Blocked(() => model.DataStore.Settings.HostRunningName = hostName, () => model.DataStore.Settings.HostRunningName = "");

    foreach (bool share in new[] { false, true })
    {
        page.Confirm = () => Task.FromResult(false);
        int prompts = page.PrivacyPrompts;
        int priorErrors = page.Errors.Count;
        if (share) page.ShareLog(); else page.Copy();
        Check(page.PrivacyPrompts == prompts + 1 && page.Errors.Count == priorErrors, $"Privacy cancel did not return cleanly. prompts={page.PrivacyPrompts}/{prompts} errors=[{string.Join("|", page.Errors)}] can={page.CanExportDebugLog}");
        NoTransfer();

        page.Confirm = () => Task.FromResult(true);
        if (share) page.ShareLog(); else page.Copy();
        Check(page.Errors.Last() == TextResource.New_DebugLogEmpty, "Missing file should show empty-log error.");
        NoTransfer();
    }

    var confirmation = new TaskCompletionSource<bool>();
    page.Confirm = () => confirmation.Task;
    page.Copy();
    Check(!page.IsDebugLogExportEnabled, "Pending confirmation must disable actions.");
    int pendingPrompts = page.PrivacyPrompts;
    page.Copy();
    page.ShareLog();
    Check(page.PrivacyPrompts == pendingPrompts, "Repeat click started another export.");
    confirmation.SetResult(false);
    NoTransfer();

    // State may change while privacy dialog is open; recheck before reading.
    confirmation = new TaskCompletionSource<bool>();
    page.Copy();
    App.Instance.IsVpnServiceRunning = true;
    confirmation.SetResult(true);
    Check(page.Errors.Last() == TextResource.New_DebugLogStopFirst, "VPN start during prompt must block export.");
    App.Instance.IsVpnServiceRunning = false;
    NoTransfer();

    confirmation = new TaskCompletionSource<bool>();
    page.Copy();
    int errors = page.Errors.Count;
    page.Disappear();
    confirmation.SetResult(true);
    Check(page.Errors.Count == errors, "Disappearing during prompt should return silently.");
    NoTransfer();
    page.Appear();

    Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
    const string text = "diagnostic snapshot\nUTF-8: café\n";
    File.WriteAllText(logPath, text);
    // Renaming unsaved Item must still export original saved host's log.
    page.Item.HostName = hostName + "-renamed";
    page.Confirm = () => Task.FromResult(true);
    page.Copy();
    Check(Clipboard.Default.Copies.SequenceEqual(new[] { text }), "Copy must use original host snapshot.");
    Check(page.Errors.Count == errors && page.IsDebugLogExportEnabled, "Copy failed or stayed busy.");
    checks++;

    string exports = Path.Combine(cache, "debug-log-exports");
    Directory.CreateDirectory(exports);
    string oldExport = Path.Combine(exports, "debug-log-old.txt");
    File.WriteAllText(oldExport, "old snapshot");
    page.ShareLog();
    Check(Share.Default.Requests.Count == 1, "Share should send one request.");
    var request = Share.Default.Requests.Single();
    Check(request.Title == TextResource.New_ShareDebugLog && request.File.ContentType == "text/plain", "Wrong share metadata.");
    Check(Path.GetDirectoryName(request.File.FullPath) == exports, "Share must use cache export directory.");
    Check(File.ReadAllText(request.File.FullPath) == text, "Share snapshot differs from log.");
    Check(!File.Exists(oldExport) && Directory.GetFiles(exports).Length == 1, "Old share snapshot not removed.");
    Check(File.ReadAllText(logPath) == text, "Export modified source log.");
    Check(page.Errors.Count == errors && page.IsDebugLogExportEnabled, "Share failed or stayed busy.");
    checks++;
    // A complete 1 MiB UTF-8 snapshot must survive clipboard rejection unchanged.
    byte[] full = System.Text.Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("café\n", 174762)) + "tail\n012345");
    Array.Resize(ref full, RollingLog.Capacity);
    File.WriteAllBytes(logPath, full);
    Clipboard.Default.Reject = true;
    int beforeShares = Share.Default.Requests.Count;
    int prompt = 0;
    page.Confirm = () => Task.FromResult(++prompt == 1); // initial privacy yes; fallback no
    page.Copy();
    Check(prompt == 2 && Share.Default.Requests.Count == beforeShares, "Rejected copy must offer fallback; cancel must not share.");
    Check(File.ReadAllBytes(logPath).SequenceEqual(full), "Rejected copy changed original bytes.");
    page.Confirm = () => Task.FromResult(true);
    page.Copy();
    Check(Share.Default.Requests.Count == beforeShares + 1, "Confirmed fallback must share complete snapshot.");
    Check(File.ReadAllBytes(Share.Default.Requests.Last().File.FullPath).SequenceEqual(full), "Fallback truncated full snapshot.");
    Check(File.ReadAllBytes(logPath).SequenceEqual(full) && page.IsDebugLogExportEnabled, "Fallback changed source or stayed busy.");
    Clipboard.Default.Reject = false;
    Microsoft.Maui.Devices.DeviceInfo.Platform = Microsoft.Maui.Devices.DevicePlatform.iOS;
    page.Copy();
    Check(System.Text.Encoding.UTF8.GetBytes(Clipboard.Default.Copies.Last()).SequenceEqual(full), "Successful large copy lost bytes.");
    Microsoft.Maui.Devices.DeviceInfo.Platform = Microsoft.Maui.Devices.DevicePlatform.Android;
    page.Copy();
    Check(Clipboard.Default.Copies.Last().Length <= LogClipboard.CharacterLimit, "Android copy must respect UTF-16 budget");
    Check(page.Errors.Last() == TextResource.New_DebugLogCopiedTail, "Truncated copy must notify user");
    page.ShareLog();
    Check(File.ReadAllBytes(Share.Default.Requests.Last().File.FullPath).SequenceEqual(full), "Share must remain complete after truncated copy");
    checks += 4;
    Console.WriteLine($"PASS: {checks} production HostEditPage export handler scenarios.");
}
finally
{
    page.Disappear();
    if (File.Exists(logPath)) File.Delete(logPath);
    if (Directory.Exists(cache)) Directory.Delete(cache, true);
}

// Compile and invoke actual shared HostsPage path without the ANDROID symbol.
#if ANDROID
#error This regression must exercise the net10.0 shared compilation without ANDROID.
#endif
string sessionDir = Path.Combine(Path.GetTempPath(), "metadata-regression-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sessionDir);
try
{
    App.Instance.ConfigPath = Path.Combine(sessionDir, "config.json");
    string metadata = App.Instance.ConfigPath + ".log-host";
    Microsoft.Maui.Devices.DeviceInfo.Platform = Microsoft.Maui.Devices.DevicePlatform.Android;
    var hostsPage = new HostsPage();
    var hosts = HostsViewModel.Latest!;
    var selected = new HostModel { HostName = "fixture-a", EnableDebugLog = true };
    hosts.CurretSelectHost = selected;
    int starts = App.Instance.Starts;
    hostsPage.OnConnectBtnClicked(hostsPage, EventArgs.Empty);
    Check(File.Exists(metadata) && File.ReadAllText(metadata) == "fixture-a", "Shared Android path must write opt-in metadata");
    Check(App.Instance.Starts == ++starts, "Successful metadata write must start VPN");
    selected.HostName = "fixture-b";
    hostsPage.OnConnectBtnClicked(hostsPage, EventArgs.Empty);
    Check(File.ReadAllText(metadata) == "fixture-b", "Host switch must replace previous name");
    Check(App.Instance.Starts == ++starts, "Switched host starts");
    selected.EnableDebugLog = false;
    hostsPage.OnConnectBtnClicked(hostsPage, EventArgs.Empty);
    Check(File.ReadAllText(metadata) == "", "Disabled logging must revoke opt-in");
    Check(App.Instance.Starts == ++starts, "Disabled logging still starts VPN");
    File.WriteAllText(metadata, "stale-host");
    selected.Prepare = () => throw new IOException("test-only preparation failure");
    hostsPage.OnConnectBtnClicked(hostsPage, EventArgs.Empty);
    Check(!File.Exists(metadata) && App.Instance.Starts == starts, "Preparation failure revokes stale metadata and blocks startup");
    selected.Prepare = () => { Directory.CreateDirectory(metadata); return "{}"; };
    hostsPage.OnConnectBtnClicked(hostsPage, EventArgs.Empty);
    Check(App.Instance.Starts == starts, "Metadata write failure must prevent startup");
    Directory.Delete(metadata);
    File.Delete(App.Instance.ConfigPath);
    Directory.CreateDirectory(App.Instance.ConfigPath);
    File.WriteAllText(metadata, "stale-host");
    selected.Prepare = () => "{}";
    hostsPage.OnConnectBtnClicked(hostsPage, EventArgs.Empty);
    Check(!File.Exists(metadata) && App.Instance.Starts == starts, "Config write failure revokes stale opt-in and blocks startup");
    Directory.Delete(App.Instance.ConfigPath);
    Microsoft.Maui.Devices.DeviceInfo.Platform = Microsoft.Maui.Devices.DevicePlatform.iOS;
    hostsPage.OnConnectBtnClicked(hostsPage, EventArgs.Empty);
    Check(!File.Exists(metadata) && App.Instance.Starts == ++starts, "Non-Android path must not create metadata");
    Console.WriteLine("PASS: compiled shared HostsPage metadata on/off, host switch, preparation/config/metadata failures and non-Android path.");
}
finally
{
    Directory.Delete(sessionDir, true);
}


foreach (string sample in new[] { "", "small log\n", new string('a', LogClipboard.CharacterLimit),
    new string('a', 1048576), string.Concat(Enumerable.Repeat("日志 café 😀\n", 100000)),
    "😀😀😀😀😀", "first line\nsecond line\nlast line\n" })
{
    string tail = LogClipboard.Tail(sample, 7);
    Check(tail.Length <= 7 && sample.EndsWith(tail, StringComparison.Ordinal), "Tail must be bounded newest text");
    Check(tail.Length == 0 || !char.IsLowSurrogate(tail[0]), "Tail split surrogate pair");
    string? copied = null;
    bool truncated = await LogClipboard.CopyAsync(sample, true, value => { copied = value; return Task.CompletedTask; });
    Check(copied == LogClipboard.Tail(sample, LogClipboard.CharacterLimit), "Android budget mismatch");
    Check(truncated == (sample.Length > LogClipboard.CharacterLimit), "Truncation notification mismatch");
}
Check(LogClipboard.Tail("prefix\nnewest\n", 9) == "newest\n", "Prefer complete newest line");
int calls = 0;
string retryText = new string('x', 10000);
bool retryTruncated = await LogClipboard.CopyAsync(retryText, true, value => {
    calls++;
    if (calls < 3) throw new Android.OS.TransactionTooLargeException();
    Check(value.Length == 2500, "Retries must halve tail");
    return Task.CompletedTask;
});
Check(calls == 3 && retryTruncated, "Bounded retry failed");
foreach (bool android in new[] { false, true })
{
    calls = 0;
    try
    {
        await LogClipboard.CopyAsync(retryText, android, value => { calls++; throw new Android.OS.TransactionTooLargeException(); });
        throw new Exception("Expected size failure");
    }
    catch (Android.OS.TransactionTooLargeException) { Check(calls == (android ? 3 : 1), "Wrong retry bound/platform"); }
}
calls = 0;
try { await LogClipboard.CopyAsync(retryText, true, value => { calls++; throw new IOException(); }); }
catch (IOException) { Check(calls == 1, "Non-size failures must not retry"); }
Console.WriteLine("PASS: clipboard ASCII/UTF-8/surrogate/empty/boundary/small/full, line tails, bounded size retries and other failures.");
