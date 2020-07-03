using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using TrojanPlusApp.Services;
using TrojanPlusApp.Views;

namespace TrojanPlusApp
{
    public partial class App : Application
    {

        public App()
        {
            InitializeComponent();

            DependencyService.Register<DataStore>();
            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
