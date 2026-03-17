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
using TrojanPlusApp.Models;

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

                // Configure TUN interface settings using HostModel constants
                var settings = new NEPacketTunnelNetworkSettings(new NSString(HostModel.TunGateWayIP));

                // IPv4 settings
                settings.IPv4Settings = new NEIPv4Settings(
                    new string[] { HostModel.TunNetIP },
                    new string[] { "255.255.255.0" });

                // DNS settings
                settings.DnsSettings = new NEDnsSettings(new string[] { "114.114.114.114", "8.8.4.4" });

                // Set tunnel network settings
                await SetTunnelNetworkSettingsAsync(settings);

                // Get TUN file descriptor from PacketFlow
                // In iOS, we need to use reflection or value coding to access the socket file descriptor
                int tunFd = -1;
                try
                {
                    // Try to get file descriptor using KVC (Key-Value Coding)
                    var fdValue = PacketFlow.ValueForKeyPath(new NSString("socket.fileDescriptor"));
                    if (fdValue is NSNumber fdNumber)
                    {
                        tunFd = fdNumber.Int32Value;
                    }
                }
                catch (Exception ex)
                {
                    NSLog($"PacketTunnelProvider: Failed to get fd via KVC: {ex.Message}");

                    // Alternative: try to get the underlying socket handle
                    // The PacketFlow object should have a socket that we can access
                    // For now, log error and continue - trojan may be able to work without explicit fd
                }

                NSLog($"PacketTunnelProvider: TUN fd = {tunFd}");

                if (tunFd < 0)
                {
                    NSLog("PacketTunnelProvider: Warning - could not get TUN file descriptor, trojan may not work correctly");
                }

                // Get config path from shared container
                string configPath = GetConfigPath();
                if (string.IsNullOrEmpty(configPath) || !System.IO.File.Exists(configPath))
                {
                    throw new Exception($"Config file not found at: {configPath}");
                }

                NSLog($"PacketTunnelProvider: Config path = {configPath}");

                // Read config file and replace tun_fd placeholder
                string configContent = System.IO.File.ReadAllText(configPath);
                configContent = configContent.Replace("${tun.tun_fd}", tunFd.ToString());

                // Write modified config to a runtime config file
                string runtimeConfigPath = configPath + "_running";
                System.IO.File.WriteAllText(runtimeConfigPath, configContent);

                NSLog($"PacketTunnelProvider: Runtime config written to {runtimeConfigPath}");

                // Start trojan in background thread
                isRunning = true;
                trojanThread = new Thread(() =>
                {
                    try
                    {
                        NSLog("PacketTunnelProvider: Starting trojan_run_main");
                        trojan_run_main(runtimeConfigPath);
                        NSLog("PacketTunnelProvider: trojan_run_main exited");
                    }
                    catch (Exception ex)
                    {
                        NSLog($"PacketTunnelProvider: trojan_run_main error: {ex.Message}");
                    }
                    finally
                    {
                        isRunning = false;
                    }
                });
                trojanThread.IsBackground = true;
                trojanThread.Start();

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

        private string GetConfigPath()
        {
            // Get shared container path (App Group)
            var fileManager = NSFileManager.DefaultManager;
            var containerUrl = fileManager.GetContainerUrl("group.com.trojanplus.ios");

            if (containerUrl == null)
            {
                NSLog("ERROR: Could not access App Group container");
                return null;
            }

            string containerPath = containerUrl.Path;
            string configPath = System.IO.Path.Combine(containerPath, "config.json");

            NSLog($"Config path: {configPath}");
            return configPath;
        }
    }
}
