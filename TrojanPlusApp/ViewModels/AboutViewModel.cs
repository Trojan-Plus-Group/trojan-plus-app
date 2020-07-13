using System;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace TrojanPlusApp.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
        public string AppVersion
        {
            get { return App.Instance.Starter.GetAppVersion(); }
        }

        public string TrojanPlusVersion
        {
            get { return App.Instance.Starter.GetTrojanPlusLibVersion(); }
        }

        public ICommand OpenWebCommand { get; } = new Command(async () =>
        {
            await Browser.OpenAsync("https://github.com/Trojan-Plus-Group/trojan-plus-app");
        });

        public ICommand ClickCommand => new Command<string>(async (url) =>
        {
            await Browser.OpenAsync(url);
        });

        public AboutViewModel()
        {
            Title = Resx.TextResource.Menu_AboutTitle;
        }
    }
}