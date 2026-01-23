# Trojan Plus App - Migration Plan: Xamarin.Forms to .NET MAUI

## Current Assessment

From the codebase analysis:
- **Framework**: Xamarin.Forms 5.0.0.2478 with .NET Standard 2.0
- **Platforms**: Android, iOS, UWP
- **Build System**: MSBuild with Azure Pipelines CI
- **Dependencies**: Xamarin.Forms, Xamarin.Essentials, Newtonsoft.Json, StyleCop
- **Architecture**: MVVM pattern with shared UI, platform-specific projects
- **Code Style**: Well-defined conventions in .editorconfig
- **Testing**: Manual testing only (no automated tests)

## Migration Plan to .NET MAUI

### Phase 1: Preparation and Prerequisites (1-2 weeks)
1. **Environment Setup**
   - Install .NET 6+ SDK (MAUI requires .NET 6 or later)
   - Install Visual Studio 2022 (17.0+) with MAUI workload
   - Update development machines to Windows 11 (recommended for UWP)
   - Install MAUI workload via Visual Studio Installer

2. **Backup and Branching**
   - Create a backup of the entire repository
   - Create a dedicated `maui-migration` branch from main/master
   - Document current working state and known issues

3. **Dependency Analysis**
   - Inventory all NuGet packages and their compatibility with MAUI
   - Identify custom components that may need replacement
   - Review platform-specific code for MAUI compatibility

### Phase 2: Core Migration (2-4 weeks)
1. **Project Structure Migration**
   - Convert .NET Standard library to .NET 6+ class library
   - Update platform-specific projects to MAUI single project structure (optional but recommended)
   - Migrate from multi-project solution to MAUI unified project if desired

2. **Framework Upgrade**
   - Update to .NET MAUI (latest stable version)
   - Replace Xamarin.Forms references with Microsoft.Maui.Controls
   - Update Xamarin.Essentials to Microsoft.Maui.Essentials (now built-in)
   - Migrate custom renderers to handlers (MAUI's new architecture)

3. **Code Migration**
   - Update using statements and namespaces
   - Replace deprecated APIs (e.g., `Device.*` methods)
   - Update XAML files for MAUI compatibility
   - Migrate platform-specific code to MAUI's unified approach
   - Update dependency injection patterns

4. **Build System Updates**
   - Update .csproj files for MAUI project format
   - Modify Azure Pipelines configuration for MAUI builds
   - Update NuGet package references and versions

### Phase 3: Testing and Validation (1-2 weeks)
1. **Unit Testing Setup**
   - Implement automated testing framework (xUnit recommended for MAUI)
   - Create unit tests for ViewModels and Models
   - Add UI tests using MAUI's testing capabilities

2. **Platform Testing**
   - Test on Android (API 21+)
   - Test on iOS (iOS 11+)
   - Test on Windows (Windows 11 preferred, Windows 10 minimum)
   - Validate VPN functionality and Trojan Plus integration

3. **Performance and Compatibility**
   - Profile memory usage and battery consumption
   - Test network connectivity and proxy functionality
   - Validate SSL certificate handling

### Phase 4: Optimization and Deployment (1 week)
1. **Performance Optimization**
   - Leverage MAUI's improved performance features
   - Optimize for .NET 6+ runtime improvements
   - Update any performance-critical code

2. **Documentation Update**
   - Update AGENTS.md with MAUI-specific guidelines
   - Update README and build documentation
   - Document any breaking changes for users

3. **Release Preparation**
   - Update app store metadata and screenshots
   - Prepare migration notes for existing users
   - Create rollback plan

### Key Technical Changes Required
- **API Updates**: Many Xamarin.Forms APIs have been renamed or moved in MAUI
- **Platform Code**: UWP support is evolving; may need Windows App SDK migration
- **Build Process**: Simplified with .NET CLI and unified project structure
- **Dependencies**: Most Xamarin packages have MAUI equivalents
- **XAML**: Minor syntax changes for MAUI compatibility

### Risk Assessment
- **High Risk**: VPN/proxy functionality must work flawlessly
- **Medium Risk**: Platform-specific custom code may need significant refactoring
- **Low Risk**: UI migration should be straightforward with MVVM preservation

### Estimated Timeline: 6-9 weeks
- Depends on team size, complexity of custom components, and testing thoroughness
- Parallel work possible on different phases

## Clarifying Questions (To be addressed before implementation)

1. **Scope**: Do you want to migrate to MAUI's single project structure, or maintain separate platform projects?

2. **Timeline**: What's your target completion date? Any hard deadlines?

3. **Testing**: Since there are no current automated tests, should we prioritize setting up a test framework as part of this migration?

4. **UWP Support**: The original project supports UWP. MAUI's Windows support is evolving - are you willing to migrate to Windows App SDK if needed, or can we drop UWP support?

5. **Resources**: How many developers will be working on this migration? Do you have MAUI experience on the team?

6. **Fallback**: If issues arise, do you want a detailed rollback plan, or are you committed to MAUI regardless?

7. **Features**: Are there any new MAUI features you specifically want to leverage (e.g., improved Blazor integration, better performance)?

## Next Steps
- Review and answer the clarifying questions above
- Create detailed task breakdown based on answers
- Begin Phase 1 preparation work
- Set up migration branch and backup procedures</content>
<parameter name="filePath">/Users/tianzhizhi/Documents/trojan-plus-app/MAUI_MIGRATION_PLAN.md