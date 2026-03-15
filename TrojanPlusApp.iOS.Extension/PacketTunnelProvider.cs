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
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using NetworkExtension;

namespace TrojanPlusApp.iOS.Extension
{
    /// <summary>
    /// Packet Tunnel Provider for iOS VPN
    /// This runs in a separate app extension process
    /// </summary>
    [Register("PacketTunnelProvider")]
    public class PacketTunnelProvider : NEPacketTunnelProvider
    {
        private int tunnelFileDescriptor = -1;
        private Thread trojanThread;
        private bool isRunning = false;

        // P/Invoke declarations
        private const string LibraryName = "__Internal";

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void trojan_run_main([MarshalAs(UnmanagedType.LPStr)] string config_path);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void trojan_stop_main();

        // Note: Method signature needs to be verified against actual NEPacketTunnelProvider API
        public async void StartTunnelWithOptions(Action<NSError> completionHandler)
        {
            try
            {
                NSLog("PacketTunnelProvider: StartTunnel called");

                // Configure TUN interface settings
                var settings = new NEPacketTunnelNetworkSettings(new NSString("10.233.233.1"));

                // IPv4 settings
                settings.IPv4Settings = new NEIPv4Settings(
                    new string[] { "10.233.233.2" },
                    new string[] { "255.255.255.0" });

                // DNS settings
                settings.DnsSettings = new NEDnsSettings(new string[] { "8.8.8.8", "8.8.4.4" });

                // Set tunnel network settings
                await SetTunnelNetworkSettingsAsync(settings);

                // TODO: Get TUN file descriptor and start trojan
                // For now, just complete successfully
                NSLog("PacketTunnelProvider: Tunnel started successfully");
                completionHandler(null);
            }
            catch (Exception ex)
            {
                NSLog($"PacketTunnelProvider ERROR: {ex.Message}");
                completionHandler(new NSError(new NSString("TrojanPlus"), 1,
                    NSDictionary.FromObjectAndKey(new NSString(ex.Message), NSError.LocalizedDescriptionKey)));
            }
        }

        public override void StopTunnel(NEProviderStopReason reason, Action completionHandler)
        {
            try
            {
                NSLog($"PacketTunnelProvider: StopTunnel called, reason: {reason}");

                if (isRunning)
                {
                    trojan_stop_main();
                    isRunning = false;
                }

                completionHandler();
            }
            catch (Exception ex)
            {
                NSLog($"PacketTunnelProvider StopTunnel ERROR: {ex.Message}");
                completionHandler();
            }
        }

        private static void NSLog(string message)
        {
            Console.WriteLine($"[PacketTunnelProvider] {message}");
        }
    }
}
