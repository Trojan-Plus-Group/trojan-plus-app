# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Trojan Plus App is a cross-platform mobile VPN/proxy client for the Trojan Plus protocol, built with .NET MAUI for Android and iOS. The project uses native C/C++ Trojan Plus core library for high-performance packet processing (2x faster than Go-based alternatives). The app provides a Shadowsocks-like user experience with route rules support.

**Important Protocol Constraint**: This app ONLY supports Trojan Plus and original Trojan protocols. There are no plans to support other protocols (Shadowsocks, V2ray, etc.).

## Technology Stack

- **Framework**: .NET MAUI 10.0.1 with .NET 10.0 (migrated from Xamarin.Forms in 2026)
- **Architecture**: MVVM pattern with shared UI and platform-specific implementations
- **Native Core**: Trojan Plus C/C++ library (libtrojan.so/.a) via P/Invoke
- **Platforms**: Android (working), iOS (code complete, needs testing)
- **Build System**: dotnet CLI

## Build Commands

### Full Solution
```bash
dotnet restore TrojanPlusApp.sln
dotnet build TrojanPlusApp.sln --configuration Release
dotnet clean TrojanPlusApp.sln
```

### Android Build & Install
```bash
# Quick build and install to connected device/emulator
chmod +x build_and_install.sh
./build_and_install.sh
```

### iOS Build
```bash
# Set your Apple Developer certificate (required for device builds)
export IOS_CODESIGN_KEY="Apple Development: Your Name (TEAM_ID)"

# Build main app
dotnet build TrojanPlusApp.iOS/TrojanPlusApp.iOS.csproj --configuration Debug

# Build Network Extension
dotnet build TrojanPlusApp.iOS.Extension/TrojanPlusApp.iOS.Extension.csproj --configuration Debug

# Note: iOS requires physical device for VPN testing (simulator not supported)
# Find your certificate identity: security find-identity -v -p codesigning
```

### Platform-Specific Builds
```bash
# Android
dotnet build TrojanPlusApp.Android/TrojanPlusApp.Android.csproj --configuration Release

# iOS
dotnet build TrojanPlusApp.iOS/TrojanPlusApp.iOS.csproj --configuration Release
```

## Architecture Overview

### MVVM Structure
The app follows strict MVVM pattern with clear separation:

