// SPDX-License-Identifier: GPL-3.0-or-later
namespace TrojanPlusApp.Droid
{
    using System;
    using System.Globalization;
    using Newtonsoft.Json.Linq;
    using TrojanPlusApp.Services;

    internal static class VpnRunConfig
    {
        public static string Resolve(string template, int tunFd)
        {
            if (tunFd < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tunFd));
            }

            return template.Replace("${tun.tun_fd}", tunFd.ToString(CultureInfo.InvariantCulture));
        }

        public static string GetDebugLogPath(string resolvedConfig, string metadata)
        {
            // Validate only complete JSON, even when capture is disabled.
            var config = JObject.Parse(resolvedConfig);
            var session = string.IsNullOrEmpty(metadata) ? new JObject() : JObject.Parse(metadata);
            string host = (string)session["HostName"];
            return !string.IsNullOrEmpty(host) && config["log_level"]?.Type == JTokenType.Integer
                && (int)config["log_level"] is >= 0 and <= 2 ? DebugLogFiles.PathForHost(host) : null;
        }

        public static bool GetDebugLogClearOnConnect(string metadata)
        {
            var session = string.IsNullOrEmpty(metadata) ? new JObject() : JObject.Parse(metadata);
            return session["ClearOnConnect"]?.Type != JTokenType.Boolean || (bool)session["ClearOnConnect"];
        }

        public static int GetDebugLogSizeMB(string metadata)
        {
            var session = string.IsNullOrEmpty(metadata) ? new JObject() : JObject.Parse(metadata);
            return RollingLog.NormalizeSizeMB(session["SizeMB"]?.Type == JTokenType.Integer
                ? (int)session["SizeMB"] : 1);
        }
    }
}
