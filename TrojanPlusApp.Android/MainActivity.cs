/*
 * This file is part of the Trojan Plus project.
 * Trojan is an unidentifiable mechanism that helps you bypass GFW.
 * Trojan Plus is derived from original trojan project and writing
 * for more experimental features.
 * Copyright (C) 2020 The Trojan Plus Group Authors.
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

namespace TrojanPlusApp.Droid
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Android.App;
    using Android.App.Job;
    using Android.Content;
    using Android.Content.PM;
    using Android.Net.Wifi;
    using Android.OS;
    using Android.Runtime;
    using Android.Util;
    using Java.Lang;
    using Microsoft.AppCenter;
    using Microsoft.AppCenter.Analytics;
    using Microsoft.AppCenter.Crashes;
    using Newtonsoft.Json;
    using TrojanPlusApp.Models;
    using Xamarin.Essentials;

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

            public void Switch(SettingsModel settings)
            {
                activity.starter.Switch(settings);
            }

            public string GetTrojanPlusLibVersion()
            {
                return TrojanPlusVPNService.GetTrojanPlusLibVersion();
            }

            public List<string> GetWifiSSIDs()
            {
                var mgr = activity.GetSystemService(WifiService) as WifiManager;
                return mgr.ConfiguredNetworks.Select(s => s.Ssid.Replace("\"", string.Empty)).ToList();
            }

            public void SettingsChanged(SettingsModel settings)
            {
                activity.settings = settings;
            }
        }

        public static readonly string PrepareConfigPath = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                "config.json");

        private static readonly string TAG = typeof(MainActivity).Name;

        private App app;
        private TrojanPlusStarter starter;

        private SettingsModel settings = null;

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
            StopJobs();
            starter.OnResume();
            base.OnResume();
        }

        protected override void OnStop()
        {
            starter.OnStop();
            RefreshJobs();
            base.OnStop();
        }

        private void StopJobs()
        {
            var jobServ = (JobScheduler)GetSystemService(JobSchedulerService);
            jobServ.Cancel(TrojanPlusWifiJobService.JobId);
            jobServ.Cancel(TrojanPlusCellurJobService.JobId);

            Log.Debug(TAG, "StopJobs");
        }

        private void RefreshJobs()
        {
            StopJobs();

            if (settings != null && settings.EnableAndroidNotification)
            {
                var jobServ = (JobScheduler)GetSystemService(JobSchedulerService);

                if (settings.AutoStopWifi.Count > 0)
                {
                    JobInfo.Builder jobBuilder = new JobInfo.Builder(
                        TrojanPlusWifiJobService.JobId,
                        new ComponentName(this, Class.FromType(typeof(TrojanPlusWifiJobService)).Name));

                    jobBuilder.SetRequiredNetworkType(NetworkType.Unmetered);

                    PersistableBundle bundle = new PersistableBundle();
                    bundle.PutString("settings", JsonConvert.SerializeObject(settings));

                    jobBuilder.SetExtras(bundle);
                    var succ = jobServ.Schedule(jobBuilder.Build());

                    Log.Debug(TAG, "RefreshJobs  TrojanPlusWifiJobService " + succ);
                }

                if (settings.AutoStartCellur)
                {
                    var jobBuilder = new JobInfo.Builder(
                        TrojanPlusCellurJobService.JobId,
                        new ComponentName(this, Class.FromType(typeof(TrojanPlusCellurJobService)).Name));

                    PersistableBundle bundle = new PersistableBundle();
                    bundle.PutString("settings", JsonConvert.SerializeObject(settings));
                    jobBuilder.SetExtras(bundle);

                    jobBuilder.SetRequiredNetworkType(NetworkType.Cellular);
                    var succ = jobServ.Schedule(jobBuilder.Build());

                    Log.Debug(TAG, "RefreshJobs  TrojanPlusCellurJobService " + succ);
                }
            }
        }
    }
}