using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.App.Job;
using Android.Content;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.Forms.Internals;

namespace TrojanPlusApp.Droid
{
    [Register("com.trojan_plus.android.TrojanPlusWifiJobService")]
    [Service(Permission = "android.permission.BIND_JOB_SERVICE")]
    public class TrojanPlusWifiJobService : JobService, TrojanPlusStarter.IActivityCommunicator
    {
        public static readonly int JobId = 1001;
        public static readonly string AutoStartWifiSSIDKey = "ssid";

        private TrojanPlusStarter starter = null;
        private string[] ssids = null;
        private JobParameters jobParam;

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
                    starter.Start(); // start again to stop the service
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