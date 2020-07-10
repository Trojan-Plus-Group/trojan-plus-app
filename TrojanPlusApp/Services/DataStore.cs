using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TrojanPlusApp.Models;

namespace TrojanPlusApp.Services
{
    public class DataStore : IDataStore<HostModel>
    {
        private const string HostsKey = "HostsKey";
        private const string HostSelectedIdxKey = "HostSelectedIdxKey";

        private readonly List<HostModel> items;
        private int currSelectHostIdx;

        public DataStore()
        {
            if (App.Instance.Properties.ContainsKey(HostsKey))
            {
                items = JsonConvert.DeserializeObject<List<HostModel>>(App.Instance.Properties[HostsKey] as string);
            }

            if (App.Instance.Properties.ContainsKey(HostSelectedIdxKey))
            {
                currSelectHostIdx = (int)App.Instance.Properties[HostSelectedIdxKey];
            }

            if (items == null)
            {
                items = new List<HostModel>();
            }
        }

        public void FillHosts(IList<HostModel> list)
        {
            foreach (var h in items)
            {
                list.Add(h);
            }
        }

        public int GetCurrSelectHostIdx()
        {
            return currSelectHostIdx;
        }

        public async void SetCurrSelectHostIdx(int idx)
        {
            currSelectHostIdx = idx;
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
            await Store();

            return await Task.FromResult(true);
        }

        private Task Store()
        {
            App.Instance.Properties[HostsKey] = JsonConvert.SerializeObject(items);
            App.Instance.Properties[HostSelectedIdxKey] = currSelectHostIdx;
            return App.Instance.SavePropertiesAsync();
        }
    }
}