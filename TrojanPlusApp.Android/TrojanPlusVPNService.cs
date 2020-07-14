using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Java.Interop;
using Microsoft.AppCenter.Crashes;
using TrojanPlusApp.Models;

namespace TrojanPlusApp.Droid
{
    [Register("com.trojan_plus.android.TrojanPlusVPNService")]
    [Service(
            Name = "com.trojan_plus.android.TrojanPlusVPNService",
            Enabled = true,
            Permission = "android.permission.BIND_VPN_SERVICE",
            Process = ":vpn_remote",
            Exported = true)]
    [IntentFilter(new string[] { "android.net.VpnService" })]
    public class TrojanPlusVPNService : Android.Net.VpnService
    {
        private static readonly string TAG = typeof(TrojanPlusVPNService).Name;

        private const string VPN_ADDRESS = "10.233.233.1";
        private const string VPN_DNS_SERVER = "10.233.233.2";
        private const int VPN_MTU = 1500;

        [DllImport("trojan.so", EntryPoint = "Java_com_trojan_1plus_android_TrojanPlusVPNService_runMain")]
        public static extern void RunMain(IntPtr jnienv, IntPtr jclass, IntPtr configPath);

        [DllImport("trojan.so", EntryPoint = "Java_com_trojan_1plus_android_TrojanPlusVPNService_stopMain")]
        public static extern void StopMain(IntPtr jnienv, IntPtr jclass);

        [DllImport("trojan.so", EntryPoint = "Java_com_trojan_1plus_android_TrojanPlusVPNService_getVersion")]
        public static extern IntPtr GetVersion(IntPtr jnienv, IntPtr jclass);

        public static string GetTrojanPlusLibVersion()
        {
            return JNIEnv.GetString(GetVersion(JNIEnv.Handle, IntPtr.Zero), JniHandleOwnership.TransferLocalRef);
        }

        [Export("protectSocket")]
        public static void ProtectSocket(int socket)
        {
            if (currentService != null)
            {
                currentService.Protect(socket);
            }
        }

        private static TrojanPlusVPNService currentService = null;

        private Messenger messenger = null;
        private ParcelFileDescriptor vpnFD = null;

        private Thread worker = null;
        private string prepareConfigPath = null;
        private string runConfigPath = null;
        private Messenger replyMessenger = null;

        public override void OnCreate()
        {
            base.OnCreate();
            messenger = new Messenger(new VpnServiceHandler(this));
            currentService = this;
        }

        public override IBinder OnBind(Intent intent)
        {
            Log.Debug(TAG, "OnBind");
            return messenger.Binder;
        }

        public override void OnDestroy()
        {
            Log.Debug(TAG, "OnDestroy");
            messenger.Dispose();
            currentService = null;

            base.OnDestroy();

            // kill this service to reset memory, otherwise libtrojan.so won't work
            Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
        }

        public override bool OnUnbind(Intent intent)
        {
            OnStartCommand(intent, 0, 0);
            return false;
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            Log.Debug(TAG, "TrojanPlusVPNService.onStartCommand");
            return StartCommandResult.Sticky;
        }

        private void BroadcastStatus(bool started)
        {
            if (replyMessenger != null)
            {
                var msg = Message.Obtain(null, TrojanPlusStarter.VPN_START);
                msg.Data = new Bundle();
                msg.Data.PutBoolean("start", started);

                replyMessenger.Send(msg);
            }
        }

        private HostModel.RouteType ParseType(string configFile)
        {
            var begin = configFile.IndexOf("\"proxy_type\"");
            if (begin == -1)
            {
                return HostModel.RouteType.Route_all;
            }

            begin = configFile.IndexOf(":", begin) + 1;
            var end = configFile.IndexOf(",", begin);

            if (end <= begin)
            {
                return HostModel.RouteType.Route_all;
            }

            var typeStr = configFile.Substring(begin, end - begin).Trim();
            int type;
            if (int.TryParse(typeStr, out type))
            {
                return (HostModel.RouteType)type;
            }
            else
            {
                return HostModel.RouteType.Route_all;
            }
        }

