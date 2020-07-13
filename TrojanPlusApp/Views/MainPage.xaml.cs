using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using TrojanPlusApp.Models;
using Xamarin.Forms;

namespace TrojanPlusApp.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : MasterDetailPage
    {
        private Dictionary<int, NavigationPage> menuPages = new Dictionary<int, NavigationPage>();
        public MainPage()
        {
            InitializeComponent();

            MasterBehavior = MasterBehavior.Popover;

            menuPages.Add((int)MenuItemType.AllHost, (NavigationPage)Detail);
        }

        public async Task NavigateFromMenu(int id)
        {
            if (!menuPages.ContainsKey(id))
            {
                switch (id)
                {
                    case (int)MenuItemType.AllHost:
                        menuPages.Add(id, new NavigationPage(new HostsPage())
                        {
                            BarBackgroundColor = Color.Black
                        });
                        break;
                    case (int)MenuItemType.About:
                        menuPages.Add(id, new NavigationPage(new AboutPage())
                        {
                            BarBackgroundColor = Color.Black
                        });
                        break;
                }
            }

            var newPage = menuPages[id];

            if (newPage != null && Detail != newPage)
            {
                Detail = newPage;

                if (Device.RuntimePlatform == Device.Android)
                {
                    await Task.Delay(100);
                }

                IsPresented = false;
            }
        }
    }
}