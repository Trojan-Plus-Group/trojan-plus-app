/*
 * This file is part of the Trojan Plus project.
 * Trojan is an unidentifiable mechanism that helps you bypass GFW.
 * Trojan Plus is derived from original trojan project and writing
 * for more experimental features.
 * Copyright (C) 2020 The Trojan Plus Group Authors.
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

using System;
using System.Linq;
using System.ComponentModel;
using Xamarin.Forms;

using TrojanPlusApp.Models;
using TrojanPlusApp.ViewModels;
using System.Runtime.InteropServices.ComTypes;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TrojanPlusApp.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class HostEditPage : ContentPage
    {
        private readonly HostsViewModel hostsModel;
        private readonly bool addedOrEdit;
        public ObservableCollection<string> LoadBalance { get; set; } = new ObservableCollection<string>();

        public HostModel Item { get; set; }
        public bool ShowDeleteBtn { get; set; }
        public bool EnableEditHostName { get; set; }
        public string PageTitle
        {
            get
            {
                return addedOrEdit ? Resx.TextResource.New_AddHostTitle : Resx.TextResource.New_EditHostTitle;
            }
        }

        public HostEditPage(HostsViewModel viewModel, HostModel editHost)
        {
            InitializeComponent();

            hostsModel = viewModel;
            addedOrEdit = editHost == null;
            Item = editHost ?? new HostModel() { SSLVerify = true };
            ShowDeleteBtn = !addedOrEdit;
            EnableEditHostName = addedOrEdit;

            foreach (var h in Item.LoadBalance)
            {
                LoadBalance.Add(h);
            }

            BindingContext = this;
        }

        public async void Save_Clicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Item.HostName))
            {
                await DisplayAlert(
                    Resx.TextResource.Common_ErrorTitle,
                    Resx.TextResource.New_HostErrorNeedName,
                    Resx.TextResource.Common_OK);
                return;
            }

            if (addedOrEdit)
            {
                if (hostsModel.DataStore.HasHost(Item.HostName))
                {
                    await DisplayAlert(
                        Resx.TextResource.Common_ErrorTitle,
                        string.Format(Resx.TextResource.New_HostHasBeenAdded, Item.HostName),
                        Resx.TextResource.Common_OK);

                    return;
                }
            }
            else
            {
                Item.LoadBalance.Clear();
                Item.LoadBalance.AddRange(LoadBalance);
                MessagingCenter.Send(this, "UpdateItem", Item);
                await Navigation.PopModalAsync();
                return;
            }

            if (!Item.IsValid())
            {
                await DisplayAlert(
                        Resx.TextResource.Common_ErrorTitle,
                        string.Format(Resx.TextResource.New_ErrorAttributes, Item.HostName),
                        Resx.TextResource.Common_OK);
                return;
            }

            Item.LoadBalance.Clear();
            Item.LoadBalance.AddRange(LoadBalance);
            LoadBalance.Clear();
            MessagingCenter.Send(this, "AddItem", Item);
            await Navigation.PopModalAsync();
        }

        public async void Cancel_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        public async void OnDeleteBtnClicked(object sender, EventArgs e)
        {
            bool response = await DisplayAlert(
                Resx.TextResource.Common_AskTitle,
                Resx.TextResource.New_HostDeleteAskText,
                Resx.TextResource.Common_Yes,
                Resx.TextResource.Common_No);

            if (response)
            {
                MessagingCenter.Send(this, "DeleteItem", Item);
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
                await DisplayAlert(
                    Resx.TextResource.Common_ErrorTitle,
                    string.Format(Resx.TextResource.New_HostHasBeenAdded, name),
                    Resx.TextResource.Common_OK);

                return;
            }

            var newItem = Item.Duplicate(name);
            newItem.LoadBalance.Clear();
            newItem.LoadBalance.AddRange(LoadBalance);
            LoadBalance.Clear();
            MessagingCenter.Send(this, "AddItem", newItem);
            await Navigation.PopModalAsync();
        }

        public async void OnAddLoadbalanceClicked(object sender, EventArgs e)
        {
            List<string> hosts = null;
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
                            && !h.HostName.Equals(Item.HostName))
                    .Select(h => h.HostName)
                    .ToList();
            }

            if (hosts.Count == 0)
            {
                await DisplayAlert(
                    Resx.TextResource.Common_ErrorTitle,
                    Resx.TextResource.New_LoadBalanceHost_Error,
                    Resx.TextResource.Common_Yes);

                return;
            }

            string action = await DisplayActionSheet(
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

            bool response = await DisplayAlert(
                Resx.TextResource.Common_AskTitle,
                string.Format(Resx.TextResource.New_LoadBalanceDelHostAsk, hostName),
                Resx.TextResource.Common_Yes,
                Resx.TextResource.Common_No);

            if (response)
            {
                LoadBalance.Remove(hostName);
            }
        }
    }
}