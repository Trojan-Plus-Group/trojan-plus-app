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

        public static bool ClearForSession(string path, bool enabled, bool clearOnConnect)
        {
            if (enabled && clearOnConnect)
            {
                try
                {
                    File.Delete(path);
                }
                catch (IOException)
                {
                    return false;
                }
                catch (UnauthorizedAccessException)
                {
                    return false;
                }
            }

            return true;
        }

        public static void RemoveOtherHosts(string path)
        {
            string directory = Path.GetDirectoryName(path) ?? ".";
            Directory.CreateDirectory(directory);
            foreach (string file in Directory.GetFiles(directory, "*.log"))
            {
                if (file != path)
                {
                    File.Delete(file);
                }
            }
        }

        public static byte[] ReadSnapshot(string path, int sizeMB = 1)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
            if (stream.Length == 0 || stream.Length > RollingLog.CapacityForSize(sizeMB))
            {
                throw new IOException("Log unavailable or exceeds size limit.");
            }

            byte[] bytes = new byte[(int)stream.Length];
            stream.ReadExactly(bytes);
            return bytes;
        }
    }
}
