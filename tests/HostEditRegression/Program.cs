// Copyright (C) 2026 The Trojan Plus Group Authors.
// SPDX-License-Identifier: GPL-3.0-or-later
// Run: dotnet run --project tests/HostEditRegression/HostEditRegression.csproj

using TrojanPlusApp.Models;
using TrojanPlusApp.Services;
using TrojanPlusApp.ViewModels;

static void Check(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static HostModel Host(string name) => new()
{
    HostName = name,
    HostAddress = "example.com",
    Password = "test-only",
};

// Reflection keeps pre-feature checks runnable against production sources.
var clearProperty = typeof(HostModel).GetProperty("DebugLogClearOnConnect");
Check(clearProperty != null, "Host must persist DebugLogClearOnConnect");
Check((bool)clearProperty!.GetValue(new HostModel())!, "New host must clear by default");
var oldHost = Newtonsoft.Json.JsonConvert.DeserializeObject<HostModel>("{\"HostName\":\"old\"}")!;
Check((bool)clearProperty.GetValue(oldHost)!, "Saved host without field must clear by default");
foreach (bool clear in new[] { true, false })
{
    var options = Host("clear-options");
    clearProperty.SetValue(options, clear);
    var saved = new DataStore();
    await saved.AddItemAsync(options);
    var loaded = new List<HostModel>();
    new DataStore().FillHosts(loaded);
    Check((bool)clearProperty.GetValue(loaded.Single(h => h.HostName == options.HostName))! == clear,
        "Save/load must retain both clear values");
    Check((bool)clearProperty.GetValue(options.Duplicate("copy"))! == clear, "Clone must retain clear option");
    await saved.DeleteItemAsync(options.HostName);
}
var clearParser = typeof(TrojanPlusApp.Droid.VpnRunConfig).GetMethod("GetDebugLogClearOnConnect");
Check(clearParser != null, "Metadata must parse ClearOnConnect");
foreach (string value in new[] { "true", "false", "null", "\"false\"", "0", "1", "[]", "{}" })
{
    bool parsed = (bool)clearParser!.Invoke(null, new object[] { "{\"ClearOnConnect\":" + value + "}" })!;
    Check(parsed == (value != "false"), "Only boolean false may disable clearing");
}
foreach (string metadata in new[] { "", "{}", "{\"HostName\":\"old\",\"SizeMB\":5}" })
{
    Check((bool)clearParser!.Invoke(null, new object[] { metadata })!, "Missing metadata field defaults true");
}
Console.WriteLine("PASS: clear defaults, legacy data, save/load and clone true/false; strict metadata boolean/default parsing.");

var viewModel = new HostsViewModel();
var store = viewModel.DataStore;
var first = Host("first");
var original = Host("original");
var dependent = Host("dependent");
original.LoadBalance.Add("first");
dependent.LoadBalance.AddRange(new[] { "original", "first", "original" });
foreach (var host in new[] { first, original, dependent })
{
    await store.AddItemAsync(host);
    viewModel.Items.Add(host);
}
viewModel.SelectedHostItem("original");
original.UI_NotRunning = false;
store.SetHostRunningName("original");

var cancelled = original.Duplicate(original.HostName);
Check(!ReferenceEquals(cancelled, original), "Edit must clone host");
Check(!ReferenceEquals(cancelled.LoadBalance, original.LoadBalance), "Edit must clone load balance list");
Check(!cancelled.UI_Selected && cancelled.UI_NotRunning, "Clone must reset UI flags");
cancelled.HostAddress = "cancelled.example.com";
cancelled.LoadBalance.Clear();
cancelled.EnableQuic = true;
Check(original.HostAddress == "example.com" && !original.EnableQuic &&
    original.LoadBalance.SequenceEqual(new[] { "first" }), "Cancel must not change original");
Check(original.UI_Selected && !original.UI_NotRunning, "Cloning must not change original UI flags");

// Same-name save must replace both references, not silently leave stale data.
var edited = original.Duplicate(original.HostName);
edited.HostAddress = "edited.example.com";
edited.EnableQuic = true;
await viewModel.UpdateItemAsync(original.HostName, edited);
var stored = new List<HostModel>();
store.FillHosts(stored);
Check(ReferenceEquals(viewModel.Items[1], edited) && ReferenceEquals(stored[1], edited),
    "Save must replace view and store references at original index");
Check(ReferenceEquals(viewModel.CurretSelectHost, edited) && viewModel.CurrSelectHostIdx == 1,
    "Save must keep selection index");
Check(edited.UI_Selected && !edited.UI_NotRunning, "Save must preserve selected/running flags");
Check(ReferenceEquals(viewModel.Items[0], first) && ReferenceEquals(viewModel.Items[2], dependent),
    "Save must preserve other items and order");

var renamed = edited.Duplicate("renamed");
renamed.EnablePipeline = true;
await viewModel.UpdateItemAsync("original", renamed);
Check(viewModel.FindHostByName("original") == null &&
    ReferenceEquals(viewModel.FindHostByName("renamed"), renamed), "Rename must replace name lookup");
Check(dependent.LoadBalance.SequenceEqual(new[] { "renamed", "first", "renamed" }),
    "Rename must update all load balance references without changing order");
Check(store.Settings.HostRunningName == "renamed" && store.Settings.HostSelectedIdx == 1,
    "Rename must preserve settings and update running name");
Check(renamed.UI_Selected && !renamed.UI_NotRunning && viewModel.Items.Count == 3,
    "Rename must preserve flags and item count");

var reloaded = new DataStore();
var restored = new List<HostModel>();
reloaded.FillHosts(restored);
Check(restored.Select(h => h.HostName).SequenceEqual(new[] { "first", "renamed", "dependent" }),
    "Persistence must preserve names and order");
Check(restored[1].HostAddress == "edited.example.com" && restored[1].EnableQuic && restored[1].EnablePipeline,
    "Persistence must preserve edited values");
Check(restored[1].LoadBalance.SequenceEqual(new[] { "first" }) &&
    restored[2].LoadBalance.SequenceEqual(dependent.LoadBalance), "Persistence must preserve load balance rename");
Check(reloaded.Settings.HostRunningName == "renamed" && reloaded.Settings.HostSelectedIdx == 1,
    "Persistence must preserve renamed running name and selection index");
Check(!restored[1].UI_Selected && restored[1].UI_NotRunning,
    "UI flags must not persist");

foreach (var pipeline in new[] { false, true })
{
    foreach (var quic in new[] { false, true })
    {
        var host = Host("description");
        host.EnablePipeline = pipeline;
        host.EnableQuic = quic;
        Check(host.UI_Description == "example.com:443" +
            (pipeline ? " (Pipeline Mode)" : "") + (quic ? " (QUIC)" : ""),
            $"Description mismatch: pipeline={pipeline}, quic={quic}");
    }
}

// Persisted JSON null must fall back to usable empty hosts and default settings.
Microsoft.Maui.Storage.Preferences.Set(DataStore.HostsKey, "null");
Microsoft.Maui.Storage.Preferences.Set(DataStore.SettingsKey, "null");
var nullStore = new DataStore();
var nullHosts = new List<HostModel>();
nullStore.FillHosts(nullHosts);
Check(nullHosts.Count == 0 && nullStore.Settings.EnableAndroidNotification,
    "JSON null must produce empty hosts and default settings");
await nullStore.AddItemAsync(Host("fallback"));
Check(nullStore.HasHost("fallback"), "Fallback host collection must accept new hosts");

Microsoft.Maui.Storage.Preferences.Set(DataStore.HostsKey, "[]");
Microsoft.Maui.Storage.Preferences.Set(DataStore.SettingsKey, "{}");
var emptyStore = new DataStore();
Check(await emptyStore.DeleteItemAsync("missing"), "Missing delete on empty store must remain successful");
await emptyStore.AddItemAsync(Host("remaining"));
try
{
    await emptyStore.DeleteItemAsync("missing");
    throw new InvalidOperationException("Missing delete on nonempty store must fail when reading old host");
}
catch (NullReferenceException)
{
}

Console.WriteLine("PASS: clone cancel isolation; update references/flags/index; rename references/settings; persistence; QUIC descriptions; JSON null fallbacks and missing deletes.");

// Old saved hosts retain safe defaults; picker indexes never become persisted options.
var legacy = Newtonsoft.Json.JsonConvert.DeserializeObject<HostModel>("{\"HostName\":\"legacy\"}")!;
Check(!legacy.EnableDebugLog && legacy.DebugLogLevel == 2 && legacy.DebugLogSizeMB == 1 &&
    legacy.UI_DebugLogLevel == 0 && legacy.UI_DebugLogSize == 0, "Old JSON must default to Warning and 1 MB");
int[] levels = { 2, 1, 0 };
int[] sizes = { 1, 5, 10, 20, 50 };
for (int index = 0; index < levels.Length; index++)
{
    legacy.DebugLogLevel = levels[index];
    Check(legacy.UI_DebugLogLevel == index, "Level must map to picker index");
    legacy.UI_DebugLogLevel = index;
    Check(legacy.DebugLogLevel == levels[index], "Picker must map to native level");
}
for (int index = 0; index < sizes.Length; index++)
{
    legacy.DebugLogSizeMB = sizes[index];
    Check(legacy.UI_DebugLogSize == index, "Size must map to picker index");
    legacy.UI_DebugLogSize = index;
    Check(legacy.DebugLogSizeMB == sizes[index], "Picker must map to size in MB");
}
foreach (int invalid in new[] { int.MinValue, -1, 3, 4, 6, 51, int.MaxValue })
{
    legacy.DebugLogLevel = invalid;
    legacy.DebugLogSizeMB = invalid;
    Check(legacy.DebugLogLevel == 2 && legacy.DebugLogSizeMB == 1, "Invalid model values must reset to defaults");
    var invalidJson = Newtonsoft.Json.JsonConvert.DeserializeObject<HostModel>(
        $"{{\"DebugLogLevel\":{invalid},\"DebugLogSizeMB\":{invalid}}}")!;
    Check(invalidJson.DebugLogLevel == 2 && invalidJson.DebugLogSizeMB == 1,
        "Invalid persisted values must reset to defaults");
}
foreach (int invalid in new[] { int.MinValue, -1, 5, int.MaxValue })
{
    legacy.DebugLogLevel = 0;
    legacy.DebugLogSizeMB = 50;
    legacy.UI_DebugLogLevel = invalid;
    legacy.UI_DebugLogSize = invalid;
    Check(legacy.DebugLogLevel == 2 && legacy.DebugLogSizeMB == 1, "Invalid picker indexes must reset to defaults");
}
foreach (int level in levels)
{
    foreach (int size in sizes)
    {
        var options = Host("options");
        options.EnableDebugLog = true;
        options.DebugLogLevel = level;
        options.DebugLogSizeMB = size;
        var duplicate = options.Duplicate("options-copy");
        Check(duplicate.EnableDebugLog && duplicate.DebugLogLevel == level && duplicate.DebugLogSizeMB == size,
            "Duplicate must retain log options");
        duplicate.EnableDebugLog = false;
        duplicate.DebugLogLevel = (level + 1) % 3;
        duplicate.DebugLogSizeMB = size == 50 ? 1 : 50;
        Check(options.EnableDebugLog && options.DebugLogLevel == level && options.DebugLogSizeMB == size,
            "Cancelled option edits must not change original");
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(options);
        Check(!json.Contains("UI_DebugLogLevel") && !json.Contains("UI_DebugLogSize"), "Picker properties must not persist");
        var roundtrip = Newtonsoft.Json.JsonConvert.DeserializeObject<HostModel>(json)!;
        Check(roundtrip.EnableDebugLog && roundtrip.DebugLogLevel == level && roundtrip.DebugLogSizeMB == size,
            "JSON roundtrip must retain every level and size");
    }
}

// Real config generator deliberately leaves an unquoted FD placeholder for Android.
foreach (int level in levels)
foreach (bool enabled in new[] { false, true })
{
    var fixture = Host("loopback-log-regression");
    fixture.HostAddress = "127.0.0.1";
    fixture.EnableDebugLog = enabled;
    fixture.DebugLogLevel = level;
    string metadata = Newtonsoft.Json.JsonConvert.SerializeObject(new { fixture.HostName, SizeMB = 1 });
    string template = fixture.PrepareConfig(viewModel);
    Check(template.Contains("${tun.tun_fd}"), "Fixture must retain actual FD placeholder");
    bool oldParseFailed = false;
    try { Newtonsoft.Json.Linq.JObject.Parse(template); }
    catch (Newtonsoft.Json.JsonReaderException) { oldParseFailed = true; }
    Check(oldParseFailed, "Pre-fix parser must fail for enabled and disabled logs");
    string resolved = TrojanPlusApp.Droid.VpnRunConfig.Resolve(template, 42);
    Check(!resolved.Contains("${tun.tun_fd}"), "FD must resolve before parsing");
    Check((int)Newtonsoft.Json.Linq.JObject.Parse(resolved)["log_level"]! == (enabled ? level : 5),
        "Config must emit selected native level or OFF");
    Check(TrojanPlusApp.Droid.VpnRunConfig.GetDebugLogPath(resolved, metadata) ==
        (enabled ? DebugLogFiles.PathForHost(fixture.HostName) : null),
        "Capture requires explicit metadata and selected native level");
    foreach (int size in sizes)
    {
        string sizedMetadata = Newtonsoft.Json.JsonConvert.SerializeObject(new { fixture.HostName, SizeMB = size });
        Check(TrojanPlusApp.Droid.VpnRunConfig.GetDebugLogSizeMB(sizedMetadata) == size,
            "Session metadata must propagate every supported cap");
        Check(TrojanPlusApp.Droid.VpnRunConfig.GetDebugLogPath(resolved, sizedMetadata) ==
            (enabled ? DebugLogFiles.PathForHost(fixture.HostName) : null), "Size must not change host log path or enable OFF capture");
    }
    Check(TrojanPlusApp.Droid.VpnRunConfig.GetDebugLogPath(resolved, "") == null, "Missing metadata disables capture");
    foreach (string invalid in new[] { template, "{", "[]" })
    {
        bool rejected = false;
        try { TrojanPlusApp.Droid.VpnRunConfig.GetDebugLogPath(invalid, metadata); }
        catch (Newtonsoft.Json.JsonException) { rejected = true; }
        Check(rejected, "Invalid or unfinished config must fail closed");
    }
}
bool invalidFd = false;
try { TrojanPlusApp.Droid.VpnRunConfig.Resolve("{}", -1); }
catch (ArgumentOutOfRangeException) { invalidFd = true; }
Check(invalidFd, "Invalid FD must be rejected");
const string hostMetadata = "{\"HostName\":\"host\",\"SizeMB\":5}";
Check(TrojanPlusApp.Droid.VpnRunConfig.GetDebugLogPath("{}", hostMetadata) == null, "Absent level disables capture");
Check(TrojanPlusApp.Droid.VpnRunConfig.GetDebugLogPath("{\"log_level\":\"0\"}", hostMetadata) == null, "Noninteger level disables capture");
foreach (int invalidLevel in new[] { -1, 3, 4, 5, int.MaxValue })
{
    Check(TrojanPlusApp.Droid.VpnRunConfig.GetDebugLogPath($"{{\"log_level\":{invalidLevel}}}", hostMetadata) == null,
        "Unsupported native levels must disable capture");
}
foreach (string metadata in new[] { "", "{}", "{\"SizeMB\":5}", "{\"HostName\":\"\"}" })
{
    Check(TrojanPlusApp.Droid.VpnRunConfig.GetDebugLogPath("{\"log_level\":2}", metadata) == null,
        "Missing host metadata must disable capture");
}
foreach (string metadata in new[] { "", "{}", "{\"SizeMB\":null}", "{\"SizeMB\":\"5\"}", "{\"SizeMB\":1.5}",
    "{\"SizeMB\":-1}", "{\"SizeMB\":0}", "{\"SizeMB\":2}", "{\"SizeMB\":51}" })
{
    Check(TrojanPlusApp.Droid.VpnRunConfig.GetDebugLogSizeMB(metadata) == 1,
        "Missing, noninteger and unsupported metadata sizes must default to 1 MB");
}
Console.WriteLine("PASS: log defaults, clamps, picker mappings, duplicate/cancel isolation and 15 JSON roundtrips; 6 level/OFF configs and 30 metadata size combinations; invalid metadata and levels.");
Directory.Delete(TrojanPlusApp.App.Instance.DataPathParent, true);
