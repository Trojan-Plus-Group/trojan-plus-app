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

namespace TrojanPlusApp.UWP
{
    using System;
    using System.Collections.Generic;
    using TrojanPlusApp.Models;

    public sealed partial class MainPage
    {
        public class Stater : TrojanPlusApp.App.IStart
        {

            public string GetTrojanPlusLibVersion()
            {
                throw new NotImplementedException();
            }

            public List<string> GetWifiSSIDs()
            {
                throw new NotImplementedException();
            }

            public void SettingsChanged(SettingsModel settings)
            {
                throw new NotImplementedException();
            }

            public void Start(SettingsModel settings)
            {
                throw new NotImplementedException();
            }
        }

        public MainPage()
        {
            this.InitializeComponent();

            LoadApplication(new TrojanPlusApp.App("", new Stater()));
        }
    }
}
