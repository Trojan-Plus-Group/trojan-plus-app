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

using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using TrojanPlusApp.Models;
using Xamarin.Forms;

namespace TrojanPlusApp.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : MasterDetailPage
    {
        private Dictionary<int, NavigationPage> menuPages = new Dictionary<int, NavigationPage>();
        public MainPage()
        {
            InitializeComponent();

            MasterBehavior = MasterBehavior.Popover;

            menuPages.Add((int)MenuItemType.AllHost, (NavigationPage)Detail);
        }

        public async Task NavigateFromMenu(int id)
        {
            if (!menuPages.ContainsKey(id))
            {
                switch (id)
                {
                    case (int)MenuItemType.AllHost:
                        menuPages.Add(id, new NavigationPage(new HostsPage())
                        {
                            BarBackgroundColor = Color.Black
                        });
                        break;
                    case (int)MenuItemType.About:
                        menuPages.Add(id, new NavigationPage(new AboutPage())
                        {
                            BarBackgroundColor = Color.Black
                        });
                        break;
                }
            }

            var newPage = menuPages[id];

            if (newPage != null && Detail != newPage)
            {
                Detail = newPage;

                if (Device.RuntimePlatform == Device.Android)
                {
                    await Task.Delay(100);
                }

                IsPresented = false;
            }
        }
    }
}