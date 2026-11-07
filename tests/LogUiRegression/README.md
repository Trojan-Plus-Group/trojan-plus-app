# Log UI handler regression

```sh
dotnet run --project tests/LogUiRegression/LogUiRegression.csproj
```

Run only after native builds finish. No test framework, MAUI workload, or NuGet package required beyond .NET 10 SDK.

Links actual production `HostEditPage.xaml.cs`, `DebugLogFiles.cs`, and `RollingLog.cs`. Partial-class wrappers call actual copy/share event handlers. Lightweight MAUI, app-state, model, and view-model stubs isolate handler behavior; file reads/writes use real temporary files and a unique host ID. Cleanup removes only files created by this check.

Checks hidden-page gating, all stopped-state inputs, disabled option, privacy cancel, missing files, repeated clicks while confirmation is pending, VPN start/page disappearance during confirmation, successful UTF-8 copy, original-host lookup after unsaved rename, successful share metadata/content, and stale share cleanup.

This is runtime handler coverage, not runtime MAUI UI coverage. XAML bindings, visual visibility/enabled state, real dialogs, OS clipboard, and platform share chooser still require device checks. Fake platform tasks complete synchronously; pending privacy tasks are explicitly released to test reentrancy without sleeps.
