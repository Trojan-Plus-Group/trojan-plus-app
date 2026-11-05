// Copyright (C) 2026 The Trojan Plus Group Authors.
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using TrojanPlusApp.Views;
using UIKit;

namespace TrojanPlusApp.iOS;

public class HostTextLabelHandler : LabelHandler
{
    public static readonly IPropertyMapper<HostTextLabel, HostTextLabelHandler> HostMapper =
        new PropertyMapper<HostTextLabel, HostTextLabelHandler>(Mapper)
        {
            [nameof(HostTextLabel.Text)] = MapText,
            [nameof(HostTextLabel.FontSize)] = MapText,
        };

    public HostTextLabelHandler() : base(HostMapper)
    {
    }

    private static void MapText(HostTextLabelHandler handler, HostTextLabel label)
    {
        LabelHandler.MapText(handler, label);
        handler.PlatformView.Lines = 1;
        handler.PlatformView.AdjustsFontSizeToFitWidth = true;
        handler.PlatformView.MinimumScaleFactor = (nfloat)HostTextLabel.MinFontScale;
        // ponytail: iOS truncates at minimum size; add native marquee only when iOS scrolling is required.
        handler.PlatformView.LineBreakMode = UILineBreakMode.TailTruncation;
    }
}
