/*
 * This file is part of the Trojan Plus project.
 * Trojan is an unidentifiable mechanism that helps you bypass GFW.
 * Trojan Plus is derived from original trojan project and writing
 * for more experimental features.
 * Copyright (C) 2026 The Trojan Plus Group Authors.
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

    [Register("com.trojan_plus.android.TrojanPlusVPNService")]
    [Service(
            Name = "com.trojan_plus.android.TrojanPlusVPNService",
            Enabled = true,
            Permission = "android.permission.BIND_VPN_SERVICE",
            Process = ":vpn_remote",
            Exported = true,
            ForegroundServiceType = (Android.Content.PM.ForegroundService)0x40000000)]
    [IntentFilter(new string[] { "android.net.VpnService" })]
    public class TrojanPlusVPNService : Android.Net.VpnService
    {
        private const string VpnAddress = HostModel.TunGateWayIP;
        private const string VpnDnsServer = HostModel.TunNetIP;
        private const int VpnMtu = HostModel.TunMtu;

        private static readonly string TAG = typeof(TrojanPlusVPNService).Name;

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

        private readonly Handler mainHandler = new Handler(Looper.MainLooper);
        private Messenger messenger = null;
        private ParcelFileDescriptor vpnFD = null;

        private Thread worker = null;
        private string prepareConfigPath = null;
        private string runConfigPath = null;
        private Messenger replyMessenger = null;
        private bool showNotification = true;

        private TrojanPlusNotification notification = null;
        private bool stopping;
        private bool stopped;
        private NativeDebugLog debugLog;
        private string debugLogPath;

        public static PendingIntent CreatePendingIntent()
        {
            var intent = new Intent(Application.Context, typeof(TrojanPlusMainActivity));
            intent.SetFlags(ActivityFlags.ReorderToFront);

            var flags = PendingIntentFlags.UpdateCurrent;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            {
                flags |= PendingIntentFlags.Immutable;
            }

            return PendingIntent.GetActivity(Application.Context, 0, intent, flags);
        }

        public override void OnRevoke()
        {
            mainHandler.Post(StopVPN);
        }

        public override void OnCreate()
        {
            base.OnCreate();

            messenger = new Messenger(new VpnServiceHandler(this));
            currentService = this;
        }

        public override IBinder OnBind(Intent intent)
        {
            Log.Debug(TAG, "OnBind");
            return intent?.Action == ServiceInterface ? base.OnBind(intent) : messenger.Binder;
        }

        public override void OnDestroy()
        {
            Log.Debug(TAG, "OnDestroy");
            StopVPN();
            messenger.Dispose();
            base.OnDestroy();

            // Run after OnDestroy and queued unbind callbacks, allowing system_server to drain teardown.
            mainHandler.PostDelayed(
                () =>
                {
                    ResetProcessWhenStopped();
                },
                500);
        }

        public override bool OnUnbind(Intent intent)
        {
            return false;
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            Log.Debug(TAG, "TrojanPlusVPNService.onStartCommand");
            return StartCommandResult.Sticky;
        }

        private void ResetProcessWhenStopped()
        {
            if (worker != null)
            {
                // Native callback and writer must finish before process reset, even after OnDestroy.
                mainHandler.PostDelayed(ResetProcessWhenStopped, 500);
                return;
            }

            Log.Info(TAG, "VPN teardown complete; resetting native process");
            currentService = null;
            Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
        }

        private void StopVPN()
        {
            if (stopping)
            {
                return;
            }

            stopping = true;
            Log.Info(TAG, "Stopping VPN: removing notification and requesting native stop");
            notification?.Destroy();
            if (worker != null)
            {
                StopMain(IntPtr.Zero, IntPtr.Zero);
                return;
            }

            FinishStop();
        }

        private void FinishStop()
        {
            if (stopped)
            {
                return;
            }

            stopped = true;
            stopping = true;
            Log.Info(TAG, "Finishing VPN stop: closing TUN");
            notification?.Destroy();
            try
            {
                CloseFD();
            }
            catch (Exception)
            {
                Log.Error(TAG, "VPN cleanup failed.");
            }
            finally
            {
                worker = null;
                StopSelf();
                BroadcastStatus(false);
            }
        }

        private void BroadcastStatus(bool started)
        {
            if (replyMessenger != null)
            {
                var msg = Message.Obtain(null, TrojanPlusStarter.VPN_START);
                msg.Data = new Bundle();
                msg.Data.PutBoolean("start", started);

                try
                {
                    replyMessenger.Send(msg);
                }
                catch (RemoteException)
                {
                    // The activity may have been removed from recents.
                    replyMessenger = null;
                }
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
                    // Satisfy foreground startup before config, metadata or TUN setup can fail.
                    if (showNotification)
                    {
                        notification = new TrojanPlusNotification(this);
                        notification.Show();
                    }

                    var configFile = File.ReadAllText(prepareConfigPath);
                    var route = ParseType(configFile);
                    string metadataPath = prepareConfigPath + ".log-host";
                    string logHost = File.Exists(metadataPath) ? File.ReadAllText(metadataPath) : string.Empty;

                    Log.Debug(TAG, "VPN Route Type: " + route);

                    Builder builder = new Builder(this);
                    builder.AddAddress(VpnAddress, 32)
                                .SetMtu(VpnMtu)
                                .SetConfigureIntent(CreatePendingIntent())
                                .AddDnsServer(VpnDnsServer)
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

                        builder.AddRoute(VpnDnsServer, 32);
                    }

                    vpnFD = builder.Establish();
                    if (vpnFD == null)
                    {
                        throw new InvalidOperationException("VPN interface establishment failed.");
                    }

                    configFile = VpnRunConfig.Resolve(configFile, vpnFD.Fd);
                    debugLogPath = VpnRunConfig.GetDebugLogPath(configFile, logHost);

                    runConfigPath = prepareConfigPath + TrojanPlusMainActivity.RunningConfigSuffix;
                    File.WriteAllText(runConfigPath, configFile);

                    worker = new Thread(new WorkerThread(this).Run);
                    worker.Start();
                }
                catch (Exception)
                {
                    // Parsing exceptions can include credential-bearing config fragments.
                    Log.Error(TAG, "VPN setup failed.");
                    StopVPN();
                }
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

                if (File.Exists(runConfigPath))
                {
                    File.Delete(runConfigPath);
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
                    if (service.debugLogPath != null)
                    {
                        // Missing callback ABI fails closed; never silently run without requested capture.
                        service.debugLog = NativeDebugLog.Start(service.debugLogPath);
                    }

                    service.BroadcastStatus(true);

                    var jclass = JNIEnv.FindClass("com.trojan_plus.android.TrojanPlusVPNService");
                    RunMain(JNIEnv.Handle, jclass, path);
                }
                catch (Exception)
                {
                    // Native startup errors may contain configuration or remote data.
                    Log.Error(TAG, "Native VPN worker failed.");
                }
                finally
                {
                    JNIEnv.DeleteLocalRef(path);
                    if (service.debugLog != null)
                    {
                        try
                        {
                            if (service.debugLog.Stop())
                            {
                                service.debugLog = null;
                            }
                            else
                            {
                                Log.Warn(TAG, "Native log unregister failed; retaining callback until process exit.");
                            }
                        }
                        catch (Exception)
                        {
                            Log.Warn(TAG, "Detailed log shutdown failed; retaining callback until process exit.");
                        }
                    }
                }

                service.mainHandler.Post(() =>
                {
                    Log.Info(TAG, "RunMain returned; stopping VPN");
                    service.FinishStop();
                });
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
                        if (service.worker == null && !service.stopping)
                        {
                            Log.Debug(TAG, "on VpnServiceHandler.HandleMessage VPN_START");
                            service.prepareConfigPath = msg.Data.GetString("config");
                            service.showNotification = msg.Data.GetBoolean("showNotification");

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
                        Log.Debug(TAG, "on VpnServiceHandler.HandleMessage VPN_STOP");
                        service.StopVPN();

                        break;
                    default:
                        Log.Error(TAG, $"Unknown msg.what value: {msg.What} . Ignoring this message.");
                        break;
                }
            }
        }
    }
}