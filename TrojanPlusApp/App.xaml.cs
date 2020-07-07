using TrojanPlusApp.Services;
using TrojanPlusApp.Views;
using Xamarin.Forms;

namespace TrojanPlusApp
{
    public partial class App : Application
    {
        public interface IStart
        {
            void Start();
        }

        public static App Instance
        {
            get { return (App)Current; }
        }

        private readonly IStart starter;

        public string ConfigPath { get; }
        public bool IsStartBtnEnabled { get; private set; }
        public bool GetStartBtnStatus { get; private set; }

        public App(string configPath, IStart starter)
        {
            this.starter = starter;
            ConfigPath = configPath;

            InitializeComponent();

            DependencyService.Register<DataStore>();
            MainPage = new MainPage();
        }

        public void Start()
        {
            starter.Start();
        }

        public void OnSetStartBtnEnabled(bool enable)
        {
            IsStartBtnEnabled = enable;
            MessagingCenter.Send(this, "Starter_OnSetStartBtnEnabled", enable);
        }

        public void OnSetStartBtnStatus(bool running)
        {
            GetStartBtnStatus = running;
            MessagingCenter.Send(this, "Starter_OnSetStartBtnStatus", running);
        }

        /*
        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
        */
    }

}
