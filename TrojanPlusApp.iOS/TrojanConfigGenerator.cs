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
using TrojanPlusApp.Models;

namespace TrojanPlusApp.iOS
{
    /// <summary>
    /// Generates trojan configuration file for iOS Network Extension
    /// </summary>
    public static class TrojanConfigGenerator
    {
        /// <summary>
        /// Save configuration to shared container for Network Extension to read
        /// </summary>
        /// <param name=\"settings\">Settings model</param>
        /// <returns>True if successful</returns>
        public static bool SaveConfigForExtension(SettingsModel settings)
        {
            try
            {
                var sharedPath = SharedDataManager.GetSharedContainerPath();
                if (string.IsNullOrEmpty(sharedPath))
                {
                    Console.WriteLine("ERROR: Cannot get shared container path");
                    return false;
                }

                // Get the config file path from main app
                string sourceConfigPath = TrojanPlusApp.App.Instance.ConfigPath;
                if (string.IsNullOrEmpty(sourceConfigPath) || !File.Exists(sourceConfigPath))
                {
                    Console.WriteLine($"ERROR: Source config file not found at: {sourceConfigPath}");
                    return false;
                }

                // Copy config file to shared container
                var destConfigPath = Path.Combine(sharedPath, "config.json");
                File.Copy(sourceConfigPath, destConfigPath, overwrite: true);

                Console.WriteLine($"Config copied from {sourceConfigPath} to {destConfigPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR saving config: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
    }
}
