using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TrojanPlusApp.Services
{
    public interface IDataStore<T>
    {
        Task<bool> AddItemAsync(T item);
        Task<bool> UpdateItemAsync(T item);
        Task<bool> DeleteItemAsync(string id);

        void FillHosts(IList<T> list);

        int GetCurrSelectHostIdx();
        void SetCurrSelectHostIdx(int idx);

        bool HasHost(string hostName);
    }
}
