using System.Collections.ObjectModel;
using System.Linq;
using TrojanPlusApp.Models;
using TrojanPlusApp.Views;
using Xamarin.Forms;

namespace TrojanPlusApp.ViewModels
{
    public class HostsViewModel : BaseViewModel
    {
        public ObservableCollection<HostModel> Items { get; set; }
        public int CurrSelectHostIdx { get; set; }

        public HostModel CurretSelectHost
        {
            get
            {
                return CurrSelectHostIdx < Items.Count ? Items[CurrSelectHostIdx] : null;
            }
        }

        public string ConnectBtnText
        {
            get
            {
                return App.Instance.GetStartBtnStatus ? "Disconnect" : "Connect";
            }
        }

        public bool IsConnectBtnEnabled
        {
            get { return App.Instance.IsStartBtnEnabled; }
        }

        public HostsViewModel()
        {
            Title = Resx.TextResource.Menu_HostsViewTitle;
            Items = new ObservableCollection<HostModel>();

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

            MessagingCenter.Subscribe<App, bool>(this, "Starter_OnSetStartBtnStatus", (sender, runing) =>
            {
                OnPropertyChanged("ConnectBtnText");
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
    }
}