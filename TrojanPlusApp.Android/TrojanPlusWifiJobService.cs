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
    using System;
    using Android.App;
    using Android.App.Job;
    using Android.Content;
    using Android.Net;
    using Android.Net.Wifi;
    using Android.OS;
    using Android.Runtime;
    using Newtonsoft.Json;
    using TrojanPlusApp.Models;
    using Xamarin.Forms.Internals;

    [Register("com.trojan_plus.android.TrojanPlusWifiJobService")]
    [Service(Permission = "android.permission.BIND_JOB_SERVICE")]
    public class TrojanPlusWifiJobService : JobService, TrojanPlusStarter.IActivityCommunicator
    {
        public static readonly int JobId = 1001;
        public static readonly string AutoStartWifiSSIDKey = "ssid";

        private TrojanPlusStarter starter = null;
        private string[] ssids = null;
        private JobParameters jobParam;
        private SettingsModel settings;

        public static string GetWIFISSID(Context context)
        {
            string ssid = null;

            if (Build.VERSION.SdkInt <= BuildVersionCodes.O || Build.VERSION.SdkInt >= BuildVersionCodes.P)
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
            jobParam = parm;
            settings = JsonConvert.DeserializeObject<SettingsModel>(jobParam.Extras.GetString("settings"));
            if (starter == null)
            {
                starter = new TrojanPlusStarter(this, this);
            }

            ssids = parm.Extras.GetStringArray(AutoStartWifiSSIDKey);
            starter.OnJobServiceStart();
            return true;
        }

        public override bool OnStopJob(JobParameters parm)
        {
            // return true to indicate to the JobManager whether you'd like to reschedule 
            // this job based on the retry criteria provided at job creation-time; 
            return true;
        }

        public void SetStartBtnEnabled(bool enable)
        {
        }

        public void OnVpnIsRunning(bool running)
        {
            if (running && ssids != null)
            {
                var currSSID = GetWIFISSID(this);
                if (ssids.IndexOf(currSSID) != -1)
                {
                    starter.Start(settings); // start again to stop the service
                }
            }

            JobFinished(jobParam, true);
        }

        public string GetConfigPath()
        {
            return MainActivity.PrepareConfigPath;
        }
    }
}