using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TrojanPlusApp.Models;
using TrojanPlusApp.Views;
using Xamarin.Forms;

namespace TrojanPlusApp.ViewModels
{
    public class HostsViewModel : BaseViewModel
    {
        public ObservableCollection<HostModel> Items { get; set; }
        public int CurrSelectHostIdx { get; set; }

        public HostsViewModel()
        {
            Title = Resx.TextResource.Menu_HostsViewTitle;
            Items = new ObservableCollection<HostModel>();

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

                if (Items.Count == 1)
                {
                    SelectedHostItem(newItem.HostName);
                }

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

                if (deleteItem.UI_Selected)
                {
                    if (Items.Count > 0)
                    {
                        SelectedHostItem(Items[0].HostName);
                    }
                }

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
    }
}