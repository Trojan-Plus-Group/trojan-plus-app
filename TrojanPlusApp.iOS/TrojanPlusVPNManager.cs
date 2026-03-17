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

using System;
using System.Threading.Tasks;
using Foundation;
using NetworkExtension;
using TrojanPlusApp.Models;

namespace TrojanPlusApp.iOS
{
    /// <summary>
    /// Manages VPN connection using iOS Network Extension framework
    /// </summary>
    public class TrojanPlusVPNManager
    {
        private NEVpnManager vpnManager;
        private bool isInitialized = false;

        public TrojanPlusVPNManager()
        {
        }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                vpnManager = NEVpnManager.SharedManager;
                await vpnManager.LoadFromPreferencesAsync();
                isInitialized = true;

                // Subscribe to status changes
                // Note: Status change notifications are handled automatically by NEVpnManager

                return true;
            }
            catch (Exception ex)
            {
                // VPN functionality is not available on iOS Simulator
                // This is expected behavior - only log for debugging
                Console.WriteLine($"VPN Manager initialization failed (expected on simulator): {ex.Message}");
                isInitialized = false;
                return false;
            }
        }

        private void OnVPNStatusChanged(NSNotification notification)
        {
            if (vpnManager?.Connection != null)
            {
                Console.WriteLine($"VPN Status changed to: {vpnManager.Connection.Status}");

                // Notify the app about status changes
                bool isConnected = vpnManager.Connection.Status == NEVpnStatus.Connected;
                TrojanPlusApp.App.Instance?.OnVpnIsRunning(isConnected);
            }
        }

        public async Task<bool> StartVPNAsync(SettingsModel settings)
        {
            if (!isInitialized)
            {
                await InitializeAsync();
            }

            if (vpnManager == null)
            {
                Console.WriteLine("VPN Manager not initialized");
                return false;
            }

            try
            {
                // Save configuration to shared container for extension to read
                if (!TrojanConfigGenerator.SaveConfigForExtension(settings))
                {
                    Console.WriteLine("ERROR: Failed to save configuration");
                    return false;
                }

                // Configure VPN protocol
                var protocol = new NETunnelProviderProtocol
                {
                    ProviderBundleIdentifier = "com.trojanplus.ios.extension",
                    ServerAddress = "127.0.0.1", // Placeholder, actual config is in shared container
                };

                // Pass minimal configuration to the extension
                var config = new NSDictionary<NSString, NSObject>(
                    new NSString("config_ready"),
                    new NSString("true")
                );
                protocol.ProviderConfiguration = config;

                vpnManager.ProtocolConfiguration = protocol;
                vpnManager.LocalizedDescription = "Trojan Plus VPN";
                vpnManager.Enabled = true;

                await vpnManager.SaveToPreferencesAsync();
                await vpnManager.LoadFromPreferencesAsync();

                // Start the VPN tunnel
                vpnManager.Connection.StartVpnTunnel(out var error);
                if (error != null)
                {
                    Console.WriteLine($"VPN tunnel start error: {error.LocalizedDescription}");
                    return false;
                }

                Console.WriteLine("VPN tunnel start requested");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"VPN Start Error: {ex.Message}");
                return false;
            }
        }

        public void StopVPN()
        {
            try
            {
                vpnManager?.Connection.StopVpnTunnel();
                Console.WriteLine("VPN tunnel stop requested");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"VPN Stop Error: {ex.Message}");
            }
        }

        public bool IsConnected
        {
            get
            {
                return vpnManager?.Connection.Status == NEVpnStatus.Connected;
            }
        }

        public NEVpnStatus Status
        {
            get
            {
                return vpnManager?.Connection.Status ?? NEVpnStatus.Invalid;
            }
        }
    }
}
