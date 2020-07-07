using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Android.Content;
using System.IO;
using Java.Lang;
using Android.Util;

namespace TrojanPlusApp.Droid
{
    [Activity(Name = "com.trojan_plus.android.MainActivity", Label = "@string/app_name", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        public class Communicator : TrojanPlusStarter.IActivityCommunicator, App.IStart
        {
            private MainActivity activity;
            public Communicator(MainActivity act)
            {
                activity = act;
            }

            public void SetStartBtnEnabled(bool enable)
            {
                activity.app.OnSetStartBtnEnabled(enable);
            }

            public void SetStartBtnStatus(bool running)
            {
                activity.app.OnSetStartBtnStatus(running);
            }

            public string GetConfigPath()
            {
                return activity.configPath;
            }

            public void Start()
            {
                activity.starter.Start();
            }

            public string GetAppVersion()
            {
                var context = Application.Context;

                PackageManager manager = context.PackageManager;
                PackageInfo info = manager.GetPackageInfo(context.PackageName, 0);

                return info.VersionName;
            }

            public int GetAppBuild()
            {
                var context = Application.Context;
                PackageManager manager = context.PackageManager;
                PackageInfo info = manager.GetPackageInfo(context.PackageName, 0);

                return info.VersionCode;
            }
        }

        App app;
        string configPath;
        private TrojanPlusStarter starter;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            configPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                "config.json");

            var com = new Communicator(this);
            starter = new TrojanPlusStarter(this, com);
            app = new App(configPath, com);

            LoadApplication(app);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions,
            [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (starter.OnActivityResult(requestCode, resultCode, data))
            {
                return;
            }

            base.OnActivityResult(requestCode, resultCode, data);
        }

        protected override void OnResume()
        {
            starter.OnResume();
            base.OnResume();
        }

        protected override void OnDestroy()
        {
            starter.OnDestroy();
            base.OnDestroy();
        }
    }
}