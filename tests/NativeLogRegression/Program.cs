// SPDX-License-Identifier: GPL-3.0-or-later
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using TrojanPlusApp.Droid;
using TrojanPlusApp.Services;

internal static class Program
{
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "NativeLogRegression-" + Guid.NewGuid().ToString("N"));

    private static void Main()
    {
        Directory.CreateDirectory(Root);
        try
        {
            DisabledDoesNotRegister();
            BytesFlushAndRestart();
            BoundedQueueAndCounts();
            CounterOnlyFlush();
            RegistrationFailure();
            UnregistrationFailure();
            Check(Android.Util.Log.Warnings == 0, "No unexpected sink failures");
            Console.WriteLine("NativeLogRegression: all checks passed.");
        }
        finally
        {
            Directory.Delete(Root, true);
        }
    }

    private static void DisabledDoesNotRegister()
    {
        Shim.Reset();
        // NativeDebugLog has no enabled flag: disabled callers must not call Start.
        // Exercise native emission with no registration, not a fake enabled wrapper.
        Emit("disabled");
        Check(Shim.RegisterCalls() == 0 && Shim.Registered() == 0, "Disabled: no native registration");
        Check(Directory.GetFiles(Root, "*", SearchOption.AllDirectories).Length == 0, "Disabled: no files");
        Check(typeof(NativeDebugLog).GetField("active", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null) == null,
            "Disabled: no managed active logger");
    }

    private static void BytesFlushAndRestart()
    {
        Shim.Reset();
        string path = LogPath("bytes");
        var log = NativeDebugLog.Start(path);
        Check(Shim.RegisterCalls() == 1 && Shim.Registered() == 1, "Start registers native callback");
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Emit("Tiếng Việt / 中文 / 😀");
        Shim.Emit(new byte[] { 65, 0, 66, 0xFF, 67 }, (UIntPtr)5);
        Shim.Emit(Array.Empty<byte>(), UIntPtr.Zero);
        // Explicit byte length must ignore bytes beyond it, including a NUL terminator.
        Shim.Emit(Encoding.UTF8.GetBytes("only-secret"), (UIntPtr)4);
        Check(log.Stop(), "Stop succeeds");
        string expected = "Tiếng Việt / 中文 / 😀\nA\uFFFDB\uFFFDC\n\nonly\n";
        Check(Read(path) == expected, "Callback preserves UTF-8, normalizes NUL/invalid UTF-8, honors byte count; Stop drains queue");
        Check(Shim.UnregisterCalls() == 1 && Shim.Registered() == 0, "Stop unregisters callback");
        Emit("after-stop");
        Check(Read(path) == expected, "Stopped callback cannot write");
        Check(DebugLogFiles.ReadSnapshot(path).Length == Encoding.UTF8.GetByteCount(expected), "Stop releases exclusive file handle");

        var restarted = NativeDebugLog.Start(path);
        Emit("restart");
        Check(restarted.Stop(), "Restart stops");
        Check(Read(path) == expected + "restart\n", "Restart appends and flushes");
        Check(Shim.RegisterCalls() == 2 && Shim.UnregisterCalls() == 2, "Restart registers and unregisters once");
    }

    private static void BoundedQueueAndCounts()
    {
        Shim.Reset();
        string path = LogPath("bounded");
        ThreadPool.GetMinThreads(out int oldMin, out int minIo);
        ThreadPool.GetMaxThreads(out int oldMax, out int maxIo);
        using var occupied = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var exited = new ManualResetEventSlim();
        NativeDebugLog log = null;
        try
        {
            // Hold sole worker so production writer cannot consume until queue assertions finish.
            Check(ThreadPool.SetMinThreads(1, minIo), "Set worker minimum");
            Check(ThreadPool.SetMaxThreads(1, maxIo), "Set worker maximum");
            ThreadPool.QueueUserWorkItem(_ =>
            {
                occupied.Set();
                release.Wait();
                exited.Set();
            });
            Check(occupied.Wait(TimeSpan.FromSeconds(10)), "Worker gate acquired");
            log = NativeDebugLog.Start(path);
            byte[] oversized = Encoding.UTF8.GetBytes("prefix€" + new string('x', 16383));
            // Tail starts on last continuation byte of €, deliberately splitting UTF-8.
            Shim.Emit(oversized, (UIntPtr)oversized.Length);
            for (int i = 1; i < 32; i++) Emit($"queued-{i:D2}");
            for (int i = 0; i < 7; i++) Emit($"dropped-{i}");
            Shim.Emit(oversized, (UIntPtr)oversized.Length); // Full queue: both truncated and dropped.
            Shim.ImpossibleLength(); // Rejected before pointer read: both truncated and dropped.
            var queue = (Channel<byte[]>)typeof(NativeDebugLog)
                .GetField("queue", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(log)!;
            Check(queue.Reader.Count == 32, "Production queue capacity exactly 32; callbacks never wait for room");
            release.Set();
            Check(exited.Wait(TimeSpan.FromSeconds(10)), "Worker gate released");
            Check(log.Stop(), "Bounded logger drains and stops");
            log = null;
            string[] lines = Read(path).Split('\n');
            Check(lines[0] == "[app] dropped=9, truncated=3", "Exact drop/truncation accounting, including rejected oversized messages");
            Check(lines[1] == "\uFFFD" + new string('x', 16383), "Keeps newest 16 KiB; split UTF-8 gets replacement character");
            for (int i = 1; i < 32; i++) Check(lines[i + 1] == $"queued-{i:D2}", "FIFO drain");
            Check(lines.Length == 34 && lines[^1] == "", "Only accepted messages plus one counter record written");
        }
        finally
        {
            release.Set();
            ThreadPool.SetMaxThreads(oldMax, maxIo);
            ThreadPool.SetMinThreads(oldMin, minIo);
            log?.Stop();
        }
    }

    private static void CounterOnlyFlush()
    {
        Shim.Reset();
        string path = LogPath("counter-only");
        var log = NativeDebugLog.Start(path);
        Shim.ImpossibleLength();
        Check(log.Stop(), "Counter-only logger stops");
        Check(Read(path) == "[app] dropped=1, truncated=1\n", "Final drain writes counters even without accepted messages");
    }

    private static void RegistrationFailure()
    {
        Shim.Reset();
        Shim.Results(1, 0);
        string path = LogPath("registration-failure");
        bool failed = false;
        try { NativeDebugLog.Start(path); }
        catch (InvalidOperationException) { failed = true; }
        Check(failed && Shim.RegisterCalls() == 1 && Shim.Registered() == 0, "Failed native registration throws");
        Check(Read(path) == "", "Failed Start drains writer and releases file");
        Emit("ignored");
        Shim.Results(0, 0);
        var log = NativeDebugLog.Start(path);
        Emit("recovered");
        Check(log.Stop() && Read(path) == "recovered\n", "Restart after failed registration");
    }

    private static void UnregistrationFailure()
    {
        Shim.Reset();
        string path = LogPath("unregistration-failure");
        var log = NativeDebugLog.Start(path);
        Emit("before");
        Shim.Results(0, 1);
        Check(!log.Stop() && Shim.Registered() == 1, "Failed unregister retains callback");
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Emit("retained");
        Shim.Results(0, 0);
        Check(log.Stop(), "Unregister retry succeeds");
        Check(Read(path) == "before\nretained\n", "Failed Stop leaves queue open and callback usable; retry flushes");
        Check(Shim.UnregisterCalls() == 2 && Shim.Registered() == 0, "Both unregister attempts reach native ABI");
    }

    private static string LogPath(string name) => Path.Combine(Root, name, "test.log");
    private static string Read(string path) => new UTF8Encoding(false, true).GetString(File.ReadAllBytes(path));
    private static void Emit(string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        Shim.Emit(bytes, (UIntPtr)bytes.Length);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static class Shim
    {
        [DllImport("trojan.so", EntryPoint = "shim_reset", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Reset();
        [DllImport("trojan.so", EntryPoint = "shim_results", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Results(int registration, int unregistration);
        [DllImport("trojan.so", EntryPoint = "shim_registered", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Registered();
        [DllImport("trojan.so", EntryPoint = "shim_register_calls", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int RegisterCalls();
        [DllImport("trojan.so", EntryPoint = "shim_unregister_calls", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int UnregisterCalls();
        [DllImport("trojan.so", EntryPoint = "shim_emit", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Emit(byte[] bytes, UIntPtr length);
        [DllImport("trojan.so", EntryPoint = "shim_emit_impossible_length", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void ImpossibleLength();
    }
}
