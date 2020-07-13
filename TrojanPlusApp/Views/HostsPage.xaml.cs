using System;
using System.ComponentModel;
using System.IO;
using TrojanPlusApp.Models;
using TrojanPlusApp.ViewModels;
using Xamarin.Forms;

namespace TrojanPlusApp.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class HostsPage : ContentPage
    {
        private readonly HostsViewModel viewModel;

        public HostsPage()
        {
            InitializeComponent();
            BindingContext = viewModel = new HostsViewModel();
        }

        public void OnItemSelected(object sender, EventArgs args)
        {
            var layout = (BindableObject)sender;
            var item = (HostModel)layout.BindingContext;
            viewModel.SelectedHostItem(item.HostName);
        }

        public async void OnItemEdit(object sender, EventArgs args)
        {
            var layout = (BindableObject)sender;
            var item = (HostModel)layout.BindingContext;
            await Navigation.PushModalAsync(new NavigationPage(new HostEditPage(viewModel, item)));
        }

        public async void AddItem_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new NavigationPage(new HostEditPage(viewModel, null)));
        }

        public async void OnConnectBtnClicked(object sender, EventArgs e)
        {
            var host = viewModel.CurretSelectHost;
            if (host == null)
            {
                await DisplayAlert(
                    Resx.TextResource.Common_AlertTitle,
                    Resx.TextResource.Hosts_PleaseAddHost,
                    Resx.TextResource.Common_OK);

                return;
            }

            try
            {
                File.WriteAllText(App.Instance.ConfigPath, viewModel.CurretSelectHost.PrepareConfig(viewModel));
                App.Instance.Start();

                viewModel.IsConnectBtnEnabled = false;
                viewModel.SetHostGoingToRun(host.HostName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
            }
        }
    }
}