- **TrojanPlusApp/** (Shared MAUI project)
  - **Models/**: Data models with validation logic
    - `HostModel`: Server configuration with TUN interface constants
    - `SettingsModel`: App settings and route rules
    - `NotificationModel`: Base class for property change notifications
  - **ViewModels/**: Inherit from `BaseViewModel` or `NotificationModel`
    - Use `SetProperty()` for property changes to trigger UI updates
  - **Views/**: XAML pages with code-behind
    - `MainPage`: FlyoutPage (replaced MasterDetailPage from Xamarin)
    - `HostsPage`: Server list management
    - `HostEditPage`: Server configuration editor
    - `SettingsPage`: Route rules and app settings
  - **Services/**: `DataStore` for JSON-based data persistence
  - **Behaviors/**: XAML validation behaviors (IP, port, hostname)
  - **Resx/**: Localization resources

### Platform-Specific Architecture

#### Android (TrojanPlusApp.Android/)
The Android implementation uses a **separate process architecture** for VPN service isolation:

- **TrojanPlusVPNService**: Main VPN service running in `:vpn_remote` process
  - Runs as foreground service with notification
  - Establishes VPN tunnel using Android VpnService API
  - Integrates with native libtrojan.so via P/Invoke
  - Uses TUN interface (constants in `HostModel`: TunGateWayIP, TunNetIP, TunMtu)

- **TrojanPlusStarter**: Helper class to start/stop VPN service from main process
  - Handles inter-process communication
  - Manages service lifecycle

- **TrojanPlusNotification**: Persistent notification for foreground service
  - Required for Android 8.0+ foreground services
  - Shows connection status

- **TrojanPlusAutoJobService**: JobScheduler service for auto-start functionality

**Why Separate Process?**: VPN service runs in `:vpn_remote` process to isolate native library crashes and memory usage from the main UI process, improving stability.

#### iOS (TrojanPlusApp.iOS/ + TrojanPlusApp.iOS.Extension/)
The iOS implementation uses **Network Extension framework** with App Extension architecture:

**Main App (TrojanPlusApp.iOS/)**:
- **TrojanPlusVPNManager**: VPN lifecycle management using NEVPNManager
  - Configures and starts/stops VPN tunnel
  - Monitors connection status
  - Saves configuration to App Group

- **TrojanConfigGenerator**: Generates trojan config JSON
  - Converts SettingsModel to trojan configuration
  - Saves to shared App Group container

- **SharedDataManager**: App Groups data sharing
  - Provides access to shared container
  - Manages configuration persistence

- **NativeInterop**: P/Invoke declarations for native library
  - trojan_run_main(), trojan_stop_main(), trojan_get_version()

**Network Extension (TrojanPlusApp.iOS.Extension/)**:
- **PacketTunnelProvider**: VPN tunnel implementation
  - Implements NEPacketTunnelProvider
  - Configures TUN interface (10.233.233.1/10.233.233.2)
  - Gets TUN file descriptor from PacketFlow
  - Runs trojan_run_main() in background thread
  - Reads config from App Group container

**Why App Extension?**: iOS requires VPN functionality to run in a Network Extension app extension, which runs in a separate process with restricted capabilities for security and stability.

**Status**: Code complete, requires Apple Developer configuration and physical device testing.

### Native Library Integration

The app integrates native Trojan Plus C/C++ library (libtrojan.so) for packet processing:

- **Location**: `TrojanPlusApp.Android/lib/{arch}/libtrojan.so`
  - arm64-v8a (primary)
  - x86, x86_64 (emulator support)

- **Integration**: P/Invoke (Platform Invoke) for C# to C++ interop
  - Native methods declared with `[DllImport]`
  - Marshaling between managed and unmanaged memory

- **Performance**: Native C/C++ processing provides 2x speed improvement over Go-based implementations

### Custom Messenger System

The app uses a custom `Messenger` class (replaced Xamarin's MessagingCenter):
- Provides pub/sub messaging between ViewModels and platform services
- Used for VPN state changes, connection status updates
- Decouples UI from platform-specific code

### Data Persistence

`DataStore` service handles all data persistence:
- JSON serialization with Newtonsoft.Json
- Stores host configurations and settings
- Uses MAUI Preferences API for simple key-value storage

### Route Rules System

Similar to Shadowsocks, supports multiple routing modes:
- Route all traffic through VPN
- Bypass China mainland IPs (uses china_ip_list)
- GFW domain list routing (uses gfwlist2dnsmasq)
- Custom rules

## Important Design Decisions

1. **Battery Consumption**: High battery usage is expected and unavoidable. All network traffic from the entire device is routed through the app when active, making it a "battery killer" by design.

2. **No Automated Tests**: The project currently has no unit or integration tests. All testing is manual on physical devices and emulators.

3. **GPL License**: All code is GPLv3. Maintain license headers in all source files.

4. **Security**: Never log sensitive information (passwords, keys). SSL certificate validation is critical for Trojan protocol.

5. **Platform Conditionals**: Use `#if __ANDROID__` / `#elif __IOS__` for platform-specific code in shared project.

## Code Style and Guidelines

For detailed code style guidelines, naming conventions, formatting rules, and MVVM implementation patterns, see [AGENTS.md](AGENTS.md).

Key points:
- 4 spaces indentation, CRLF line endings
- PascalCase for types/methods/properties, camelCase with underscore for private fields
- Use `async`/`await` consistently
- Follow existing patterns in the codebase
- StyleCop analysis runs during build

## Migration Context

The project was migrated from Xamarin.Forms to .NET MAUI in 2026:
- UWP support was dropped
- MessagingCenter → Custom Messenger
- Properties → Preferences API
- MasterDetailPage → FlyoutPage
- See [MAUI_MIGRATION_PLAN.md](MAUI_MIGRATION_PLAN.md) for details

## Dependencies

- Trojan Plus core library (native C/C++)
- Microsoft.Maui.Controls and Microsoft.Maui.Essentials (built-in)
- Newtonsoft.Json for serialization
- China mainland IP list and GFW domain list for routing
