// SPDX-License-Identifier: GPL-3.0-or-later
namespace TrojanPlusApp.Droid
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using TrojanPlusApp.Services;

    internal sealed class NativeDebugLog
    {
        private const int MessageLimit = 16 * 1024;
        private static readonly object Lifecycle = new object();
        private static readonly LogCallback Callback = Receive;
        private static NativeDebugLog active;
        private readonly Channel<byte[]> queue = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(32)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
        });
        private readonly LogCallback callback;
        private readonly Task writer;
        private long dropped;
        private long truncated;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void LogCallback(IntPtr message, UIntPtr length, int level, IntPtr context);

        [DllImport("trojan.so", EntryPoint = "trojan_set_log_callback", CallingConvention = CallingConvention.Cdecl)]
        private static extern int SetCallback(LogCallback callback, IntPtr context);

        [DllImport("trojan.so", EntryPoint = "trojan_set_log_callback", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ClearCallback(IntPtr callback, IntPtr context);

        private NativeDebugLog(string path, int sizeMB, bool clearOnConnect)
        {
            callback = Callback;
            DebugLogFiles.RemoveOtherHosts(path);
            if (!DebugLogFiles.ClearForSession(path, true, clearOnConnect))
            {
                Android.Util.Log.Warn("NativeDebugLog", "Detailed log clear failed; appending existing log.");
            }

            var sink = new RollingLog(path, sizeMB);
            writer = Task.Run(async () =>
            {
                using (sink)
                {
                    try
                    {
                        while (await queue.Reader.WaitToReadAsync())
                        {
                            while (queue.Reader.TryRead(out byte[] bytes))
                            {
                                WriteCounts(sink);
                                sink.Write(bytes);
                            }

                            sink.Flush();
                        }

                        WriteCounts(sink);
                    }
                    catch (Exception)
                    {
                        // Never report message content, filenames or native configuration.
                        Android.Util.Log.Warn("NativeDebugLog", "Detailed log file write failed.");
                        queue.Writer.TryComplete();
                    }
                }
            });
        }

        public static NativeDebugLog Start(string path, int sizeMB = 1, bool clearOnConnect = true)
        {
            lock (Lifecycle)
            {
                if (active != null)
                {
                    throw new InvalidOperationException("Native log callback already owned.");
                }

                var log = new NativeDebugLog(path, sizeMB, clearOnConnect);
                try
                {
                    Volatile.Write(ref active, log);
                    if (SetCallback(log.callback, IntPtr.Zero) != 0)
                    {
                        throw new InvalidOperationException("Native log callback registration failed.");
                    }

                    return log;
                }
                catch
                {
                    Volatile.Write(ref active, null);
                    log.queue.Writer.TryComplete();
                    log.writer.GetAwaiter().GetResult();
                    throw;
                }
            }
        }

        public bool Stop()
        {
            lock (Lifecycle)
            {
                if (active != this)
                {
                    return true;
                }

                if (ClearCallback(IntPtr.Zero, IntPtr.Zero) != 0)
                {
                    // Caller must retain this instance/delegate until process termination.
                    return false;
                }

                queue.Writer.TryComplete();
                writer.GetAwaiter().GetResult();
                Volatile.Write(ref active, null);
                GC.KeepAlive(callback);
                return true;
            }
        }

        private static void Receive(IntPtr message, UIntPtr length, int level, IntPtr context)
        {
            Volatile.Read(ref active)?.Enqueue(message, length);
        }

        private void Enqueue(IntPtr message, UIntPtr length)
        {
            try
            {
                ulong size = length.ToUInt64();
                int count = (int)Math.Min(size, (ulong)MessageLimit);
                if (size > MessageLimit)
                {
                    Interlocked.Increment(ref truncated);
                }

                // Keep newest bytes of oversized messages. Replacement decoding handles split UTF-8.
                ulong offset = size - (ulong)count;
                if (offset > (ulong)nint.MaxValue)
                {
                    Interlocked.Increment(ref dropped);
                    return;
                }

                byte[] bytes = new byte[count];
                Marshal.Copy((IntPtr)((nint)message + (nint)offset), bytes, 0, count);
                if (!queue.Writer.TryWrite(bytes))
                {
                    Interlocked.Increment(ref dropped);
                }
            }
            catch (Exception)
            {
                Interlocked.Increment(ref dropped);
            }
        }

        private void WriteCounts(RollingLog sink)
        {
            long lost = Interlocked.Exchange(ref dropped, 0);
            long shortened = Interlocked.Exchange(ref truncated, 0);
            if (lost != 0 || shortened != 0)
            {
                sink.Write(Encoding.UTF8.GetBytes($"[app] dropped={lost}, truncated={shortened}"));
            }
        }
    }
}
