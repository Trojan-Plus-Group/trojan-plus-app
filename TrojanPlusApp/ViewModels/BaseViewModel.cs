using TrojanPlusApp.Models;
using TrojanPlusApp.Services;
using Xamarin.Forms;

namespace TrojanPlusApp.ViewModels
{
    public class BaseViewModel : NotificationModel
    {
        private static readonly DataStore DataStoreValue = new DataStore();
        public DataStore DataStore
        {
            get { return DataStoreValue; }
        }

        private string title = string.Empty;
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }
    }
}
