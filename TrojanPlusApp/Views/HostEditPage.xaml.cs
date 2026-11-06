/*
 * This file is part of the Trojan Plus project.
 * Trojan is an unidentifiable mechanism that helps you bypass GFW.
 * Trojan Plus is derived from original trojan project and writing
 * for more experimental features.
 * Copyright (C) 2026 The Trojan Plus Group Authors.
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

namespace TrojanPlusApp.Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Maui.ApplicationModel.DataTransfer;
    using Microsoft.Maui.Controls;
    using Microsoft.Maui.Storage;
    using TrojanPlusApp.Models;
    using TrojanPlusApp.ViewModels;

    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class HostEditPage : ContentPage
    {
        private readonly HostsViewModel hostsModel;
        private readonly bool addedOrEdit;
        private readonly string? originalName;
        private bool exportingDebugLog;
        private bool pageVisible;
        public ObservableCollection<string> LoadBalance { get; set; } = new ObservableCollection<string>();

        public HostModel Item { get; set; }
        public bool ShowDeleteBtn { get; set; }
        public string PageTitle
        {
            get
            {
                return addedOrEdit ? Resx.TextResource.New_AddHostTitle : Resx.TextResource.New_EditHostTitle;
            }
        }

        public HostEditPage(HostsViewModel viewModel, HostModel? editHost)
        {
            InitializeComponent();

            hostsModel = viewModel;
            addedOrEdit = editHost == null;
            originalName = editHost?.HostName;
            Item = editHost?.Duplicate(originalName) ?? new HostModel() { SSLVerify = true };
            ShowDeleteBtn = !addedOrEdit;

            foreach (var h in Item.LoadBalance)
            {
                LoadBalance.Add(h);
            }

            BindingContext = this;
        }

        public bool CanExportDebugLog => Services.DebugLogFiles.CanExport(
            Item.EnableDebugLog,
            !App.Instance.IsVpnServiceRunning && App.Instance.IsStartBtnEnabled
                && string.IsNullOrEmpty(hostsModel.DataStore.Settings.HostRunningName));

        public bool IsDebugLogExportEnabled => !exportingDebugLog;

        protected override void OnAppearing()
        {
            base.OnAppearing();
            pageVisible = true;
            hostsModel.PropertyChanged += OnHostsPropertyChanged;
            RefreshDebugLogActions();
        }

        protected override void OnDisappearing()
        {
            pageVisible = false;
            hostsModel.PropertyChanged -= OnHostsPropertyChanged;
            base.OnDisappearing();
        }

        private void OnHostsPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Dispatcher.Dispatch(RefreshDebugLogActions);
        }

        private void OnDebugLogToggled(object sender, ToggledEventArgs e)
        {
            if (BindingContext == this)
            {
                RefreshDebugLogActions();
            }
        }

        private void RefreshDebugLogActions()
        {
            OnPropertyChanged(nameof(CanExportDebugLog));
            OnPropertyChanged(nameof(IsDebugLogExportEnabled));
        }

        private async void OnCopyDebugLogClicked(object sender, EventArgs e)
        {
            await ExportDebugLogAsync(false);
        }

        private async void OnShareDebugLogClicked(object sender, EventArgs e)
        {
            await ExportDebugLogAsync(true);
        }

        private async Task ExportDebugLogAsync(bool share)
        {
            if (exportingDebugLog || !pageVisible || !CanExportDebugLog)
            {
                return;
            }

            exportingDebugLog = true;
            RefreshDebugLogActions();
            try
            {
                bool confirmed = await DisplayAlertAsync(
                    Resx.TextResource.New_DebugLogPrivacyTitle,
                    Resx.TextResource.New_DebugLogPrivacyWarning,
                    share ? Resx.TextResource.New_ShareDebugLog : Resx.TextResource.New_CopyDebugLog,
                    Resx.TextResource.Common_Cancel);
                if (!confirmed || !pageVisible)
                {
                    return;
                }

                if (!CanExportDebugLog)
                {
                    await DisplayAlertAsync(Resx.TextResource.Common_ErrorTitle,
                        Resx.TextResource.New_DebugLogStopFirst, Resx.TextResource.Common_OK);
                    return;
                }

                // Keep snapshot read synchronous: no UI yield between stopped check and file read.
                byte[] snapshot = Services.DebugLogFiles.ReadSnapshot(
                    Services.DebugLogFiles.PathForHost(originalName ?? Item.HostName));
                if (snapshot.Length == 0)
                {
                    await DisplayAlertAsync(Resx.TextResource.Common_ErrorTitle,
                        Resx.TextResource.New_DebugLogEmpty, Resx.TextResource.Common_OK);
                    return;
                }

                if (share)
                {
                    string directory = Path.Combine(FileSystem.CacheDirectory, "debug-log-exports");
                    Directory.CreateDirectory(directory);
                    // Keep at most one temporary export; next share revokes old snapshot availability.
                    foreach (string oldFile in Directory.GetFiles(directory, "debug-log-*.txt"))
                    {
                        File.Delete(oldFile);
                    }

                    string path = Path.Combine(directory, $"debug-log-{Guid.NewGuid():N}.txt");
                    File.WriteAllBytes(path, snapshot);
                    // Recipient may read after chooser closes; leave temporary file in OS-managed cache.
                    await Share.Default.RequestAsync(new ShareFileRequest
                    {
                        Title = Resx.TextResource.New_ShareDebugLog,
                        File = new ShareFile(path, "text/plain"),
                    });
                }
                else
                {
                    await Clipboard.Default.SetTextAsync(Encoding.UTF8.GetString(snapshot));
                }
            }
            catch (FileNotFoundException)
            {
                await DisplayAlertAsync(Resx.TextResource.Common_ErrorTitle,
                    Resx.TextResource.New_DebugLogEmpty, Resx.TextResource.Common_OK);
            }
            catch (DirectoryNotFoundException)
            {
                await DisplayAlertAsync(Resx.TextResource.Common_ErrorTitle,
                    Resx.TextResource.New_DebugLogEmpty, Resx.TextResource.Common_OK);
            }
            catch (Exception)
            {
                await DisplayAlertAsync(Resx.TextResource.Common_ErrorTitle,
                    Resx.TextResource.New_DebugLogExportFailed, Resx.TextResource.Common_OK);
            }
            finally
            {
                exportingDebugLog = false;
                RefreshDebugLogActions();
            }
        }

        public async void Save_Clicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Item.HostName))
            {
                await DisplayAlertAsync(
                    Resx.TextResource.Common_ErrorTitle,
                    Resx.TextResource.New_HostErrorNeedName,
                    Resx.TextResource.Common_OK);
                return;
            }

            if (Item.HostName != originalName && hostsModel.DataStore.HasHost(Item.HostName))
            {
                await DisplayAlertAsync(
                    Resx.TextResource.Common_ErrorTitle,
                    Resx.TextResource.New_HostNameDuplicate,
                    Resx.TextResource.Common_OK);
                return;
            }

            if (!Item.IsValid())
            {
                await DisplayAlertAsync(
                        Resx.TextResource.Common_ErrorTitle,
                        string.Format(Resx.TextResource.New_ErrorAttributes, Item.HostName),
                        Resx.TextResource.Common_OK);
                return;
            }

            Item.LoadBalance.Clear();
            Item.LoadBalance.AddRange(LoadBalance);
            LoadBalance.Clear();
            if (addedOrEdit)
            {
                Messenger.Send("AddItem", Item);
            }
            else
            {
                await hostsModel.UpdateItemAsync(originalName, Item);
            }

            await Navigation.PopModalAsync();
        }

        public async void Cancel_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        public async void OnDeleteBtnClicked(object sender, EventArgs e)
        {
            bool response = await DisplayAlertAsync(
                Resx.TextResource.Common_AskTitle,
                Resx.TextResource.New_HostDeleteAskText,
                Resx.TextResource.Common_Yes,
                Resx.TextResource.Common_No);

            if (response)
            {
                Messenger.Send("DeleteItem", hostsModel.FindHostByName(originalName));
                await Navigation.PopModalAsync();
            }
        }

        public async void OnDuplicateBtnClicked(object sender, EventArgs e)
        {
            string name = await DisplayPromptAsync(
                Resx.TextResource.Common_AskTitle,
                Resx.TextResource.New_AskDuplicatePrompt,
                Resx.TextResource.Common_OK,
                Resx.TextResource.Common_Cancel,
                initialValue: Item.HostName + " 1");

            if (name == null)
            {
                return;
            }

            if (hostsModel.DataStore.HasHost(name))
            {
                await DisplayAlertAsync(
                    Resx.TextResource.Common_ErrorTitle,
                    string.Format(Resx.TextResource.New_HostHasBeenAdded, name),
                    Resx.TextResource.Common_OK);

                return;
            }

            var newItem = Item.Duplicate(name);
            newItem.LoadBalance.Clear();
            newItem.LoadBalance.AddRange(LoadBalance);
            LoadBalance.Clear();
            Messenger.Send("AddItem", newItem);
            await Navigation.PopModalAsync();
        }

        public async void OnAddLoadbalanceClicked(object sender, EventArgs e)
        {
            List<string> hosts;
            if (addedOrEdit)
            {
                hosts = hostsModel.Items
                    .Where(h => h.EnablePipeline
                           && !LoadBalance.Contains(h.HostName))
                    .Select(h => h.HostName)
                    .ToList();
            }
            else
            {
                hosts = hostsModel.Items
                    .Where(h => h.EnablePipeline
                            && !LoadBalance.Contains(h.HostName)
                            && !h.HostName.Equals(originalName))
                    .Select(h => h.HostName)
                    .ToList();
            }

            if (hosts.Count == 0)
            {
                await DisplayAlertAsync(
                    Resx.TextResource.Common_ErrorTitle,
                    Resx.TextResource.New_LoadBalanceHost_Error,
                    Resx.TextResource.Common_Yes);

                return;
            }

            string action = await DisplayActionSheetAsync(
                Resx.TextResource.New_LoadBalanceHost_Title,
                Resx.TextResource.Common_Cancel,
                null,
                hosts.ToArray());

            if (hosts.Contains(action))
            {
                LoadBalance.Add(action);
            }
        }

        public async void OnDeleteLoadbalanceClicked(object sender, EventArgs e)
        {
            var layout = (BindableObject)sender;
            var hostName = layout.BindingContext as string;

            bool response = await DisplayAlertAsync(
                Resx.TextResource.Common_AskTitle,
                string.Format(Resx.TextResource.New_LoadBalanceDelHostAsk, hostName),
                Resx.TextResource.Common_Yes,
                Resx.TextResource.Common_No);

            if (response)
            {
                LoadBalance.Remove(hostName!);
            }
        }
    }
}