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
using System.IO;
using Foundation;

namespace TrojanPlusApp.iOS
{
    /// <summary>
    /// Manages shared data between main app and Network Extension using App Groups
    /// </summary>
    public static class SharedDataManager
    {
        private const string AppGroupId = "group.com.trojanplus.ios";

        /// <summary>
        /// Get the shared container path for App Group
        /// </summary>
        public static string GetSharedContainerPath()
        {
            var containerUrl = NSFileManager.DefaultManager.GetContainerUrl(AppGroupId);
            return containerUrl?.Path;
        }

        /// <summary>
        /// Get the path to the trojan configuration file in shared container
        /// </summary>
        public static string GetSharedConfigPath()
        {
            var containerPath = GetSharedContainerPath();
            if (string.IsNullOrEmpty(containerPath))
            {
                Console.WriteLine("ERROR: Cannot access App Group container");
                return null;
            }

            var configPath = Path.Combine(containerPath, "trojan_config.json");
            return configPath;
        }

        /// <summary>
        /// Save configuration JSON to shared container
        /// </summary>
        public static bool SaveConfiguration(string json)
        {
            try
            {
                var configPath = GetSharedConfigPath();
                if (configPath == null)
                    return false;

                File.WriteAllText(configPath, json);
                Console.WriteLine($"Configuration saved to: {configPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load configuration JSON from shared container
        /// </summary>
        public static string LoadConfiguration()
        {
            try
            {
                var configPath = GetSharedConfigPath();
                if (configPath != null && File.Exists(configPath))
                {
                    return File.ReadAllText(configPath);
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get path to log file in shared container
        /// </summary>
        public static string GetLogFilePath()
        {
            var containerPath = GetSharedContainerPath();
            if (string.IsNullOrEmpty(containerPath))
                return null;

            return Path.Combine(containerPath, "trojan.log");
        }
    }
}
