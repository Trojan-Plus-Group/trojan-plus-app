# AGENTS.md - Coding Guidelines for Trojan Plus App

This document provides comprehensive guidelines for coding agents working on the Trojan Plus App, a cross-platform mobile VPN/proxy client built with .NET MAUI for Android and iOS.

## Project Overview

Trojan Plus App is a mobile client for the Trojan Plus VPN protocol, supporting Android and iOS platforms. It uses .NET MAUI for cross-platform UI and native platform-specific implementations.

## MAUI Migration (2026)

The project has been successfully migrated from Xamarin.Forms to .NET MAUI:

- **Framework**: Updated to .NET 10.0 with MAUI 10.0.1
- **Removed Platforms**: UWP support discontinued
- **API Updates**: MessagingCenter → Custom Messenger, Properties → Preferences, MasterDetailPage → FlyoutPage
- **Build System**: Migrated from MSBuild to dotnet CLI
- **Dependencies**: All packages updated to MAUI-compatible versions

## Build Commands

### Full Solution Build
```bash
dotnet build TrojanPlusApp.sln --configuration Release
```

### Platform-Specific Builds

#### Android
```bash
dotnet build TrojanPlusApp.Android/TrojanPlusApp.Android.csproj --configuration Release
```

#### iOS
```bash
dotnet build TrojanPlusApp.iOS/TrojanPlusApp.iOS.csproj --configuration Release
```

### Package Restore
```bash
dotnet restore TrojanPlusApp.sln
```

### Clean Build
```bash
dotnet clean TrojanPlusApp.sln
dotnet build TrojanPlusApp.sln --configuration Release
```

### Linting
```bash
# StyleCop analysis is included in build process
dotnet build TrojanPlusApp/TrojanPlusApp.csproj --no-restore
```

## Testing

Currently, there are no automated unit or integration tests configured. Manual testing is performed on physical devices and emulators.

## Code Style Guidelines

### General Principles

- Follow the established patterns in the codebase
- Use meaningful, descriptive names
- Keep methods focused on single responsibilities
- Use proper error handling and validation
- Follow the MVVM pattern consistently
- Use async/await for asynchronous operations

### Formatting (from .editorconfig)

#### Indentation and Spacing
- Use 4 spaces for indentation
- Use CRLF line endings
- Maximum line length: 120 characters
- Place opening braces on new lines for all control structures

#### Naming Conventions
- **Types**: PascalCase (e.g., `HostModel`, `SettingsViewModel`)
- **Interfaces**: PascalCase with 'I' prefix (e.g., `INotifyPropertyChanged`)
- **Methods**: PascalCase (e.g., `PrepareConfig()`, `IsValid()`)
- **Properties**: PascalCase (e.g., `HostName`, `SSLVerify`)
- **Fields**: camelCase with underscore prefix for private fields (e.g., `private string title`)
- **Constants**: PascalCase (e.g., `TunGateWayIP`)
- **Enums**: PascalCase (e.g., `RouteType.Route_all`)

#### C# Specific Styles
- Use `var` for built-in types where the type is apparent
- Prefer expression-bodied members for simple getters/setters:
  ```csharp
  public Color UI_SelectedColor => UI_Selected ? Color.Black : Color.LightGray;
  ```
- Use object initializers and collection initializers
- Prefer conditional expressions over assignments
- Use null propagation operator (`?.`)
- Use pattern matching where appropriate
- Prefer `is null` check over reference equality method
- Use simplified boolean expressions
- Use simplified interpolation
- Prefer `readonly` fields where appropriate

### Imports and Using Directives

- Place `using` directives outside namespaces
- Group system directives first, then third-party, then project-specific
- Remove unused `using` directives
- Use fully qualified names when there are conflicts

Example:
```csharp
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Maui.Essentials;
using Microsoft.Maui.Controls;

using TrojanPlusApp.Behaviors;
using TrojanPlusApp.ViewModels;
```

### Comments and Documentation

- Use XML documentation comments for public APIs
- Use single-line comments for implementation details
- Avoid unnecessary comments that restate what the code does
- Use TODO comments for planned improvements
- GPL license headers are required for all source files

### Error Handling

- Use try-catch blocks for expected exceptions
- Validate input parameters, especially for public methods
- Use custom exceptions for business logic errors
- Log errors appropriately (currently uses basic logging)
- Handle platform-specific exceptions (Android/iOS/UWP differences)

### Asynchronous Programming

- Use `async`/`await` pattern consistently
- Name async methods with `Async` suffix
- Use `Task` for fire-and-forget operations
- Handle exceptions in async methods properly
- Avoid blocking calls in UI threads

### MVVM Pattern Implementation

#### ViewModels
- Inherit from `BaseViewModel` or `NotificationModel`
- Implement `INotifyPropertyChanged` via base classes
- Use `SetProperty` method for property changes:
  ```csharp
  private string title = string.Empty;
  public string Title
  {
      get { return title; }
      set { SetProperty(ref title, value); }
  }
  ```

#### Models
- Use data validation methods (e.g., `IsValid()`)
- Implement serialization support with `JsonIgnore` for UI properties
- Use computed properties for UI display logic

#### Views
- Use XAML for UI definition
- Bind to ViewModel properties
- Use platform-specific renderers when needed

### Platform-Specific Code

- Use conditional compilation for platform differences:
  ```csharp
  #if __ANDROID__
      // Android-specific code
  #elif __IOS__
      // iOS-specific code
  #endif
  ```

- Implement platform interfaces in respective projects
- Use dependency injection for platform services

### File Organization

- **Models/**: Data models and business logic
- **ViewModels/**: MVVM view models
- **Views/**: XAML pages and code-behind
- **Services/**: Application services (data store, etc.)
- **Behaviors/**: Xamarin.Forms behaviors for validation
- **Resx/**: Resource files for localization

### Security Considerations

- Never log sensitive information (passwords, keys)
- Validate SSL certificates properly
- Use secure storage for sensitive data
- Follow principle of least privilege
- Validate all user inputs

### Performance Guidelines

- Use lazy loading for expensive operations
- Cache frequently accessed data
- Minimize UI thread blocking
- Use appropriate data structures
- Profile memory usage on mobile devices

### Code Analysis Rules

StyleCop rules are enforced (see .editorconfig for specific settings):
- SA1101: Prefix local calls with this (disabled)
- SA1200: Using directives must be placed correctly
- SA1201: Elements must appear in correct order (disabled)
- SA1413: Use trailing comma in multi-line initializers (disabled)
- SA1516: Elements must be separated by blank line (disabled)
- SA1600-SA1602: Documentation rules (disabled)

## Commit Guidelines

- Use descriptive commit messages following conventional format
- Keep commits focused on single changes
- Reference issue numbers when applicable
- Ensure builds pass before committing

## Code Review Checklist

- [ ] Code follows established patterns and style guidelines
- [ ] No sensitive information logged or committed
- [ ] Proper error handling implemented
- [ ] Tests pass (when available)
- [ ] Documentation updated if needed
- [ ] Performance considerations addressed
- [ ] Platform-specific code properly isolated</content>
<parameter name="filePath">/Users/tianzhizhi/Documents/trojan-plus-app/AGENTS.md