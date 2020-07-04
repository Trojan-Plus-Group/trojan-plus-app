using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using TrojanPlusApp.Models;
using TrojanPlusApp.Views;
using Xamarin.Forms;

namespace TrojanPlusApp.ViewModels
{
    public class HostsViewModel : BaseViewModel
    {
        public ObservableCollection<HostModel> Items { get; set; }
        public Command LoadItemsCommand { get; set; }

        public HostsViewModel()
        {
            Title = Resx.TextResource.Menu_HostsViewTitle;
            Items = new ObservableCollection<HostModel>();
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());

            MessagingCenter.Subscribe<HostEditPage, HostModel>(this, "AddItem", async (obj, item) =>
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
                await DataStore.AddItemAsync(newItem);
            });

            MessagingCenter.Subscribe<HostEditPage, HostModel>(this, "DeleteItem", async (obj, item) =>
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
                await DataStore.DeleteItemAsync(deleteItem.HostName);
            });

            MessagingCenter.Subscribe<HostEditPage, HostModel>(this, "UpdateItem", async (obj, item) =>
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
        }

        public async Task ExecuteLoadItemsCommand()
        {
            IsBusy = true;

            try
            {
                Items.Clear();
                var items = await DataStore.GetItemsAsync(true);
                foreach (var item in items)
                {
                    Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}