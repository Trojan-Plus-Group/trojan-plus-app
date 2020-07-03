using System;
using System.ComponentModel;
using Xamarin.Forms;
using TrojanPlusApp.ViewModels;
using TrojanPlusApp.Models;

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

        public async void OnItemSelected(object sender, EventArgs args)
        {
            //var layout = (BindableObject)sender;
            //var item = (HostModel)layout.BindingContext;
            //await Navigation.PushModalAsync(new NavigationPage(new HostEditPage(viewModel, item)));
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

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (viewModel.Items.Count == 0)
                viewModel.IsBusy = true;
        }
    }
}