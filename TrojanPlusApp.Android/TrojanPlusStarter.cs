﻿/*
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
    using Android.App;
    using Android.Content;
    using Android.Net;
    using Android.OS;
    using Android.Util;
    using Microsoft.AppCenter.Crashes;
    using TrojanPlusApp.Models;

    public class TrojanPlusStarter
    {
        public interface IActivityCommunicator
        {
            void SetStartBtnEnabled(bool enable);
            void OnVpnIsRunning(bool running);
            string GetConfigPath();
        }

        public const int VPN_START = 1000;
        public const int VPN_STOP = 1001;
        public const int VPN_STATUS_ASK = 1002;

        private static readonly string TAG = typeof(TrojanPlusStarter).Name;

        private readonly IActivityCommunicator communicator;
        private readonly Context context;
        private readonly Activity activity;
        private readonly VPNServiceConnection serviceConnection;
        private readonly Messenger messengerHandler;

        private SettingsModel settings;
        private bool serviceIsBound = false;
        private bool serviceIsRunning = false;
        private bool needSendStartMsg = false;

        public TrojanPlusStarter(Context context, IActivityCommunicator communicator)
        {
            this.communicator = communicator;
            this.context = context;
            activity = context as Activity;
            serviceConnection = new VPNServiceConnection(this);
            messengerHandler = new Messenger(new VPNMessageHandler(this));
        }

        public void Switch(SettingsModel settings)
        {
            this.settings = settings;

            if (serviceIsRunning)
            {
                StopVPNService();
            }
            else
            {
                StartVPNService();
            }
        }

        public void StopVPNService()
        {
            if (serviceIsBound && serviceConnection.Messenger != null)
            {
                Log.Debug(TAG, "StopVPNService " + context.GetType().Name);

                try
                {
                    var msg = Message.Obtain(null, VPN_STOP);
                    msg.ReplyTo = messengerHandler;

                    serviceConnection.Messenger.Send(msg);
                    communicator.SetStartBtnEnabled(false);
                }
                catch (RemoteException ex)
                {
                    Crashes.TrackError(ex);
                    Log.Error(TAG, ex.Message + "\n" + ex.StackTrace);
                }
            }
        }

        public void OnJobServiceStart()
        {
            BindVpnService();
        }

        public void OnJobServiceStop()
        {
            UnbindVpnService();
        }

        public void OnResume()
        {
            BindVpnService();
            RefreshRunningStatus();
        }

        public void OnStop()
        {
            UnbindVpnService();
        }

        public bool OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode == VPN_START && resultCode == Result.Ok)
            {
                communicator.SetStartBtnEnabled(false);
                if (serviceIsBound)
                {
                    SendStartMessage();
                }
                else
                {
                    needSendStartMsg = true;
                    BindVpnService();
                }

                return true;
            }

            return false;
        }

        private void BindVpnService()
        {
            if (!serviceIsBound)
            {
                serviceIsBound = true;

                Log.Debug(TAG, "BindVpnService " + context.GetType().Name);

                Intent serviceToStart = new Intent(context, typeof(TrojanPlusVPNService));
                context.BindService(serviceToStart, serviceConnection, Bind.AutoCreate);
            }
        }

        private void UnbindVpnService()
        {
            if (serviceIsBound && serviceConnection != null)
            {
                Log.Debug(TAG, "UnbindVpnService " + context.GetType().Name);

                context.UnbindService(serviceConnection);
                serviceIsBound = false;
            }
        }

        private void RefreshRunningStatus()
        {
            communicator.OnVpnIsRunning(serviceIsRunning);
        }

        private void SendStartMessage()
        {
            if (serviceConnection.Messenger != null)
            {
                Intent serviceToStart = new Intent(context, typeof(TrojanPlusVPNService));
                if (settings == null || settings.EnableAndroidNotification)
                {
                    context.StartForegroundService(serviceToStart);
                }
                else
                {
                    context.StartService(serviceToStart);
                }

                Log.Debug(TAG, "SendStartMessage " + context.GetType().Name);

                try
                {
                    Bundle data = new Bundle();
                    data.PutString("config", communicator.GetConfigPath());
                    data.PutBoolean("showNotification", settings == null ? true : settings.EnableAndroidNotification);

                    var msg = Message.Obtain(null, VPN_START);
                    msg.Data = data;
                    msg.ReplyTo = messengerHandler;

                    serviceConnection.Messenger.Send(msg);
                    communicator.SetStartBtnEnabled(false);
                }
                catch (RemoteException ex)
                {
                    Crashes.TrackError(ex);
                    Log.Error(TAG, ex.Message + "\n" + ex.StackTrace);
                }
            }
        }

        private void SendAskStatusMessage()
        {
            if (serviceConnection.Messenger != null)
            {
                Log.Debug(TAG, "SendAskStatusMessage " + context.GetType().Name);

                try
                {
                    var msg = Message.Obtain(null, VPN_STATUS_ASK);
                    msg.ReplyTo = messengerHandler;

                    serviceConnection.Messenger.Send(msg);
                }
                catch (RemoteException ex)
                {
                    Crashes.TrackError(ex);
                    Log.Error(TAG, ex.Message + "\n" + ex.StackTrace);
                }
            }
        }



        private void StartVPNService()
        {
            Log.Debug(TAG, "StartVPNService");

            Intent intent = VpnService.Prepare(context);
            if (intent != null)
            {
                if (activity != null)
                {
                    activity.StartActivityForResult(intent, VPN_START);
                }
            }
            else
            {
                OnActivityResult(VPN_START, Result.Ok, null);
            }
        }

        private class VPNServiceConnection : Java.Lang.Object, IServiceConnection
        {
            private readonly TrojanPlusStarter starter;
            public Messenger Messenger { get; private set; }

            public VPNServiceConnection(TrojanPlusStarter starter)
            {
                this.starter = starter;
            }

            public void OnServiceConnected(ComponentName name, IBinder service)
            {
                Log.Debug(TAG, "OnServiceConnected " + starter.context.GetType().Name);

                if (service != null)
                {
                    Messenger = new Messenger(service);

                    if (starter.needSendStartMsg)
                    {
                        starter.needSendStartMsg = false;
                        starter.SendStartMessage();
                    }
                    else
                    {
                        starter.SendAskStatusMessage();
                    }
                }
            }

            public void OnServiceDisconnected(ComponentName name)
            {
                Log.Debug(TAG, "OnServiceDisconnected " + starter.context.GetType().Name);

                starter.serviceIsRunning = false;
                starter.serviceIsBound = false;
                starter.RefreshRunningStatus();
                Messenger = null;
            }
        }

        private class VPNMessageHandler : Handler
        {
            private readonly TrojanPlusStarter starter;
            public VPNMessageHandler(TrojanPlusStarter starter)
            {
                this.starter = starter;
            }

            public override void HandleMessage(Message msg)
            {
                int what = msg.What;
                switch (what)
                {
                    case VPN_START:
                    case VPN_STATUS_ASK:
                        {
                            Log.Debug(TAG, $"HandleMessage {what} " + starter.context.GetType().Name);

                            starter.serviceIsRunning = msg.Data.GetBoolean("start");

                            if (starter.activity != null)
                            {
                                starter.activity.RunOnUiThread(() => ProcessMessage(what));
                            }
                            else
                            {
                                ProcessMessage(what);
                            }
                        }

                        break;
                    default:
                        Log.Warn(TAG, $"Unknown msg.what value: {msg.What} . Ignoring this message.");
                        break;
                }
            }

            private void ProcessMessage(int what)
            {
                starter.communicator.SetStartBtnEnabled(true);
                starter.RefreshRunningStatus();
                if (what != VPN_STATUS_ASK)
                {
                    if (!starter.serviceIsRunning)
                    {
                        starter.UnbindVpnService();
                    }
                }
            }
        }
    }
}