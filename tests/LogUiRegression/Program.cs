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
    Console.WriteLine($"PASS: {checks} production HostEditPage export handler scenarios.");
}
finally
{
    page.Disappear();
    if (File.Exists(logPath)) File.Delete(logPath);
    if (Directory.Exists(cache)) Directory.Delete(cache, true);
}
