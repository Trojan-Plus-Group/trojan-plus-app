# Log regression checks

Run from repository root with .NET 10 SDK:

```sh
dotnet run --project tests/LogRegression/LogRegression.csproj
```

Dependency-free console checks compile production `RollingLog.cs` and `DebugLogFiles.cs` directly. Failed checks print exceptions and return exit code 1. Files use a unique temporary directory, removed on completion.

Coverage: repeated small writes beyond 1 MiB, single oversized writes, invalid UTF-8 and NUL normalization, multibyte truncation and rolling boundaries, capacity limits, oversized existing files, restart persistence, flush visibility before disposal, exclusive concurrent export rejection, missing/empty exports, and all `CanExport` input combinations.

`CanExport` is tested as a policy gate; `ReadSnapshot` does not accept enabled/stopped state and does not enforce that gate itself.
