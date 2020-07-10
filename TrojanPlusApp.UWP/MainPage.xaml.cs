using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace TrojanPlusApp.UWP
{
    public sealed partial class MainPage
    {
        public class Stater : TrojanPlusApp.App.IStart
        {
            public int GetAppBuild()
            {
                throw new NotImplementedException();
            }

            public string GetAppVersion()
            {
                throw new NotImplementedException();
            }

            public string GetTrojanPlusLibVersion()
            {
                throw new NotImplementedException();
            }

            public void Start()
            {
                throw new NotImplementedException();
            }
        }

        public MainPage()
        {
            this.InitializeComponent();

            LoadApplication(new TrojanPlusApp.App("", new Stater()));
        }
    }
}
