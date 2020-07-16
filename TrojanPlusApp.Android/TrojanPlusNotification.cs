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

namespace TrojanPlusApp.Droid
{
    using Android.App;
    using Android.Content;
    using Android.OS;
    using Android.Support.V4.App;

    public class TrojanPlusNotification
    {
        private static readonly string ChannelID = "TrojanPlusNotificationChannel";
        private NotificationCompat.Builder builder;
        private TrojanPlusVPNService service;
        public TrojanPlusNotification(TrojanPlusVPNService service)
        {
            this.service = service;

            CreateNotificationChannel();

            builder = new NotificationCompat.Builder(service, ChannelID)
                .SetWhen(0)
                .SetContentTitle(Resx.TextResource.Notification_Title)
                .SetContentIntent(service.CreatePendingIntent())
                .SetSmallIcon(Resource.Mipmap.notification_small_icon)
                .SetCategory(NotificationCompat.CategoryService)
                .SetPriority((int)NotificationPriority.Low);
        }

        public void Show()
        {
            service.StartForeground(1, builder.Build());
        }

        public void Destroy()
        {
            service.StopForeground(true);
        }

        private void CreateNotificationChannel()
        {
            // Create the NotificationChannel, but only on API 26+ because
            // the NotificationChannel class is new and not in the support library
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var name = Resx.TextResource.Notification_ChannelName;
                var desc = Resx.TextResource.Notification_ChannelDescription;

                NotificationChannel channel = new NotificationChannel(ChannelID, name, NotificationImportance.Low);
                channel.Description = desc;
                channel.SetShowBadge(false);

                // Register the channel with the system; you can't change the importance
                // or other notification behaviors after this
                NotificationManager notificationManager = service.GetSystemService(Context.NotificationService) as NotificationManager;
                notificationManager.CreateNotificationChannel(channel);
            }
        }
    }
}