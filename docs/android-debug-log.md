# Android detailed rolling log

## Status and native dependency

App integration targets `trojan_set_log_callback` documented in core branch
`feat/android-log-callback`, initially reviewed at `a431302`. Existing packaged
libraries have NOT been replaced: enabled sessions fail closed at registration
until matching callback-capable libraries are integrated. Disabled sessions do not
resolve or call the new export. This commit is not a release-ready APK.

Coordinate native artifact replacement with separate app core49 work. Build both
ABIs from one exact approved core SHA in isolated build directories, record source
and submodule state, compiler options (SSL key logging disabled) and hashes, then
verify packaged APK contents. Do not overwrite core49 libraries without preserving
that integration. No source or submodule changes in core are needed by app code.

## Storage and lifecycle

Native logs still reach logcat under `trojan`. Only explicit enabled host sessions
also register a C callback in `:vpn_remote`; no logcat reader is used. Private
`config.json.log-host` metadata identifies host; native configuration must also
have `log_level=0`. No credentials are copied into this metadata. Removed previous
external `config_debug.json` generation because it copied credential-bearing config.
Previously created external copies are not automatically deleted by this change.

Private storage uses `Environment.SpecialFolder.Personal/debug-log/<SHA256(host-name)>.log`.
Only latest logged host's file remains; exporting another host reports no file.
Same host's enabled sessions append after process restart. Disabled sessions do not
collect, alter or export logs. Unsaved rename exports original host's file.

Callback copies at most 16 KiB per message into a 32-entry bounded queue, never
waits for disk or queue capacity. Oversized messages are truncated; full queue
messages dropped. Writer emits aggregate dropped/truncated counters, normalizes
invalid UTF-8 and NUL, and flushes each accepted message. Rolling file never exceeds
1 MiB; oldest text is removed at line/UTF-8 boundaries where practical. One writer
holds exclusive file access, so concurrent snapshots fail safely. Native return
precedes unregister (which waits for callbacks), drain/flush, stopped notification
and delayed process kill. Static delegate and active sink remain rooted if
unregistration fails, until native process exits.

## Export/privacy

Buttons require stopped state, no pending start/stop, and checked host option.
Both exports warn that logs include sensitive browsing metadata. Reads are bounded,
exclusive snapshots; repeated clicks are guarded. Share uses MAUI `ShareFile` from
private cache with platform FileProvider URI permissions, not raw file URLs.
Only one temporary share snapshot is retained; next share deletes previous snapshot,
so delayed recipients should read before another share. Clipboard intentionally
exposes confirmed text to platform clipboard policy. Core must suppress credentials,
raw payloads and TLS keys before callback delivery; app does not claim arbitrary
native message redaction.

## Checks (not run while core agent active)

```
dotnet run --project tests/LogRegression/LogRegression.csproj
dotnet run --project tests/HostEditRegression/HostEditRegression.csproj
dotnet publish TrojanPlusApp.Android/TrojanPlusApp.Android.csproj -f net10.0-android -c Release
```

Device acceptance still required after native artifacts: no-server/loopback fixture,
disabled/enabled and stop/start lifecycle, real file content/size, privacy cancel,
share recipient access and clipboard, missing log, repeated clicks and failure UI.
Coordinate shared emulator use and run builds/tests sequentially with core owner.
