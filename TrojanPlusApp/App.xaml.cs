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

namespace TrojanPlusApp
{
    using System.IO;
    using TrojanPlusApp.Models;
    using TrojanPlusApp.Views;
    using Xamarin.Forms;

    public partial class App : Application
    {
        public interface IStart
        {
            // start libtrojan.so
            void Start(SettingsModel settings);

            string GetAppVersion();
            int GetAppBuild();

            string GetTrojanPlusLibVersion();

            // start job service to check network status changed
            bool StartMonitorNetwork(string[] autoStartWifiSSID, bool autoStartCellur);
            void StopMonitorNetwork(bool wifi, bool cellur);
        }

        public static App Instance
        {
            get { return (App)Current; }
        }

        public IStart Starter { get; private set; }

        public string ConfigPath { get; private set; }
        public string DataPathParent { get; private set; }

        public bool IsStartBtnEnabled { get; private set; }
        public bool IsVpnServiceRunning { get; private set; }

        public App(string configPath, IStart starter)
        {
            Starter = starter;
            DataPathParent = configPath.Substring(0, configPath.LastIndexOf(Path.DirectorySeparatorChar));
            ConfigPath = configPath;

            InitializeComponent();
            MainPage = new MainPage();
        }

        public void Start(SettingsModel settings)
        {
            Starter.Start(settings);
        }

        public void OnSetStartBtnEnabled(bool enable)
        {
            IsStartBtnEnabled = enable;
            MessagingCenter.Send(this, "Starter_OnSetStartBtnEnabled", enable);
        }

        public void OnVpnIsRunning(bool running)
        {
            IsVpnServiceRunning = running;
            MessagingCenter.Send(this, "Starter_OnVpnIsRunning", running);
        }

        /*
        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
        */
    }
}
