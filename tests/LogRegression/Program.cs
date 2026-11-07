// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text;
using TrojanPlusApp.Services;

string root = Path.Combine(Path.GetTempPath(), "TrojanPlus-LogRegression-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
var strictUtf8 = new UTF8Encoding(false, true);
var tests = new (string Name, Action Run)[]
{
    ("small writes roll beyond 1 MiB", SmallWrites),
    ("small appends after overflow do not trim again", AmortizedTrim),
    ("single write beyond low-water room", LargeAppend),
    ("single write beyond 1 MiB", LargeWrite),
    ("invalid UTF-8 and NUL normalization", InvalidUtf8),
    ("multibyte input truncation boundary", MultibyteInput),
    ("multibyte rolling boundary", MultibyteRolling),
    ("capacity and snapshot bounds", Bounds),
    ("supported sizes and unknown-size fallback", SizeOptions),
    ("per-host caps retain newest whole lines", SizedWrites),
    ("50 MiB construction uses bounded allocation", ConstructionAllocation),
    ("streamed trim finds distant newline and preserves UTF-8 fallback", StreamedTrim),
    ("restart preserves data and writes flush", RestartAndFlush),
    ("concurrent export rejected while writer owns file", ExclusiveExport),
    ("missing and empty exports rejected", Unavailable),
    ("CanExport truth table", ExportGate),
};
int failures = 0;
try
{
    foreach (var test in tests)
    {
        try
        {
            test.Run();
            Console.WriteLine($"PASS {test.Name}");
        }
        catch (Exception error)
        {
            failures++;
            Console.Error.WriteLine($"FAIL {test.Name}: {error}");
        }
    }
}
finally
{
    Directory.Delete(root, true);
}

Console.WriteLine($"{tests.Length - failures}/{tests.Length} checks passed.");
return failures == 0 ? 0 : 1;

string LogPath(string name) => Path.Combine(root, name + ".log");

void Check(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

void Throws<T>(Action action) where T : Exception
{
    try
    {
        action();
    }
    catch (T)
    {
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

string Snapshot(string path, int sizeMB = 1)
{
    byte[] bytes = DebugLogFiles.ReadSnapshot(path, sizeMB);
    Check(bytes.Length > 0 && bytes.Length <= RollingLog.CapacityForSize(sizeMB), "Snapshot outside bounds.");
    Check(!bytes.Contains((byte)0), "Snapshot contains NUL.");
    return strictUtf8.GetString(bytes);
}

void SmallWrites()
{
    string path = LogPath("small");
    var lines = Enumerable.Range(0, 1600).Select(i => $"{i:D4}:" + new string('x', 1018) + "\n").ToArray();
    Check(lines.Sum(line => line.Length) > RollingLog.Capacity, "Fixture must exceed capacity.");
    using (var log = new RollingLog(path))
    {
        foreach (string line in lines)
        {
            log.Write(Encoding.UTF8.GetBytes(line[..^1]));
            log.Flush();
            long length = new FileInfo(path).Length;
            Check(length <= RollingLog.Capacity, "File exceeded capacity during writes.");
            if (int.Parse(line.AsSpan(0, 4)) >= 1024)
            {
                Check(length >= 768 * 1024, "Overflow retained less than 768 KiB.");
            }
        }
    }

    string text = Snapshot(path);
    Check(text.EndsWith(lines[^1], StringComparison.Ordinal), "Newest small write lost.");
    Check(!text.Contains(lines[0], StringComparison.Ordinal), "Oldest small write not evicted.");
    string[] retained = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    int first = int.Parse(retained[0].AsSpan(0, 4));
    Check(text == string.Concat(lines.Skip(first)), "Retained small writes are not a contiguous whole-line tail.");
}

void AmortizedTrim()
{
    string path = LogPath("amortized");
    File.WriteAllBytes(path, Enumerable.Repeat((byte)'x', RollingLog.Capacity).ToArray());
    using var log = new RollingLog(path);
    log.Write("overflow"u8);
    log.Flush();
    long trimmedLength = new FileInfo(path).Length;
    Check(trimmedLength >= 768 * 1024 && trimmedLength < RollingLog.Capacity, "Overflow did not reach low-water range.");
    for (int i = 1; i <= 1000; i++)
    {
        log.Write("small"u8);
        log.Flush();
        Check(new FileInfo(path).Length == trimmedLength + (i * 6), "Small append trimmed again instead of growing by input size.");
    }
}

void LargeAppend()
{
    string path = LogPath("large-append");
    File.WriteAllBytes(path, Enumerable.Repeat((byte)'x', RollingLog.Capacity).ToArray());
    string incoming = new string('y', 300 * 1024);
    using (var log = new RollingLog(path))
    {
        log.Write(Encoding.UTF8.GetBytes(incoming));
    }

    Check(Snapshot(path).EndsWith(incoming + "\n", StringComparison.Ordinal), "Write larger than trim room lost newest bytes.");
}

void LargeWrite()
{
    string path = LogPath("large");
    byte[] bytes = Encoding.UTF8.GetBytes(new string('a', RollingLog.Capacity + 123) + "latest");
    using (var log = new RollingLog(path))
    {
        log.Write(bytes);
    }

    string expected = Encoding.UTF8.GetString(bytes.AsSpan(bytes.Length - RollingLog.Capacity + 1)) + "\n";
    Check(Snapshot(path) == expected, "Oversized write must retain bounded newest input tail.");
}

void InvalidUtf8()
{
    string path = LogPath("invalid");
    byte[] input = { (byte)'A', 0, 0xFF, 0xC3, (byte)'(', 0xE2, 0x82 };
    using (var log = new RollingLog(path))
    {
        log.Write(input);
    }

    string expected = Encoding.UTF8.GetString(input).Replace('\0', '\uFFFD') + "\n";
    Check(Snapshot(path) == expected, "Malformed UTF-8 or NUL not normalized as replacement characters.");
}

void MultibyteInput()
{
    foreach (string symbol in new[] { "é", "€", "😀" })
    {
        string path = LogPath("input-" + symbol.Length + "-" + Encoding.UTF8.GetByteCount(symbol));
        byte[] input = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat(symbol, RollingLog.Capacity)) + "END");
        int start = input.Length - RollingLog.Capacity + 1;
        while ((input[start] & 0xC0) == 0x80)
        {
            start++;
        }

        using (var log = new RollingLog(path))
        {
            log.Write(input);
        }

        string text = Snapshot(path);
        Check(text == strictUtf8.GetString(input.AsSpan(start)) + "\n", "Input truncation split a UTF-8 character.");
        Check(!text.Contains('\uFFFD'), "Valid input produced replacement characters.");
    }
}

void MultibyteRolling()
{
    string path = LogPath("rolling-utf8");
    string line = string.Concat(Enumerable.Repeat("é€😀", 457));
    byte[] bytes = Encoding.UTF8.GetBytes(line);
    using (var log = new RollingLog(path))
    {
        for (int i = 0; i < 400; i++)
        {
            log.Write(bytes);
        }
    }

    string text = Snapshot(path);
    Check((bytes.Length + 1) * 400 > RollingLog.Capacity, "Fixture must roll.");
    Check(text.Split('\n', StringSplitOptions.RemoveEmptyEntries).All(value => value == line), "Rolling split a line or multibyte character.");
    Check(!text.Contains('\uFFFD'), "Rolling introduced invalid UTF-8.");
}

void Bounds()
{
    string path = LogPath("bounds");
    File.WriteAllBytes(path, Enumerable.Repeat((byte)'x', RollingLog.Capacity).ToArray());
    Check(DebugLogFiles.ReadSnapshot(path).Length == RollingLog.Capacity, "Exact-capacity export rejected.");
    using (var log = new RollingLog(path))
    {
        log.Write("tail"u8);
    }

    Check(Snapshot(path).EndsWith("tail\n", StringComparison.Ordinal), "Capacity boundary lost newest write.");
    File.WriteAllBytes(path, new byte[RollingLog.Capacity + 1]);
    Throws<IOException>(() => DebugLogFiles.ReadSnapshot(path));
    using (var log = new RollingLog(path))
    {
        Check(new FileInfo(path).Length == 0, "Oversized existing file not reset.");
        log.Write("reset"u8);
    }

    Check(Snapshot(path) == "reset\n", "Reset retained oversized file contents.");
}

void SizeOptions()
{
    foreach (int sizeMB in new[] { 1, 5, 10, 20, 50, 0, -1, 2, int.MaxValue })
    {
        int normalized = sizeMB is 1 or 5 or 10 or 20 or 50 ? sizeMB : 1;
        int capacity = normalized * RollingLog.Capacity;
        Check(RollingLog.NormalizeSizeMB(sizeMB) == normalized, "Unexpected normalized size.");
        Check(RollingLog.CapacityForSize(sizeMB) == capacity, "Unexpected size capacity.");
        string path = LogPath("size-" + sizeMB);
        using (var file = File.Create(path))
        {
            file.SetLength(capacity);
        }

        Check(DebugLogFiles.ReadSnapshot(path, sizeMB).Length == capacity, "Exact per-host cap rejected.");
        using (var log = new RollingLog(path, sizeMB))
        {
            Check(new FileInfo(path).Length == capacity, "Exact per-host cap reset.");
        }

        using (var file = File.OpenWrite(path))
        {
            file.SetLength(capacity + 1L);
        }

        Throws<IOException>(() => DebugLogFiles.ReadSnapshot(path, sizeMB));
        using (var log = new RollingLog(path, sizeMB))
        {
            Check(new FileInfo(path).Length == 0, "Oversized per-host log not reset.");
            log.Write("reset"u8);
        }

        Check(Snapshot(path, sizeMB) == "reset\n", "Per-host reset lost newest write.");
    }
}

void SizedWrites()
{
    foreach (int sizeMB in new[] { 1, 5 })
    {
        int capacity = RollingLog.CapacityForSize(sizeMB);
        string path = LogPath("sized-writes-" + sizeMB);
        int count = capacity / 1024 + 400;
        var lines = Enumerable.Range(0, count).Select(i => $"{i:D5}:" + new string('x', 1017) + "\n").ToArray();
        Check(lines.Sum(line => line.Length) > capacity, "Sized fixture must exceed cap.");
        using (var log = new RollingLog(path, sizeMB))
        {
            foreach (string line in lines)
            {
                log.Write(Encoding.UTF8.GetBytes(line[..^1]));
                log.Flush();
                Check(new FileInfo(path).Length <= capacity, "Sized write exceeded cap.");
            }
        }

        string text = Snapshot(path, sizeMB);
        int first = int.Parse(text.AsSpan(0, 5));
        Check(first > 0 && text == string.Concat(lines.Skip(first)), "Sized writes lost newest whole-line tail.");
        Check(Encoding.UTF8.GetByteCount(text) >= capacity * 3 / 4, "Sized trim ignored 75% low-water mark.");
        if (sizeMB > 1)
        {
            Throws<IOException>(() => DebugLogFiles.ReadSnapshot(path));
        }

        byte[] oversized = Encoding.UTF8.GetBytes(new string('z', capacity + 123) + "latest");
        using (var log = new RollingLog(path, sizeMB))
        {
            log.Write(oversized);
        }

        string expected = Encoding.UTF8.GetString(oversized.AsSpan(oversized.Length - capacity + 1)) + "\n";
        Check(Snapshot(path, sizeMB) == expected, "Sized oversized message lost bounded newest tail.");
    }
}

void ConstructionAllocation()
{
    string path = LogPath("allocation");
    // Warm FileStream setup before measuring only construction, not writes or snapshots.
    using (var warmup = new RollingLog(path))
    {
    }

    long before = GC.GetAllocatedBytesForCurrentThread();
    using var log = new RollingLog(path, 50);
    long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    Check(allocated < 256 * 1024, $"50 MiB construction allocated {allocated} bytes.");
}

void StreamedTrim()
{
    foreach (bool newline in new[] { false, true })
    {
        string path = LogPath("streamed-" + newline);
        // Tail starts inside a multibyte character; first newline lies beyond one trim buffer.
        string original = "x" + string.Concat(Enumerable.Repeat("é€", (RollingLog.Capacity - 1) / 5));
        if (newline)
        {
            original = original[..160000] + "\n" + original[160001..];
        }

        byte[] bytes = Encoding.UTF8.GetBytes(original);
        File.WriteAllBytes(path, bytes);
        byte[] incoming = Encoding.UTF8.GetBytes("newest\n");
        int keep = RollingLog.Capacity * 3 / 4;
        int start = bytes.Length - keep;
        while ((bytes[start] & 0xC0) == 0x80)
        {
            start++;
        }

        int nextLine = Array.IndexOf(bytes, (byte)'\n', start);
        if (nextLine >= 0)
        {
            start = nextLine + 1;
        }

        using (var log = new RollingLog(path))
        {
            log.Write(incoming.AsSpan(0, incoming.Length - 1));
        }

        Check(Snapshot(path) == strictUtf8.GetString(bytes.AsSpan(start)) + "newest\n", "Streamed trim changed retained UTF-8 tail.");
    }
}

void RestartAndFlush()
{
    string path = LogPath("restart");
    using (var log = new RollingLog(path))
    {
        log.Write("first"u8);
        log.Flush();
        Check(new FileInfo(path).Length == 6, "Explicit flush did not persist write.");
    }

    Check(Snapshot(path) == "first\n", "First session not persisted.");
    using (var log = new RollingLog(path))
    {
        log.Write("second"u8);
    }

    Check(Snapshot(path) == "first\nsecond\n", "Restart did not append existing log.");
}

void ExclusiveExport()
{
    string path = LogPath("exclusive");
    using (var log = new RollingLog(path))
    {
        log.Write("owned"u8);
        Task.Run(() => Throws<IOException>(() => DebugLogFiles.ReadSnapshot(path))).GetAwaiter().GetResult();
        Throws<IOException>(() =>
        {
            using var second = new RollingLog(path);
        });
    }

    Check(Snapshot(path) == "owned\n", "Export still rejected after writer disposed.");
    using (var reader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
    {
        Task.Run(() => Throws<IOException>(() => DebugLogFiles.ReadSnapshot(path))).GetAwaiter().GetResult();
    }

    Check(Snapshot(path) == "owned\n", "Exclusive export lock not released.");
}

void Unavailable()
{
    string path = LogPath("unavailable");
    Throws<FileNotFoundException>(() => DebugLogFiles.ReadSnapshot(path));
    using (File.Create(path))
    {
    }

    Throws<IOException>(() => DebugLogFiles.ReadSnapshot(path));
    File.WriteAllText(path, "available\n");
    Check(Snapshot(path) == "available\n", "Failed export leaked handle.");
}

void ExportGate()
{
    Check(!DebugLogFiles.CanExport(false, false), "Disabled/running export allowed.");
    Check(!DebugLogFiles.CanExport(false, true), "Disabled/stopped export allowed.");
    Check(!DebugLogFiles.CanExport(true, false), "Enabled/running export allowed.");
    Check(DebugLogFiles.CanExport(true, true), "Enabled/stopped export rejected.");
}
