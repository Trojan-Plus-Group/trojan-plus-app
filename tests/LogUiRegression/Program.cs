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
    byte[] full = System.Text.Encoding.UTF8.GetBytes(new string('x', RollingLog.Capacity));
    File.WriteAllBytes(logPath, full);
    Microsoft.Maui.Devices.DeviceInfo.Platform = Microsoft.Maui.Devices.DevicePlatform.Android;
    page.Copy();
    Check(Clipboard.Default.Copies.Last().Length == 65536, "Android copy must cap at 64K characters");
    Check(page.Errors.Last() == TextResource.New_DebugLogCopiedTail, "Truncated copy must notify user");
    page.ShareLog();
    Check(File.ReadAllBytes(Share.Default.Requests.Last().File.FullPath).SequenceEqual(full), "Share must remain complete");
    Clipboard.Default.Reject = true;
    int failurePrompts = page.PrivacyPrompts;
    int shares = Share.Default.Requests.Count;
    page.Copy();
    Check(page.PrivacyPrompts == failurePrompts + 1 && Share.Default.Requests.Count == shares, "Failure must not prompt again or share");
    Check(page.Errors.Last() == TextResource.New_DebugLogExportFailed, "Copy failure needs generic error");
    Check(File.ReadAllBytes(logPath).SequenceEqual(full), "Copy must not change source");
    Clipboard.Default.Reject = false;
    checks += 2;
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


Check(LogClipboard.Tail("") == "", "Empty log unchanged");
Check(LogClipboard.Tail("short log\n") == "short log\n", "Short log copied whole");
string exact = new string('a', 65536);
Check(LogClipboard.Tail(exact) == exact, "Boundary log copied whole");
Check(LogClipboard.Tail("old" + exact) == exact, "Newest 64K retained");
Check(LogClipboard.Tail("prefix\n" + new string('a', 65530) + "\nlast\n") == new string('a', 65530) + "\nlast\n", "Full line retained");
Check(LogClipboard.Tail("prefix" + new string('a', 65530) + "\nlast\n") == "last\n", "Partial oldest line removed");
string unicode = "old😀" + new string('x', 65535);
string tail = LogClipboard.Tail(unicode);
Check(tail.Length == 65535 && tail == new string('x', 65535), "Surrogate pair must not split");
Console.WriteLine("PASS: fixed 64K tail, line/surrogate boundaries and short/empty logs.");
