using System.IO;
using Android.App;
using Android.App.Job;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Java.Lang;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

namespace TrojanPlusApp.Droid
{
    [Activity(
        Name = "com.trojan_plus.android.MainActivity",
        Label = "@string/app_name",
        Icon = "@mipmap/icon",
        Theme = "@style/MainTheme",
        MainLauncher = true,
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

            public void OnVpnIsRunning(bool running)
            {
                activity.app.OnVpnIsRunning(running);
            }

            public string GetConfigPath()
            {
                return PrepareConfigPath;
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

            public string GetTrojanPlusLibVersion()
            {
                return TrojanPlusVPNService.GetTrojanPlusLibVersion();
            }

            public bool StartMonitorNetwork(string[] autoStartWifiSSID, bool autoStartCellur)
            {
                bool succ = true;
                var jobServ = (JobScheduler)activity.GetSystemService(JobSchedulerService);

                if (autoStartWifiSSID != null)
                {
                    jobServ.Cancel(TrojanPlusWifiJobService.JobId);

                    JobInfo.Builder jobBuilder = new JobInfo.Builder(
                        TrojanPlusWifiJobService.JobId,
                        new ComponentName(activity, Class.FromType(typeof(TrojanPlusWifiJobService)).Name));

                    jobBuilder.SetRequiredNetworkType(NetworkType.Unmetered);

                    PersistableBundle bundle = new PersistableBundle();
                    bundle.PutStringArray(TrojanPlusWifiJobService.AutoStartWifiSSIDKey, autoStartWifiSSID);
                    jobBuilder.SetExtras(bundle);

                    succ = jobServ.Schedule(jobBuilder.Build()) > 0;
                }

                if (autoStartCellur)
                {
                    jobServ.Cancel(TrojanPlusCellurJobService.JobId);

                    var jobBuilder = new JobInfo.Builder(
                        TrojanPlusCellurJobService.JobId,
                        new ComponentName(activity, Class.FromType(typeof(TrojanPlusCellurJobService)).Name));

                    jobBuilder.SetRequiredNetworkType(NetworkType.Cellular);

                    succ = succ && jobServ.Schedule(jobBuilder.Build()) > 0;
                }

                return succ;
            }

            public void StopMonitorNetwork(bool wifi, bool cellur)
            {
                var jobServ = (JobScheduler)activity.GetSystemService(JobSchedulerService);
                if (wifi)
                {
                    jobServ.Cancel(TrojanPlusWifiJobService.JobId);
                }

                if (cellur)
                {
                    jobServ.Cancel(TrojanPlusCellurJobService.JobId);
                }
            }
        }

        public static readonly string PrepareConfigPath = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                "config.json");

        private App app;
        private TrojanPlusStarter starter;

        private string autoStartWifiSSID;
        private bool autoStartCellur;

        public override void OnRequestPermissionsResult(
            int requestCode,
            string[] permissions,
            [GeneratedEnum] Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            AppCenter.Start("ac977bfd-2c63-4663-8fe1-4d3ea3f4750c", typeof(Analytics), typeof(Crashes));

            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            var com = new Communicator(this);
            starter = new TrojanPlusStarter(this, com);
            app = new App(PrepareConfigPath, com);

            LoadApplication(app);

            //com.StartMonitorNetwork(null, true);
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