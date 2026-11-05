// Copyright (C) 2026 The Trojan Plus Group Authors.
// SPDX-License-Identifier: GPL-3.0-or-later

using Android.Text;
using Android.Util;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using TrojanPlusApp.Views;

namespace TrojanPlusApp.Droid;

public class HostTextLabelHandler : LabelHandler
{
    public static readonly IPropertyMapper<HostTextLabel, HostTextLabelHandler> HostMapper =
        new PropertyMapper<HostTextLabel, HostTextLabelHandler>(Mapper)
        {
            [nameof(HostTextLabel.Text)] = MapText,
            [nameof(HostTextLabel.FontSize)] = MapText,
            [nameof(HostTextLabel.IsSelected)] = MapText,
        };

    public HostTextLabelHandler()
        : base(HostMapper)
    {
    }

    protected override void ConnectHandler(AndroidX.AppCompat.Widget.AppCompatTextView platformView)
    {
        base.ConnectHandler(platformView);
        platformView.LayoutChange += OnLayoutChange;
    }

    protected override void DisconnectHandler(AndroidX.AppCompat.Widget.AppCompatTextView platformView)
    {
        platformView.LayoutChange -= OnLayoutChange;
        base.DisconnectHandler(platformView);
    }

    private static void MapText(HostTextLabelHandler handler, HostTextLabel label)
    {
        LabelHandler.MapText(handler, label);
        handler.UpdateText();
    }

    private void OnLayoutChange(object sender, Android.Views.View.LayoutChangeEventArgs e) => UpdateText();

    private void UpdateText()
    {
        if (VirtualView is not HostTextLabel label || PlatformView.Width <= 0)
        {
            return;
        }

        var view = PlatformView;
        var width = view.Width - view.CompoundPaddingLeft - view.CompoundPaddingRight;
        if (width <= 0)
        {
            return;
        }

        view.SetSingleLine(true);

        // Native auto-size and horizontally scrolling TextViews do not cooperate. Measure with native Paint instead.
        using var paint = new Android.Graphics.Paint(view.Paint);
        var scale = TypedValue.ApplyDimension(ComplexUnitType.Sp, 1, view.Resources!.DisplayMetrics);
        var size = label.FontSize;
        var minSize = label.FontSize * HostTextLabel.MinFontScale;
        paint.TextSize = (float)(size * scale);
        while (size > minSize && paint.MeasureText(label.Text ?? string.Empty) > width)
        {
            size = System.Math.Max(minSize, size - 1);
            paint.TextSize = (float)(size * scale);
        }

        var marquee = label.IsSelected && paint.MeasureText(label.Text ?? string.Empty) > width;
        view.SetTextSize(ComplexUnitType.Sp, (float)size);
        view.Ellipsize = marquee ? TextUtils.TruncateAt.Marquee : TextUtils.TruncateAt.End;
        view.SetMarqueeRepeatLimit(-1);
        view.Selected = marquee;
    }
}
