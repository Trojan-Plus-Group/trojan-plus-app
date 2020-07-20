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

namespace TrojanPlusApp.ViewModels
{
    using System.Windows.Input;
    using Xamarin.Essentials;
    using Xamarin.Forms;

    public class AboutViewModel : BaseViewModel
    {
        public string AppVersion
        {
            get { return VersionTracking.CurrentVersion; }
        }

        public string TrojanPlusVersion
        {
            get { return App.Instance.Starter.GetTrojanPlusLibVersion(); }
        }

        public ICommand OpenWebCommand { get; } = new Command(async () =>
        {
            await Browser.OpenAsync("https://github.com/Trojan-Plus-Group/trojan-plus-app");
        });

        public ICommand ClickCommand => new Command<string>(async (url) =>
        {
            await Browser.OpenAsync(url);
        });

        public AboutViewModel()
        {
            Title = Resx.TextResource.Menu_AboutTitle;
        }
    }
}