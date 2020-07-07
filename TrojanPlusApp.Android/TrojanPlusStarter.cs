using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Xamarin.Forms.Platform.Android;

namespace TrojanPlusApp.Droid
{
    public class TrojanPlusStarter
    {
        public interface IActivityCommunicator
        {
            void SetStartBtnEnabled(bool enable);
            void SetStartBtnStatus(bool running);
            string GetConfigPath();
        }

        private static readonly string TAG = typeof(TrojanPlusStarter).Name;
        public const int VPN_START = 1000;
        public const int VPN_STOP = 1001;
        public const int VPN_STATUS_ASK = 1002;

        private bool m_service_is_bound = false;
        private bool m_service_is_running = false;
        private bool m_need_send_start_msg = false;

        private readonly IActivityCommunicator m_communicator;
        private readonly Activity m_activity;

        private Messenger MessengerHandler { get; set; }

        VPNServiceConnection m_serviceConnection;
        public TrojanPlusStarter(Activity activity, IActivityCommunicator communicator)
        {
            m_communicator = communicator;
            m_activity = activity;
            m_serviceConnection = new VPNServiceConnection(this);
            MessengerHandler = new Messenger(new VPNMessageHandler(this));
        }

        private void BindVpnService()
        {
            if (!m_service_is_bound)
            {
                m_service_is_bound = true;

                Log.Debug(TAG, "BindVpnService");
                Intent serviceToStart = new Intent(m_activity, typeof(TrojanPlusVPNService));
                m_activity.BindService(serviceToStart, m_serviceConnection, Bind.AutoCreate);
                m_activity.StartService(serviceToStart);
            }
        }

        private void UnbindVpnService()
        {
            if (m_service_is_bound && m_serviceConnection.Messenger != null)
            {
                m_activity.UnbindService(m_serviceConnection);
                m_service_is_bound = false;
            }
        }

        private void RefreshBtnStatus()
        {
            m_communicator.SetStartBtnStatus(m_service_is_running);
        }

        private void SendStartMessage()
        {
            if (m_serviceConnection.Messenger != null)
            {
                try
                {
                    Bundle data = new Bundle();
                    data.PutString("config", m_communicator.GetConfigPath());

                    var msg = Message.Obtain(null, VPN_START);
                    msg.Data = data;
                    msg.ReplyTo = MessengerHandler;

                    m_serviceConnection.Messenger.Send(msg);
                    m_communicator.SetStartBtnEnabled(false);
                }
                catch (RemoteException ex)
                {
                    Log.Error(TAG, ex.Message + "\n" + ex.StackTrace);
                }
            }
        }

        private void SendAskStatusMessage()
        {
            if (m_serviceConnection.Messenger != null)
            {
                try
                {
                    var msg = Message.Obtain(null, VPN_STATUS_ASK);
                    msg.ReplyTo = MessengerHandler;

                    m_serviceConnection.Messenger.Send(msg);
                }
                catch (RemoteException ex)
                {
                    Log.Error(TAG, ex.Message + "\n" + ex.StackTrace);
                }
            }
        }

        public void Start()
        {
            if (m_service_is_running)
            {
                StopVPNService();
            }
            else
            {
                StartVPNService();
            }
        }

        private void StopVPNService()
        {
            if (m_serviceConnection.Messenger != null)
            {
                try
                {
                    var msg = Message.Obtain(null, VPN_STOP);
                    msg.ReplyTo = MessengerHandler;

                    m_serviceConnection.Messenger.Send(msg);
                    m_communicator.SetStartBtnEnabled(false);
                }
                catch (RemoteException ex)
                {
                    Log.Error(TAG, ex.Message + "\n" + ex.StackTrace);
                }
            }
        }

        private void StartVPNService()
        {
            Intent intent = VpnService.Prepare(m_activity);
            if (intent != null)
            {
                m_activity.StartActivityForResult(intent, VPN_START);
            }
            else
            {
                OnActivityResult(VPN_START, Result.Ok, null);
            }
        }

        public void OnResume()
        {
            BindVpnService();
            RefreshBtnStatus();
        }

        public void OnDestroy()
        {
            UnbindVpnService();
        }

        public bool OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode == VPN_START && resultCode == Result.Ok)
            {
                m_communicator.SetStartBtnEnabled(false);
                if (m_service_is_bound)
                {
                    SendStartMessage();
                }
                else
                {
                    m_need_send_start_msg = true;
                    BindVpnService();
                }

                return true;
            }
            return false;
        }

        private class VPNServiceConnection : Java.Lang.Object, IServiceConnection
        {
            TrojanPlusStarter m_starter;
            public Messenger Messenger { get; private set; }

            public VPNServiceConnection(TrojanPlusStarter stater)
            {
                m_starter = stater;
            }

            public void OnServiceConnected(ComponentName name, IBinder service)
            {
                Log.Debug(TAG, $"OnServiceConnected {name.ClassName}");

                if (service != null)
                {
                    Messenger = new Messenger(service);

                    if (m_starter.m_need_send_start_msg)
                    {
                        m_starter.m_need_send_start_msg = false;
                        m_starter.SendStartMessage();
                    }
                    else
                    {
                        m_starter.SendAskStatusMessage();
                    }
                }
            }

            public void OnServiceDisconnected(ComponentName name)
            {
                Log.Debug(TAG, $"OnServiceDisconnected {name.ClassName}");

                m_starter.m_service_is_running = false;
                m_starter.m_service_is_bound = false;
                m_starter.RefreshBtnStatus();
                Messenger = null;
            }
        }

        private class VPNMessageHandler : Handler
        {
            private readonly TrojanPlusStarter m_starter;

            public VPNMessageHandler(TrojanPlusStarter starter)
            {
                m_starter = starter;
            }

            public override void HandleMessage(Message msg)
            {
                int what = msg.What;
                switch (what)
                {
                    case VPN_START:
                    case VPN_STATUS_ASK:
                        {
                            m_starter.m_service_is_running = msg.Data.GetBoolean("start");

                            m_starter.m_activity.RunOnUiThread(() =>
                            {
                                m_starter.m_communicator.SetStartBtnEnabled(true);
                                m_starter.RefreshBtnStatus();

                                if (what != VPN_STATUS_ASK)
                                {
                                    if (!m_starter.m_service_is_running)
                                    {
                                        m_starter.UnbindVpnService();
                                    }
                                }
                            });
                        }
                        break;
                    default:
                        Log.Warn(TAG, $"Unknown msg.what value: {msg.What} . Ignoring this message.");
                        break;
                }
            }
        }
    }
}