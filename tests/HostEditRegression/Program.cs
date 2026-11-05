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