        private void OpenFD()
        {
            if (vpnFD == null)
            {
                try
                {
                    var configFile = File.ReadAllText(prepareConfigPath);
                    var route = ParseType(configFile);

                    Log.Debug(TAG, "VPN Route Type: " + route);

                    var intent = new Intent(Application.Context, typeof(MainActivity));
                    intent.SetFlags(ActivityFlags.ReorderToFront);

                    PendingIntent pintent = PendingIntent.GetActivity(Application.Context, 0, intent, 0);

                    Builder builder = new Builder(this);
                    builder.AddAddress(VPN_ADDRESS, 32)
                                .SetMtu(VPN_MTU)
                                .SetConfigureIntent(pintent)
                                .AddDnsServer(VPN_DNS_SERVER)
                                .SetSession(GetString(Resource.String.app_name));

                    if (route == HostModel.RouteType.Route_all
                        || route == HostModel.RouteType.Route_bypass_cn_mainland)
                    {
                        builder.AddRoute("0.0.0.0", 0);
                    }
                    else
                    {
                        var ips = Resources.GetTextArray(Resource.Array.bypass_private_route);
                        foreach (var ip in ips)
                        {
                            string[] addr = ip.Split('/');
                            builder.AddRoute(addr[0], int.Parse(addr[1]));
                        }

                        builder.AddRoute(VPN_DNS_SERVER, 32);
                    }

                    vpnFD = builder.Establish();

                    configFile = configFile.Replace("${tun.tun_fd}", vpnFD.Fd.ToString());

                    runConfigPath = prepareConfigPath + "_running";
                    File.WriteAllText(runConfigPath, configFile);
                }
                catch (Exception ex)
                {
                    Crashes.TrackError(ex);
                    Log.Error(TAG, ex.StackTrace);
                    CloseFD();
                    StopSelf();
                    return;
                }

                worker = new Thread(new WorkerThread(this).Run);
                worker.Start();
            }
        }

        private void CloseFD()
        {
            if (vpnFD != null)
            {
                try
                {
                    Log.Debug(TAG, "close fd");
                    vpnFD.Close();
                }
                catch (Exception ex)
                {
                    Crashes.TrackError(ex);
                    Log.Error(TAG, ex.Message + "\n" + ex.StackTrace);
                }

                vpnFD = null;
            }
        }

        private class WorkerThread
        {
            private readonly TrojanPlusVPNService service;
            public WorkerThread(TrojanPlusVPNService service)
            {
                this.service = service;
            }

            public void Run()
            {
                IntPtr path = JNIEnv.NewString(service.runConfigPath);
                try
                {
                    service.BroadcastStatus(true);

                    var jclass = JNIEnv.FindClass("com.trojan_plus.android.TrojanPlusVPNService");
                    RunMain(JNIEnv.Handle, jclass, path);
                }
                catch (Exception ex)
                {
                    Crashes.TrackError(ex);
                    Log.Error(TAG, ex.Message + "\n" + ex.StackTrace);
                }
                finally
                {
                    JNIEnv.DeleteLocalRef(path);
                }

                service.CloseFD();
                service.StopSelf();
                service.worker = null;
                service.BroadcastStatus(false);
            }
        }

        private class VpnServiceHandler : Handler
        {
            private readonly TrojanPlusVPNService service;
            public VpnServiceHandler(TrojanPlusVPNService service)
            {
                this.service = service;
            }

            public override void HandleMessage(Message msg)
            {
                service.replyMessenger = msg.ReplyTo;
                switch (msg.What)
                {
                    case TrojanPlusStarter.VPN_START:
                        if (service.worker == null)
                        {
                            Log.Debug(TAG, "on VpnServiceHandler.HandleMessage VPN_START");
                            service.prepareConfigPath = msg.Data.GetString("config");
                            service.OpenFD();
                        }

                        break;
                    case TrojanPlusStarter.VPN_STATUS_ASK:
                        {
                            var reply = Message.Obtain(null, TrojanPlusStarter.VPN_STATUS_ASK);
                            reply.Data = new Bundle();
                            reply.Data.PutBoolean("start", service.worker != null);

                            msg.ReplyTo.Send(reply);
                        }

                        break;
                    case TrojanPlusStarter.VPN_STOP:
                        if (service.worker != null)
                        {
                            Log.Debug(TAG, "on VpnServiceHandler.HandleMessage VPN_STOP");
                            StopMain(IntPtr.Zero, IntPtr.Zero);
                        }
                        else
                        {
                            service.StopSelf();
                        }

                        break;
                    default:
                        Log.Error(TAG, $"Unknown msg.what value: {msg.What} . Ignoring this message.");
                        break;
                }
            }
        }
    }
}