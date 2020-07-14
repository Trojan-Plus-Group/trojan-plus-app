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

using System.Collections.ObjectModel;
using System.Linq;
using TrojanPlusApp.Models;
using TrojanPlusApp.Views;
using Xamarin.Forms;

namespace TrojanPlusApp.ViewModels
{
    public class HostsViewModel : BaseViewModel
    {
        private string goingToRuningHostName;
        public ObservableCollection<HostModel> Items { get; set; }
        public int CurrSelectHostIdx
        {
            get { return DataStore.Settings.HostSelectedIdx; }
            set { DataStore.SetCurrSelectHostIdx(value); }
        }

        public HostModel CurretSelectHost
        {
            get { return CurrSelectHostIdx < Items.Count ? Items[CurrSelectHostIdx] : null; }
        }

        public string ConnectBtnText
        {
            get
            {
                return App.Instance.IsVpnServiceRunning ?
                  Resx.TextResource.Hosts_BtnDisconnect :
                  Resx.TextResource.Hosts_BtnConnect;
            }
        }

        public bool IsConnectBtnEnabled
        {
            get { return App.Instance.IsStartBtnEnabled; }
            set { OnPropertyChanged(); }
        }

        public HostsViewModel()
        {
            Title = Resx.TextResource.Menu_HostsViewTitle;
            Items = new ObservableCollection<HostModel>();
            DataStore.FillHosts(Items);

            if (CurrSelectHostIdx < Items.Count)
            {
                Items[CurrSelectHostIdx].UI_Selected = true;
            }

            goingToRuningHostName = DataStore.Settings.HostRunningName;

            MessagingCenter.Subscribe<HostEditPage, HostModel>(this, "AddItem", async (sender, item) =>
            {
                var newItem = item as HostModel;
                if (newItem != null)
                {
                    if (DataStore.HasHost(newItem.HostName))
                    {
                        return;
                    }
                }

                Items.Add(newItem);

                if (Items.Count == 1)
                {
                    SelectedHostItem(newItem.HostName);
                }

                await DataStore.AddItemAsync(newItem);
            });

            MessagingCenter.Subscribe<HostEditPage, HostModel>(this, "DeleteItem", async (sender, item) =>
            {
                var deleteItem = item as HostModel;
                if (deleteItem != null)
                {
                    if (!DataStore.HasHost(deleteItem.HostName))
                    {
                        return;
                    }
                }

                Items.RemoveAll(i => i.HostName.Equals(deleteItem.HostName));

                if (deleteItem.UI_Selected)
                {
                    if (Items.Count > 0)
                    {
                        SelectedHostItem(Items[0].HostName);
                    }
                }

                await DataStore.DeleteItemAsync(deleteItem.HostName);
            });

            MessagingCenter.Subscribe<HostEditPage, HostModel>(this, "UpdateItem", async (sender, item) =>
            {
                var updatedItem = item as HostModel;
                if (updatedItem != null)
                {
                    if (!DataStore.HasHost(updatedItem.HostName))
                    {
                        return;
                    }
                }

                for (int i = 0; i < Items.Count; i++)
                {
                    if (Items[i].HostName.Equals(updatedItem.HostName))
                    {
                        Items[i] = updatedItem;
                        break;
                    }
                }

                await DataStore.UpdateItemAsync(updatedItem);
            });

            MessagingCenter.Subscribe<App, bool>(this, "Starter_OnSetStartBtnEnabled", (sender, enable) =>
            {
                OnPropertyChanged("IsConnectBtnEnabled");
            });

            MessagingCenter.Subscribe<App, bool>(this, "Starter_OnVpnIsRunning", (sender, running) =>
            {
                OnPropertyChanged("ConnectBtnText");

                if (running)
                {
                    var host = FindHostByName(goingToRuningHostName);
                    if (host != null)
                    {
                        DataStore.SetHostRunningName(host.HostName);
                        host.UI_NotRunning = false;
                        foreach (var load in host.LoadBalance)
                        {
                            var h = FindHostByName(load);
                            if (h != null)
                            {
                                h.UI_NotRunning = false;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var h in Items)
                    {
                        h.UI_NotRunning = true;
                    }

                    DataStore.SetHostRunningName(string.Empty);
                }
            });
        }

        public void SelectedHostItem(string hostName)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].HostName.Equals(hostName))
                {
                    CurrSelectHostIdx = i;
                    Items[i].UI_Selected = true;
                }
                else
                {
                    Items[i].UI_Selected = false;
                }
            }
        }

        public HostModel FindHostByName(string hostName)
        {
            return Items.FindOrNull(i => i.HostName.Equals(hostName));
        }

        public void SetHostGoingToRun(string hostName)
        {
            goingToRuningHostName = hostName;
        }


    }
}