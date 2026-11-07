# Native callback regression checks

Links actual production `NativeDebugLog.cs`, `RollingLog.cs`, and `DebugLogFiles.cs` into a plain .NET console program. No NuGet test framework, Android SDK, MAUI workload, device, or source-core changes needed. Android stubs supply only attribute/logging signatures. Tiny C shim supplies callback registration and emission through the real P/Invoke/reverse-P/Invoke boundary; it does not implement queueing, decoding, accounting, or file handling.

## Run (Linux host)

Prerequisites: .NET 10 SDK and `cc` supporting C11/shared libraries. Wait for other native builds to finish; run sequentially from repository root:

```bash
timeout 120s dotnet run --project tests/NativeLogRegression/NativeLogRegression.csproj
```

MSBuild compiles `shim.c` into project output as `libtrojan.so` before managed compilation. .NET resolves `DllImport("trojan.so")` from that output directory. No installation or replacement of committed Android native libraries. Temporary logs use a unique directory and are deleted on normal exit. Timeout is intentional: a regression that makes callbacks block on a full queue must fail rather than hang CI forever.

## Coverage

- No logger started: no native registration, no active instance, no file; unregistered native emissions ignored.
- Start/Stop call native registration/unregistration; callback survives managed GC.
- Explicit native byte lengths, valid multibyte UTF-8, invalid UTF-8, embedded NUL, zero-length messages.
- Real bounded channel holds exactly 32 items, FIFO; excess callbacks return without waiting. Test temporarily holds the sole thread-pool worker so production writer cannot race queue assertions; settings restored afterward.
- Oversized messages retain newest 16 KiB, including replacement decoding at split UTF-8 boundary.
- Exact drop/truncation counts for full queue, oversized dropped messages, and unrepresentable pointer offsets rejected before dereference.
- Stop drains accepted messages and counters (also when no message was accepted), releases file for export, unregisters, and ignores subsequent native emissions.
- Restart appends with new registration; registration failure throws and releases writer/file; failed unregister retains usable callback/queue until successful retry.

Disabled capture belongs to `TrojanPlusVPNService.WorkerThread.Run`'s `debugLogPath != null` guard, not `NativeDebugLog`. This host test verifies no-Start behavior, not Android service settings/control flow. It does not claim to test real core callback locking/concurrent unregister, native ABI absence, Android lifecycle, or actual native logging content.

No production fix proposed by these checks; production files remain untouched. Tests have not been executed during authoring because native builds must remain sequential.
