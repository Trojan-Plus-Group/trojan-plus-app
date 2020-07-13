using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TrojanPlusApp.Models;

namespace TrojanPlusApp.Services
{
    public class DataStore
    {
        private const string HostsKey = "HostsKey";
        private const string SettingsKey = "SettingsKey";

        private readonly List<HostModel> items;
        public SettingsModel Settings { get; private set; }

        public DataStore()
        {
            if (App.Instance.Properties.ContainsKey(HostsKey))
            {
                items = JsonConvert.DeserializeObject<List<HostModel>>(App.Instance.Properties[HostsKey] as string);
            }
            else
            {
                items = new List<HostModel>();
            }

            if (App.Instance.Properties.ContainsKey(SettingsKey))
            {
                Settings = JsonConvert.DeserializeObject<SettingsModel>(App.Instance.Properties[SettingsKey] as string);
            }
            else
            {
                Settings = new SettingsModel();
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
            await Store();
        }

        public async void SetHostRunningName(string hostName)
        {
            Settings.HostRunningName = hostName;
            await Store();
        }

        public bool HasHost(string hostName)
        {
            return items.FindIndex(i => i.HostName.Equals(hostName)) != -1;
        }

        public async Task<bool> AddItemAsync(HostModel item)
        {
            items.Add(item);
            await Store();

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

            await Store();

            return await Task.FromResult(true);
        }

        public async Task<bool> DeleteItemAsync(string id)
        {
            var oldItem = items.Where((HostModel arg) => arg.HostName == id).FirstOrDefault();
            items.Remove(oldItem);

            foreach (var h in items)
            {
                h.LoadBalance.Remove(oldItem.HostName);
            }

            await Store();

            return await Task.FromResult(true);
        }

        private Task Store()
        {
            App.Instance.Properties[HostsKey] = JsonConvert.SerializeObject(items);
            App.Instance.Properties[SettingsKey] = JsonConvert.SerializeObject(Settings);

            return App.Instance.SavePropertiesAsync();
        }
    }
}