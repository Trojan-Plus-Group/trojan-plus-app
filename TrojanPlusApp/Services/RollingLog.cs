// SPDX-License-Identifier: GPL-3.0-or-later
namespace TrojanPlusApp.Services
{
    using System;
    using System.IO;
    using System.Text;

    // One writer owns the file. Exclusive sharing also rejects exports during teardown.
    public sealed class RollingLog : IDisposable
    {
        public const int Capacity = 1024 * 1024;
        private readonly FileStream stream;
        private readonly byte[] buffer = new byte[Capacity];

        public RollingLog(string path)
        {
            string directory = Path.GetDirectoryName(path) ?? ".";
            Directory.CreateDirectory(directory);
            stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            if (stream.Length > Capacity)
            {
                stream.SetLength(0);
            }
        }

        public void Write(ReadOnlySpan<byte> bytes)
        {
            // Bound allocations even for a single oversized message.
            if (bytes.Length > Capacity)
            {
                bytes = bytes.Slice(bytes.Length - Capacity);
                while (!bytes.IsEmpty && (bytes[0] & 0xC0) == 0x80)
                {
                    bytes = bytes.Slice(1);
                }
            }

            // Normalize external bytes, including invalid UTF-8, before storing text.
            string text = Encoding.UTF8.GetString(bytes);
            byte[] incoming = Encoding.UTF8.GetBytes(text.Replace('\0', '\uFFFD') + "\n");
            if (incoming.Length > Capacity)
            {
                int start = incoming.Length - Capacity;
                while (start < incoming.Length && (incoming[start] & 0xC0) == 0x80)
                {
                    start++;
                }

                incoming = incoming.AsSpan(start).ToArray();
            }

            int keep = (int)Math.Min(stream.Length, Capacity - incoming.Length);
            if (stream.Length + incoming.Length > Capacity)
            {
                long start = stream.Length - keep;
                stream.Position = start;
                stream.ReadExactly(buffer.AsSpan(0, keep));
                int skip = 0;
                while (skip < keep && (buffer[skip] & 0xC0) == 0x80)
                {
                    skip++;
                }

                // Prefer whole lines, unless retained tail contains no line break.
                int newline = buffer.AsSpan(skip, keep - skip).IndexOf((byte)'\n');
                if (newline >= 0)
                {
                    skip += newline + 1;
                }

                stream.Position = 0;
                stream.Write(buffer.AsSpan(skip, keep - skip));
                stream.SetLength(keep - skip);
            }

            stream.Position = stream.Length;
            stream.Write(incoming);
            stream.Flush();
        }

        public void Dispose()
        {
            stream.Dispose();
        }
    }
}
