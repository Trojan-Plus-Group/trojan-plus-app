// SPDX-License-Identifier: GPL-3.0-or-later
// Only platform/UI collaborators are fakes; export handlers and file access are linked production code.
namespace Microsoft.Maui.Controls
{
    public class BindableObject { public object? BindingContext { get; set; } }
    public class ToggledEventArgs : EventArgs { }
    public sealed class Dispatcher { public void Dispatch(Action action) => action(); }
    public sealed class Navigation { public Task PopModalAsync() => Task.CompletedTask; }
    public class ContentPage : BindableObject
    {
        public readonly List<string> Errors = new();
        public int PrivacyPrompts;
        public Func<Task<bool>> Confirm = () => Task.FromResult(true);
        public Dispatcher Dispatcher { get; } = new();
        public Navigation Navigation { get; } = new();
        protected virtual void OnAppearing() { }
        protected virtual void OnDisappearing() { }
        protected void OnPropertyChanged(string name) { }
        protected Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel)
        {
            PrivacyPrompts++;
            return Confirm();
        }
        protected Task DisplayAlertAsync(string title, string message, string accept)
        {
            Errors.Add(message);
            return Task.CompletedTask;
        }
        protected Task<string> DisplayPromptAsync(string title, string message, string accept, string cancel, string initialValue) => Task.FromResult(initialValue);
        protected Task<string> DisplayActionSheetAsync(string title, string cancel, string? destruction, params string[] buttons) => Task.FromResult(cancel);
    }
}
namespace Microsoft.Maui.Storage
{
    public static class FileSystem { public static string CacheDirectory { get; set; } = ""; }
}
namespace Microsoft.Maui.ApplicationModel.DataTransfer
{
    public sealed class Clipboard
    {
        public static Clipboard Default { get; } = new();
        public List<string> Copies { get; } = new();
        public Task SetTextAsync(string text) { Copies.Add(text); return Task.CompletedTask; }
    }
    public sealed class Share
    {
        public static Share Default { get; } = new();
        public List<ShareFileRequest> Requests { get; } = new();
        public Task RequestAsync(ShareFileRequest request) { Requests.Add(request); return Task.CompletedTask; }
    }
    public sealed class ShareFileRequest
    {
        public string Title { get; set; } = "";
        public ShareFile File { get; set; } = null!;
    }
    public sealed class ShareFile(string path, string contentType)
    {
        public string FullPath { get; } = path;
        public string ContentType { get; } = contentType;
    }
}
namespace TrojanPlusApp
{
    public sealed class App
    {
        public static App Instance { get; } = new();
        public bool IsVpnServiceRunning { get; set; }
        public bool IsStartBtnEnabled { get; set; } = true;
    }
    public static class Messenger { public static void Send(string topic, object? item) { } }
}
namespace TrojanPlusApp.Models
{
    public sealed class HostModel
    {
        public string HostName { get; set; } = "";
        public bool EnableDebugLog { get; set; }
        public bool EnablePipeline { get; set; }
        public bool SSLVerify { get; set; }
        public List<string> LoadBalance { get; set; } = new();
        public HostModel Duplicate(string? name) => new() { HostName = name ?? HostName, EnableDebugLog = EnableDebugLog, LoadBalance = new(LoadBalance) };
        public bool IsValid() => true;
    }
}
namespace TrojanPlusApp.ViewModels
{
    public sealed class Settings { public string HostRunningName { get; set; } = ""; }
    public sealed class Store
    {
        public Settings Settings { get; } = new();
        public bool HasHost(string name) => false;
    }
    public sealed class HostsViewModel
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged { add { } remove { } }
        public Store DataStore { get; } = new();
        public List<Models.HostModel> Items { get; } = new();
        public Task UpdateItemAsync(string? name, Models.HostModel item) => Task.CompletedTask;
        public Models.HostModel? FindHostByName(string? name) => null;
    }
}
namespace TrojanPlusApp.Views
{
    public partial class HostEditPage
    {
        private void InitializeComponent() { }
        public void Appear() => OnAppearing();
        public void Disappear() => OnDisappearing();
        public void Copy() => OnCopyDebugLogClicked(this, EventArgs.Empty);
        public void ShareLog() => OnShareDebugLogClicked(this, EventArgs.Empty);
    }
}
namespace TrojanPlusApp.Resx
{
    public static class TextResource
    {
        public const string New_AddHostTitle = nameof(New_AddHostTitle), New_EditHostTitle = nameof(New_EditHostTitle),
            New_DebugLogPrivacyTitle = nameof(New_DebugLogPrivacyTitle), New_DebugLogPrivacyWarning = nameof(New_DebugLogPrivacyWarning),
            New_ShareDebugLog = nameof(New_ShareDebugLog), New_CopyDebugLog = nameof(New_CopyDebugLog),
            Common_Cancel = nameof(Common_Cancel), Common_ErrorTitle = nameof(Common_ErrorTitle), Common_OK = nameof(Common_OK),
            New_DebugLogStopFirst = nameof(New_DebugLogStopFirst), New_DebugLogEmpty = nameof(New_DebugLogEmpty),
            New_DebugLogExportFailed = nameof(New_DebugLogExportFailed), New_HostErrorNeedName = nameof(New_HostErrorNeedName),
            New_HostNameDuplicate = nameof(New_HostNameDuplicate), New_ErrorAttributes = nameof(New_ErrorAttributes),
            Common_AskTitle = nameof(Common_AskTitle), New_HostDeleteAskText = nameof(New_HostDeleteAskText),
            Common_Yes = nameof(Common_Yes), Common_No = nameof(Common_No), New_AskDuplicatePrompt = nameof(New_AskDuplicatePrompt),
            New_HostHasBeenAdded = nameof(New_HostHasBeenAdded), New_LoadBalanceHost_Error = nameof(New_LoadBalanceHost_Error),
            New_LoadBalanceHost_Title = nameof(New_LoadBalanceHost_Title), New_LoadBalanceDelHostAsk = nameof(New_LoadBalanceDelHostAsk);
    }
}
