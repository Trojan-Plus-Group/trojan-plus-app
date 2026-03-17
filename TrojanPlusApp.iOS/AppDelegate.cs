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

namespace TrojanPlusApp.iOS
{
    using System;
    using System.Collections.Generic;
    using Foundation;
    using TrojanPlusApp.Models;
    using UIKit;
    using Microsoft.Maui;
    using Microsoft.Maui.Hosting;

    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Microsoft.Maui.MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            // Call base first to create App instance
            bool result = base.FinishedLaunching(application, launchOptions);

            // Initialize App with config path and Starter
            string configPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "..",
                "Library",
                "config.json");

            TrojanPlusApp.App.Instance.Initialize(configPath, new Stater());

            return result;
        }

        public class Stater : TrojanPlusApp.App.IStart
        {
            private TrojanPlusVPNManager vpnManager;

            public Stater()
            {
                vpnManager = new TrojanPlusVPNManager();
                _ = vpnManager.InitializeAsync();
            }

            public string GetTrojanPlusLibVersion()
            {
                return NativeInterop.GetTrojanVersion();
            }

            public List<string> GetWifiSSIDs()
            {
                // iOS restricts WiFi SSID access
                // Requires location permission and specific entitlements
                // For now, return empty list
                var ssids = new List<string>();

                // TODO: Implement WiFi SSID retrieval if needed
                // Requires: Hotspot Configuration entitlement
                // and NEHotspotHelper API

                return ssids;
            }

            public void SettingsChanged(SettingsModel settings)
            {
                // Handle settings changes
                // If VPN is running, may need to restart with new settings
                if (vpnManager.IsConnected)
                {
                    Console.WriteLine("Settings changed while VPN is connected");
                    // TODO: Decide whether to restart VPN automatically
                }
            }

            public async void Switch(SettingsModel settings)
            {
                try
                {
                    if (vpnManager.IsConnected)
                    {
                        vpnManager.StopVPN();
                        TrojanPlusApp.App.Instance?.OnSetStartBtnEnabled(true);
                    }
                    else
                    {
                        TrojanPlusApp.App.Instance?.OnSetStartBtnEnabled(false);
                        bool success = await vpnManager.StartVPNAsync(settings);

                        if (!success)
                        {
                            Console.WriteLine("Failed to start VPN");
                            TrojanPlusApp.App.Instance?.OnSetStartBtnEnabled(true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Switch error: {ex.Message}");
                    TrojanPlusApp.App.Instance?.OnSetStartBtnEnabled(true);
                }
            }
        }
    }
}
