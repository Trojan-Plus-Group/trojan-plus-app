using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrojanPlusApp.Models;

namespace TrojanPlusApp.Services
{
    public class DataStore : IDataStore<HostModel>
    {
        private readonly List<HostModel> items;

        public DataStore()
        {
            items = new List<HostModel>();
        }

        public bool HasHost(string hostName)
        {
            return items.FindIndex(i => i.HostName.Equals(hostName)) != -1;
        }

        public async Task<bool> AddItemAsync(HostModel item)
        {
            items.Add(item);

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

            return await Task.FromResult(true);
        }

        public async Task<bool> DeleteItemAsync(string id)
        {
            var oldItem = items.Where((HostModel arg) => arg.HostName == id).FirstOrDefault();
            items.Remove(oldItem);

            return await Task.FromResult(true);
        }

        public async Task<HostModel> GetItemAsync(string id)
        {
            return await Task.FromResult(items.FirstOrDefault(s => s.HostName == id));
        }

        public async Task<IEnumerable<HostModel>> GetItemsAsync(bool forceRefresh = false)
        {
            return await Task.FromResult(items);
        }
    }
}