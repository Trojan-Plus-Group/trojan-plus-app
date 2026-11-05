// Copyright (C) 2026 The Trojan Plus Group Authors.
// SPDX-License-Identifier: GPL-3.0-or-later

namespace TrojanPlusApp.Views
{
    public class HostTextLabel : Label
    {
        public const double MinFontScale = 0.7;

        public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(
            nameof(IsSelected), typeof(bool), typeof(HostTextLabel), false);

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }
    }
}
