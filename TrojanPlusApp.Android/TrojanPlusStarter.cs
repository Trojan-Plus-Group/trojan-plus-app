using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Util;
using Microsoft.AppCenter.Crashes;

namespace TrojanPlusApp.Droid
{
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

        public void Start()
        {
            if (serviceIsRunning)
            {
                StopVPNService();
            }
            else
            {
                StartVPNService();
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

        public void OnDestroy()
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

                Log.Debug(TAG, "BindVpnService");

                Intent serviceToStart = new Intent(context, typeof(TrojanPlusVPNService));
                context.BindService(serviceToStart, serviceConnection, Bind.AutoCreate);
                context.StartService(serviceToStart);
            }
        }

        private void UnbindVpnService()
        {
            if (serviceIsBound && serviceConnection.Messenger != null)
            {
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
                try
                {
                    Bundle data = new Bundle();
                    data.PutString("config", communicator.GetConfigPath());

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

        private void StopVPNService()
        {
            if (serviceConnection.Messenger != null)
            {
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

        private void StartVPNService()
        {
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
                Log.Debug(TAG, $"OnServiceConnected {name.ClassName}");

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
                Log.Debug(TAG, $"OnServiceDisconnected {name.ClassName}");

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