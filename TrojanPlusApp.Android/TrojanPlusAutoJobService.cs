
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

    [Register("com.trojan_plus.android.TrojanPlusAutoJobService")]
    [Service(Permission = "android.permission.BIND_JOB_SERVICE")]
    public class TrojanPlusAutoJobService : JobService, TrojanPlusStarter.IActivityCommunicator
    {
        public static readonly int JobId = 1001;
        private static readonly string TAG = typeof(TrojanPlusAutoJobService).Name;

        public enum ConnectivityType
        {
            Cellur,
            Wifi,
            None,
        }

        private TrojanPlusStarter starter = null;
        private JobParameters jobParam;
        private SettingsModel settings;
        private ConnectivityType type;
        private ConnectivitiyCallback connectivityCallback;

        public override bool OnStartJob(JobParameters parm)
        {
            Log.Debug(TAG, "OnStartJob");

            jobParam = parm;
            settings = JsonConvert.DeserializeObject<SettingsModel>(jobParam.Extras.GetString("settings"));

            ConnectivityManager connManager = (ConnectivityManager)GetSystemService(ConnectivityService);
            if (connManager != null)
            {
                var builder = new NetworkRequest.Builder();
                builder.AddTransportType(TransportType.Wifi);
                builder.AddTransportType(TransportType.WifiAware);
                builder.AddTransportType(TransportType.Cellular);

                connectivityCallback = new ConnectivitiyCallback(this);
                connManager.RegisterNetworkCallback(builder.Build(), connectivityCallback);
            }
            else
            {
                return false;
            }

            return true;
        }

        public override bool OnStopJob(JobParameters parm)
        {
            Log.Debug(TAG, "OnStopJob");

            // return true to indicate to the JobManager whether you'd like to reschedule
            // this job based on the retry criteria provided at job creation-time;
            return true;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            Log.Debug(TAG, "OnDestroy");

            if (starter != null)
            {
                starter.OnJobServiceStop();
            }

            UnregisterNetworkCallback();
        }

        public void SetStartBtnEnabled(bool enable)
        {
            // to do nothing
        }

        public void OnVpnIsRunning(bool running)
        {
            Log.Debug(TAG, "OnVpnIsRunning " + running);

            if (type == ConnectivityType.Wifi)
            {
                if (running)
                {
                    var currSSID = GetWIFISSID();

                    Log.Debug(TAG, "GetWIFISSID " + currSSID);

                    if ((currSSID == null && settings.AutoStopWifi.Count > 0) // ssid will be null in Android 10+ (API 29+)
                        || settings.AutoStopWifi.Contains(currSSID))
                    {
                        starter.Switch(settings); // start again to stop the service
                        TrojanPlusMainActivity.ShowAutoNotification(this, Resx.TextResource.Notification_AutoStop);
                    }
                }
            }
            else
            {
                if (!running)
                {
                    starter.Switch(settings); // start the service
                    TrojanPlusMainActivity.ShowAutoNotification(this, Resx.TextResource.Notification_AutoStart);
                }
            }

            starter.OnJobServiceStop();
        }

        public string GetConfigPath()
        {
            return TrojanPlusMainActivity.PrepareConfigPath;
        }

        private void OnConnectivityGot(ConnectivityType ty)
        {
            Log.Debug(TAG, "OnConnectivityGot " + ty);
            UnregisterNetworkCallback();

            type = ty;

            if (type == ConnectivityType.Wifi)
            {
                if (settings.AutoStopWifi.Count == 0)
                {
                    Log.Debug(TAG, "settings.AutoStopWifi.Count == 0");
                    JobFinished(jobParam, true);
                    return;
                }

                if (!File.Exists(TrojanPlusMainActivity.RunningConfigPath))
                {
                    Log.Debug(TAG, "RunningConfigPath file is not exist, VPN is not running");
                    JobFinished(jobParam, true);
                    return;
                }
            }
            else if (type == ConnectivityType.Cellur)
            {
                if (!settings.AutoStartCellur)
                {
                    Log.Debug(TAG, "settings.AutoStartCellur == false");
                    JobFinished(jobParam, true);
                    return;
                }

                if (!File.Exists(TrojanPlusMainActivity.PrepareConfigPath))
                {
                    Log.Debug(TAG, "PrepareConfig file is not exist");
                    JobFinished(jobParam, false);
                    return;
                }

                if (File.Exists(TrojanPlusMainActivity.RunningConfigPath))
                {
                    Log.Debug(TAG, "RunningConfigPath file is exist, VPN is running");
                    JobFinished(jobParam, true);
                    return;
                }
            }
            else
            {
                JobFinished(jobParam, true);
                return;
            }

            if (starter == null)
            {
                starter = new TrojanPlusStarter(this, this);
            }

            starter.OnJobServiceStart();
        }

        private void UnregisterNetworkCallback()
        {
            if (connectivityCallback == null)
            {
                return;
            }

            ConnectivityManager connManager = (ConnectivityManager)GetSystemService(Context.ConnectivityService);
            if (connManager != null)
            {
                connManager.UnregisterNetworkCallback(connectivityCallback);
                connectivityCallback = null;
            }
        }

        private string GetWIFISSID()
        {
            string ssid = null;

            // this function will failed in Android Q(10) API 29
            if (Build.VERSION.SdkInt <= BuildVersionCodes.O || Build.VERSION.SdkInt == BuildVersionCodes.P)
            {
                WifiManager mWifiManager = (WifiManager)GetSystemService(Context.WifiService);
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
                ConnectivityManager connManager = (ConnectivityManager)GetSystemService(Context.ConnectivityService);
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

        private class ConnectivitiyCallback : ConnectivityManager.NetworkCallback
        {
            private readonly TrojanPlusAutoJobService service;
            public ConnectivitiyCallback(TrojanPlusAutoJobService service)
            {
                this.service = service;
            }

            public override void OnCapabilitiesChanged(Network network, NetworkCapabilities networkCapabilities)
            {
                if (networkCapabilities.HasTransport(TransportType.Wifi)
                    || networkCapabilities.HasTransport(TransportType.WifiAware))
                {
                    service.OnConnectivityGot(ConnectivityType.Wifi);
                }
                else if (networkCapabilities.HasTransport(TransportType.Cellular))
                {
                    service.OnConnectivityGot(ConnectivityType.Cellur);
                }
                else
                {
                    service.OnConnectivityGot(ConnectivityType.None);
                }
            }
        }
    }
}