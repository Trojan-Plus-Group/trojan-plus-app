# AGENTS.md

Trojan Plus App: .NET MAUI (net10.0, MAUI 10.0.1) VPN client for the Trojan Plus protocol. Android ships;
iOS code exists but is untested. The proxy itself is the native trojan-plus core (`libtrojan.so`), called through
P/Invoke. The app supports only the Trojan / Trojan Plus protocols.

## Layout
- `TrojanPlusApp/`: shared UI (MVVM). `Models/HostModel.cs` builds the core config (`PrepareConfig`,
  `ConfigTemplate`); `Services/DataStore.cs` persists hosts/settings as JSON in MAUI `Preferences`;
  `Messenger.cs` is the pub/sub used instead of MessagingCenter; `Resx/` holds en + zh strings and bundled data
  (CA bundle, gfwlist, CN IP list; regenerate with `Resources/update.sh`).
- `TrojanPlusApp.Android/`: `TrojanPlusVPNService` runs in the separate `:vpn_remote` process and calls the core;
  `TrojanPlusStarter` talks to it over IPC. Custom handlers are registered in `MainApplication.cs`.
- `TrojanPlusApp.iOS/` + `TrojanPlusApp.iOS.Extension/`: NEPacketTunnelProvider; needs Apple signing
  (`.env.example`) and a physical device.
- `tests/HostEditRegression/`: console check that compiles the production model/store/view-model sources.

## Build and test
```bash
dotnet publish TrojanPlusApp.Android/TrojanPlusApp.Android.csproj -f net10.0-android -c Release
dotnet run --project tests/HostEditRegression/HostEditRegression.csproj
./build_and_install.sh   # publish + adb install to the first connected device
```
- CI (`azure-pipelines.Android.yml`) builds Android only and publishes the signed APK as artifact `AndroidAPK`.
- Keep the Android build at 0 warnings from project code (only the toolchain warning XA0033 may appear). Fix
  warnings in code; suppress only with a targeted attribute/pragma and a reason.
- UI changes need an emulator/device check; there is no UI test suite.

## Rules that are easy to break
- Saved data: `HostModel` and `SettingsModel` are serialized with Newtonsoft.Json. Never rename or remove their
  public properties; new options need a safe default (a property initializer survives deserialization of old
  data). UI-only members get `[JsonIgnore]`.
- Hosts are keyed by `HostName`. Renaming must update other hosts' `LoadBalance` lists and
  `Settings.HostRunningName`; names must stay unique.
- Native core: `TrojanPlusApp.Android/lib/{arm64-v8a,x86_64}/libtrojan.so` are prebuilt from trojan-plus and
  committed. The csproj removes auto-imported `.so` items and lists both explicitly; keep that. New config keys
  need a matching core build.
- The core needs a fresh process per VPN session; keep the delayed process kill in `TrojanPlusVPNService`.
- `TrojanPlusApp.csproj` has `<Nullable>enable</Nullable>`: annotate with `?`, and use `!` only where the value is
  non-null by lifecycle.
- minSdk is 24: guard newer APIs with `OperatingSystem.IsAndroidVersionAtLeast(n)`.
- User-visible strings go in both `TextResource.resx` and `TextResource.zh.resx`, plus `TextResource.Designer.cs`.
- Never log passwords or full configs. Keep SSL verification on by default.
- Debug logs: per-host Warning/Info/All and 1/5/10/20/50 MB cap (defaults Warning/1 MB).
  Existing `.log-host` JSON carries host and size to `:vpn_remote`; keep trimming buffer bounded.

## Style
- Follow `.editorconfig` and StyleCop (runs in the build). Match the surrounding file: most files put `using`
  directives inside the namespace and use 4-space indents; private fields are camelCase without a prefix.
- New source files get the GPL-3.0 license header (SPDX form is fine).
- Repo text (code, comments, docs, commits, PRs) is English; Chinese only in `*.zh.resx`.
- Version: `ApplicationDisplayVersion`/`ApplicationVersion` in both csproj files and
  `versionName`/`versionCode` in `TrojanPlusApp.Android/Properties/AndroidManifest.xml`; bump all together.
