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
    using Android.Support.V4.App;
    using Android.Util;
    using Java.Lang;
    using Microsoft.AppCenter;
    using Microsoft.AppCenter.Analytics;
    using Microsoft.AppCenter.Crashes;
    using Newtonsoft.Json;
    using TrojanPlusApp.Models;
    using Xamarin.Essentials;

    [Activity(
        Name = "com.trojan_plus.android.TrojanPlusMainActivity",
        Label = "@string/app_name",
        Icon = "@mipmap/icon",
        Theme = "@style/MainTheme",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class TrojanPlusMainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        public class Communicator : TrojanPlusStarter.IActivityCommunicator, App.IStart
        {
            private TrojanPlusMainActivity activity;
            public Communicator(TrojanPlusMainActivity act)
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

        public const int AutoJobServiceBackoffCriteria = 30 * 1000;
        public const int AutoJobServiceMinimumLatency = 5 * 1000;
        public const int AutoJobServiceMaxLatency = 60 * 1000;

        public static readonly string PrepareConfigPath = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                "config.json");

        public static readonly string RunningConfigSuffix = "_running";
        public static readonly string RunningConfigPath = PrepareConfigPath + RunningConfigSuffix;

        public static readonly string AutoChannelID = "TrojanPlusAutoStartStopChannel";
        public static readonly int AutoNotificationId = 1001;

        public static void ShowAutoNotification(Context context, string title)
        {
            NotificationCompat.Builder builder = new NotificationCompat.Builder(context, TrojanPlusMainActivity.AutoChannelID)
                .SetContentTitle(title)
                .SetContentIntent(TrojanPlusVPNService.CreatePendingIntent())
                .SetSmallIcon(Resource.Mipmap.notification_small_icon)
                .SetPriority((int)NotificationPriority.Low)
                .SetAutoCancel(true);

            NotificationManagerCompat notificationManager = NotificationManagerCompat.From(context);
            notificationManager.Notify(TrojanPlusMainActivity.AutoNotificationId, builder.Build());
        }

        private static readonly string TAG = typeof(TrojanPlusMainActivity).Name;
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
            jobServ.Cancel(TrojanPlusAutoJobService.JobId);

            Log.Debug(TAG, "StopJobs");
        }

        private void CreateAutoJobNotificationChannel()
        {
            // Create the NotificationChannel, but only on API 26+ because
            // the NotificationChannel class is new and not in the support library
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var name = Resx.TextResource.Notification_AutoChannelName;
                var desc = Resx.TextResource.Notification_AutoChannelDescription;

                NotificationChannel channel = new NotificationChannel(AutoChannelID, name, NotificationImportance.Low);
                channel.Description = desc;
                channel.SetShowBadge(false);

                // Register the channel with the system; you can't change the importance
                // or other notification behaviors after this
                NotificationManager notificationManager = GetSystemService(NotificationService) as NotificationManager;
                notificationManager.CreateNotificationChannel(channel);
            }
        }

        private void RefreshJobs()
        {
            StopJobs();

            if (settings != null && settings.EnableAndroidNotification)
            {
                CreateAutoJobNotificationChannel();

                var jobServ = (JobScheduler)GetSystemService(JobSchedulerService);

                if (settings.AutoStopWifi.Count > 0 || settings.AutoStartCellur)
                {
                    JobInfo.Builder jobBuilder = new JobInfo.Builder(
                        TrojanPlusAutoJobService.JobId,
                        new ComponentName(this, Class.FromType(typeof(TrojanPlusAutoJobService)).Name));

                    jobBuilder.SetRequiredNetworkType(NetworkType.Any);
                    jobBuilder.SetBackoffCriteria(AutoJobServiceBackoffCriteria, BackoffPolicy.Linear);
                    jobBuilder.SetMinimumLatency(AutoJobServiceMinimumLatency);
                    jobBuilder.SetOverrideDeadline(AutoJobServiceMaxLatency);
                    jobBuilder.SetPersisted(true);

                    PersistableBundle bundle = new PersistableBundle();
                    bundle.PutString("settings", JsonConvert.SerializeObject(settings));

                    jobBuilder.SetExtras(bundle);
                    var succ = jobServ.Schedule(jobBuilder.Build());

                    Log.Debug(TAG, "RefreshJobs  TrojanPlusAutoJobService " + succ);
                }
            }
        }
    }
}