// SPDX-License-Identifier: GPL-3.0-or-later
namespace TrojanPlusApp.Services
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    public static class DebugLogFiles
    {
        public static bool CanExport(bool enabled, bool stopped) => enabled && stopped;

        public static string PathForHost(string hostName)
        {
            string id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hostName)));
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "debug-log", id + ".log");
        }

        public static void RemoveOtherHosts(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            foreach (string file in Directory.GetFiles(Path.GetDirectoryName(path)!, "*.log"))
            {
                if (file != path)
                {
                    File.Delete(file);
                }
            }
        }

        public static byte[] ReadSnapshot(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
            if (stream.Length == 0 || stream.Length > RollingLog.Capacity)
            {
                throw new IOException("Log unavailable or exceeds size limit.");
            }

            byte[] bytes = new byte[(int)stream.Length];
            stream.ReadExactly(bytes);
            return bytes;
        }
    }
}
