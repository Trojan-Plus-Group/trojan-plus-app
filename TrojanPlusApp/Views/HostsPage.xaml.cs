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
using System.ComponentModel;
using System.IO;
using System.Linq;
using TrojanPlusApp.Models;
using TrojanPlusApp.ViewModels;
using Xamarin.Forms;

namespace TrojanPlusApp.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class HostsPage : ContentPage
    {
        private readonly HostsViewModel viewModel;

        public HostsPage()
        {
            InitializeComponent();
            BindingContext = viewModel = new HostsViewModel();
        }

        public void OnItemSelected(object sender, EventArgs args)
        {
            var layout = (BindableObject)sender;
            var item = (HostModel)layout.BindingContext;
            viewModel.SelectedHostItem(item.HostName);
        }

        public async void OnItemEdit(object sender, EventArgs args)
        {
            var layout = (BindableObject)sender;
            var item = (HostModel)layout.BindingContext;
            await Navigation.PushModalAsync(new NavigationPage(new HostEditPage(viewModel, item)));
        }

        public async void AddItem_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new NavigationPage(new HostEditPage(viewModel, null)));
        }

        public async void OnConnectBtnClicked(object sender, EventArgs e)
        {
            var host = viewModel.CurretSelectHost;
            if (host == null)
            {
                await DisplayAlert(
                    Resx.TextResource.Common_AlertTitle,
                    Resx.TextResource.Hosts_PleaseAddHost,
                    Resx.TextResource.Common_OK);

                return;
            }

            if (host.EnablePipeline && host.LoadBalance.Count > 0)
            {
                var hasInvalid = host.LoadBalance.Any(h =>
                {
                    var n = viewModel.FindHostByName(h);
                    return n == null || !n.EnablePipeline;
                });

                if (hasInvalid)
                {
                    await DisplayAlert(
                       Resx.TextResource.Common_AlertTitle,
                       Resx.TextResource.Hosts_LoadBalanceNodeError,
                       Resx.TextResource.Common_OK);

                    return;
                }
            }

            try
            {
                File.WriteAllText(App.Instance.ConfigPath, host.PrepareConfig(viewModel));
                App.Instance.Start();

                viewModel.IsConnectBtnEnabled = false;
                viewModel.SetHostGoingToRun(host.HostName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
            }
        }
    }
}