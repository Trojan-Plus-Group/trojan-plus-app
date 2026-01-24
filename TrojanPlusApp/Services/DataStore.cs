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

namespace TrojanPlusApp.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using TrojanPlusApp.Models;
    using Microsoft.Maui.Storage;

    public class DataStore
    {
        public const string HostsKey = "HostsKey";
        public const string SettingsKey = "SettingsKey";

        private readonly List<HostModel> items;
        public SettingsModel Settings { get; private set; }

        public DataStore()
        {
            var hostsJson = Preferences.Get(HostsKey, string.Empty);
            if (!string.IsNullOrEmpty(hostsJson))
            {
                items = JsonConvert.DeserializeObject<List<HostModel>>(hostsJson);
            }
            else
            {
                items = new List<HostModel>();
            }

            var settingsJson = Preferences.Get(SettingsKey, string.Empty);
            if (!string.IsNullOrEmpty(settingsJson))
            {
                Settings = JsonConvert.DeserializeObject<SettingsModel>(settingsJson);
            }
            else
            {
                Settings = new SettingsModel();
            }

            // Only call SettingsChanged if Starter is initialized
            if (App.Instance.Starter != null)
            {
                App.Instance.Starter.SettingsChanged(Settings);
            }
        }

        public void FillHosts(IList<HostModel> list)
        {
            foreach (var h in items)
            {
                list.Add(h);
            }
        }

        public async void SetCurrSelectHostIdx(int idx)
        {
            Settings.HostSelectedIdx = idx;
            await StoreToFile();
        }

        public async void SetHostRunningName(string hostName)
        {
            Settings.HostRunningName = hostName;
            await StoreToFile();
        }

        public bool HasHost(string hostName)
        {
            return items.FindIndex(i => i.HostName.Equals(hostName)) != -1;
        }

        public async Task<bool> AddItemAsync(HostModel item)
        {
            items.Add(item);
            await StoreToFile();

            return await Task.FromResult(true);
        }

        public async Task<bool> UpdateItemAsync(HostModel item)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].HostName.Equals(item.HostName))
                {
                    items[i] = item;
                    break;
                }
            }

            await StoreToFile();

            return await Task.FromResult(true);
        }

        public async Task<bool> DeleteItemAsync(string id)
        {
            var oldItem = items.Where(h => h.HostName == id).FirstOrDefault();
            items.Remove(oldItem);

            foreach (var h in items)
            {
                h.LoadBalance.Remove(oldItem.HostName);
            }

            await StoreToFile();

            return await Task.FromResult(true);
        }

        public Task StoreToFile()
        {
            Preferences.Set(HostsKey, JsonConvert.SerializeObject(items));
            Preferences.Set(SettingsKey, JsonConvert.SerializeObject(Settings));

            // Only call SettingsChanged if Starter is initialized
            if (App.Instance.Starter != null)
            {
                App.Instance.Starter.SettingsChanged(Settings);
            }

            return Task.CompletedTask;
        }
    }
}