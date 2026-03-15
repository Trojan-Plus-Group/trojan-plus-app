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

namespace TrojanPlusApp.iOS
{
    /// <summary>
    /// P/Invoke declarations for native libtrojan library
    /// Maps to the C wrapper functions in platform_exports.h/cpp
    /// </summary>
    public static class NativeInterop
    {
        // Use __Internal for statically linked libraries in iOS
        private const string LibraryName = "__Internal";

        /// <summary>
        /// Start trojan main loop with configuration file
        /// </summary>
        /// <param name="config_path">Path to trojan configuration file</param>
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void trojan_run_main([MarshalAs(UnmanagedType.LPStr)] string config_path);

        /// <summary>
        /// Stop trojan main loop (sends SIGUSR2 signal)
        /// </summary>
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void trojan_stop_main();

        /// <summary>
        /// Get trojan library version string
        /// </summary>
        /// <returns>Pointer to version string (const char*)</returns>
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr trojan_get_version();

        /// <summary>
        /// Helper method to get trojan version as managed string
        /// </summary>
        public static string GetTrojanVersion()
        {
            try
            {
                IntPtr ptr = trojan_get_version();
                if (ptr != IntPtr.Zero)
                {
                    return Marshal.PtrToStringAnsi(ptr) ?? "unknown";
                }
                return "unknown";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting trojan version: {ex.Message}");
                return "error";
            }
        }
    }
}
