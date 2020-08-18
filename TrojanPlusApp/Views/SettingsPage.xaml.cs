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

namespace TrojanPlusApp.Views
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using TrojanPlusApp.ViewModels;
    using Xamarin.Essentials;
    using Xamarin.Forms;

    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class SettingsPage : ContentPage
    {
        private SettingsViewModel viewModel;
        public SettingsPage()
        {
            InitializeComponent();

            BindingContext = viewModel = new SettingsViewModel();
        }

        public async void OnAddWifiClicked(object sender, EventArgs e)
        {
            string ssid;
            var wifiSSIDs = App.Instance.Starter.GetWifiSSIDs();
            if (wifiSSIDs != null && wifiSSIDs.Count > 0)
            {
                wifiSSIDs = wifiSSIDs.Where(ss => !viewModel.Settings.AutoStopWifi.Contains(ss)).ToList();

                if (wifiSSIDs.Count == 0)
                {
                    return;
                }

                ssid = await DisplayActionSheet(
                    Resx.TextResource.Settings_AutoStopWifiAdd,
                    Resx.TextResource.Common_Cancel,
                    null,
                    wifiSSIDs.ToArray());

                if (!string.IsNullOrEmpty(ssid) && wifiSSIDs.Contains(ssid))
                {
                    viewModel.Settings.AutoStopWifi.Add(ssid);
                }

                return;
            }

            ssid = await DisplayPromptAsync(
                Resx.TextResource.Common_AskTitle,
                Resx.TextResource.Settings_AutoStopWifiAdd,
                Resx.TextResource.Common_OK,
                Resx.TextResource.Common_Cancel);

            if (!string.IsNullOrEmpty(ssid) && !viewModel.Settings.AutoStopWifi.Contains(ssid))
            {
                viewModel.Settings.AutoStopWifi.Add(ssid);
            }
        }

        public async void OnDeleteWifiClicked(object sender, EventArgs e)
        {
            bool response = await DisplayAlert(
               Resx.TextResource.Common_AskTitle,
               Resx.TextResource.Settings_AutoStopWifiDelete,
               Resx.TextResource.Common_Yes,
               Resx.TextResource.Common_No);

            if (response)
            {
                var layout = (BindableObject)sender;
                var ssid = layout.BindingContext as string;
                viewModel.Settings.AutoStopWifi.Remove(ssid);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            viewModel.DataStore.StoreToFile();
        }
    }
}