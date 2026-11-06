// Copyright (C) 2026 The Trojan Plus Group Authors.
// SPDX-License-Identifier: GPL-3.0-or-later

// ponytail: headless edit/persistence checks only; use device tests for MAUI and native storage.
namespace Microsoft.Maui.Controls { }
namespace TrojanPlusApp.Views { }

namespace Microsoft.Maui.Essentials
{
    public static class VersionTracking
    {
        public static string CurrentVersion => "regression";
    }
}

namespace Microsoft.Maui.Storage
{
    public static class Preferences
    {
        private static readonly Dictionary<string, string> Values = new();
        public static string Get(string key, string fallback) => Values.GetValueOrDefault(key, fallback);
        public static void Set(string key, string value) => Values[key] = value;
    }
}

namespace TrojanPlusApp.Behaviors
{
    // Validation is outside this check; fail loudly if a new path starts depending on it.
    public static class HostAddressValidation
    {
        public static bool IsValid(string value) => throw new NotSupportedException();
    }
    public static class IPAddressValidation
    {
        public static bool IsValid(string value) => throw new NotSupportedException();
    }
}

namespace TrojanPlusApp
{
    public sealed class App
    {
        public static App Instance { get; } = new();
        public StarterStub Starter => null;
        public string DataPathParent { get; } = Path.Combine(Path.GetTempPath(), "HostEditRegression-" + Guid.NewGuid().ToString("N"));
        public bool IsVpnServiceRunning => false;
        public bool IsStartBtnEnabled => true;
    }

    public sealed class StarterStub
    {
        public void SettingsChanged(Models.SettingsModel settings) => throw new NotSupportedException();
    }
}

namespace TrojanPlusApp.Resx
{
    public static class TextResource
    {
        public const string Hosts_BtnDisconnect = "Disconnect";
        public const string Hosts_BtnConnect = "Connect";
        public const string Menu_HostsViewTitle = "Hosts";
        public static string gfwlist => string.Empty;
        public static string cn_mainland_ips => string.Empty;
        public static string cacert => string.Empty;
    }
}
