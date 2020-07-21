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
    using System.IO;
    using Android.App;
    using Android.App.Job;
    using Android.Content;
    using Android.Net;
    using Android.Net.Wifi;
    using Android.OS;
    using Android.Runtime;
    using Android.Util;
    using Newtonsoft.Json;
    using TrojanPlusApp.Models;

    [Register("com.trojan_plus.android.TrojanPlusWifiJobService")]
    [Service(Permission = "android.permission.BIND_JOB_SERVICE")]
    public class TrojanPlusWifiJobService : JobService, TrojanPlusStarter.IActivityCommunicator
    {
        public static readonly int JobId = 1001;
        private static readonly string TAG = typeof(TrojanPlusWifiJobService).Name;

        private TrojanPlusStarter starter = null;
        private JobParameters jobParam;
        private SettingsModel settings;

        public static string GetWIFISSID(Context context)
        {
            string ssid = null;

            // this function will failed in Android Q(10) API 29
            if (Build.VERSION.SdkInt <= BuildVersionCodes.O
                || Build.VERSION.SdkInt == BuildVersionCodes.P)
            {
                WifiManager mWifiManager = (WifiManager)context.GetSystemService(Context.WifiService);
                if (mWifiManager != null)
                {
                    if (Build.VERSION.SdkInt < BuildVersionCodes.Kitkat)
                    {
                        return mWifiManager.ConnectionInfo.SSID;
                    }
                    else
                    {
                        return mWifiManager.ConnectionInfo.SSID.Replace("\"", string.Empty);
                    }
                }
            }
            else if (Build.VERSION.SdkInt == BuildVersionCodes.OMr1)
            {
                ConnectivityManager connManager = (ConnectivityManager)context.GetSystemService(Context.ConnectivityService);
                if (connManager != null)
                {
                    NetworkInfo networkInfo = connManager.ActiveNetworkInfo;
                    if (networkInfo.IsConnected)
                    {
                        if (networkInfo.ExtraInfo != null)
                        {
                            return networkInfo.ExtraInfo.Replace("\"", string.Empty);
                        }
                    }
                }
            }

            return ssid;
        }

        public override bool OnStartJob(JobParameters parm)
        {
            Log.Debug(TAG, "OnStartJob");

            if (!File.Exists(MainActivity.RunningConfigPath))
            {
                Log.Debug(TAG, "RunningConfigPath file is not exist, VPN is not running");
                return true;
            }

            if (starter == null)
            {
                jobParam = parm;
                settings = JsonConvert.DeserializeObject<SettingsModel>(jobParam.Extras.GetString("settings"));

                starter = new TrojanPlusStarter(this, this);
            }

            starter.OnJobServiceStart();
            return true;
        }

        public override bool OnStopJob(JobParameters parm)
        {
            Log.Debug(TAG, "OnStopJob");

            if (starter != null)
            {
                starter.OnJobServiceStop();
            }

            // return true to indicate to the JobManager whether you'd like to reschedule
            // this job based on the retry criteria provided at job creation-time;
            return true;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            Log.Debug(TAG, "OnDestroy");
        }

        public void SetStartBtnEnabled(bool enable)
        {
            // to do nothing
        }

        public void OnVpnIsRunning(bool running)
        {
            Log.Debug(TAG, "OnVpnIsRunning " + running);

            if (running)
            {
                var currSSID = GetWIFISSID(this);

                Log.Debug(TAG, "GetWIFISSID " + currSSID);

                if ((currSSID == null && settings.AutoStopWifi.Count > 0) // ssid will be null in Android 10+ (API 29+)
                    || settings.AutoStopWifi.Contains(currSSID))
                {
                    starter.Switch(settings); // start again to stop the service
                    MainActivity.ShowAutoNotification(this, Resx.TextResource.Notification_AutoStop);
                }
            }

            starter.OnJobServiceStop();
        }

        public string GetConfigPath()
        {
            return MainActivity.PrepareConfigPath;
        }


    }
}