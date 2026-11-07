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
        private readonly int capacity;
        private readonly FileStream stream;
        private readonly byte[] buffer = new byte[64 * 1024];

        public RollingLog(string path, int sizeMB = 1)
        {
            capacity = CapacityForSize(sizeMB);
            string directory = Path.GetDirectoryName(path) ?? ".";
            Directory.CreateDirectory(directory);
            stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            if (stream.Length > capacity)
            {
                stream.SetLength(0);
            }
        }

        public static int NormalizeSizeMB(int sizeMB) => sizeMB is 1 or 5 or 10 or 20 or 50 ? sizeMB : 1;

        public static int CapacityForSize(int sizeMB) => NormalizeSizeMB(sizeMB) * Capacity;

        public void Write(ReadOnlySpan<byte> bytes)
        {
            // Bound allocations even for a single oversized message.
            if (bytes.Length > capacity)
            {
                bytes = bytes.Slice(bytes.Length - capacity);
                while (!bytes.IsEmpty && (bytes[0] & 0xC0) == 0x80)
                {
                    bytes = bytes.Slice(1);
                }
            }

            // Normalize external bytes, including invalid UTF-8, before storing text.
            string text = Encoding.UTF8.GetString(bytes);
            byte[] incoming = Encoding.UTF8.GetBytes(text.Replace('\0', '\uFFFD') + "\n");
            if (incoming.Length > capacity)
            {
                int start = incoming.Length - capacity;
                while (start < incoming.Length && (incoming[start] & 0xC0) == 0x80)
                {
                    start++;
                }

                incoming = incoming.AsSpan(start).ToArray();
            }

            if (stream.Length + incoming.Length > capacity)
            {
                int keep = Math.Min(capacity * 3 / 4, capacity - incoming.Length);
                long end = stream.Length;
                long start = end - keep;
                long cursor = start;

                // Prefer whole lines, unless retained tail contains no line break.
                while (cursor < end)
                {
                    int count = (int)Math.Min(buffer.Length, end - cursor);
                    stream.Position = cursor;
                    stream.ReadExactly(buffer.AsSpan(0, count));
                    int skip = 0;
                    if (cursor == start)
                    {
                        while (skip < count && (buffer[skip] & 0xC0) == 0x80)
                        {
                            skip++;
                        }

                        start += skip;
                    }

                    int newline = buffer.AsSpan(skip, count - skip).IndexOf((byte)'\n');
                    if (newline >= 0)
                    {
                        start = cursor + skip + newline + 1;
                        break;
                    }

                    cursor += count;
                }

                long written = 0;
                while (start < end)
                {
                    int count = (int)Math.Min(buffer.Length, end - start);
                    stream.Position = start;
                    stream.ReadExactly(buffer.AsSpan(0, count));
                    stream.Position = written;
                    stream.Write(buffer.AsSpan(0, count));
                    start += count;
                    written += count;
                }

                stream.SetLength(written);
            }

            stream.Position = stream.Length;
            stream.Write(incoming);
        }

        public void Flush()
        {
            stream.Flush();
        }

        public void Dispose()
        {
            stream.Dispose(); // FileStream flushes buffered writes before closing.
        }
    }
}
