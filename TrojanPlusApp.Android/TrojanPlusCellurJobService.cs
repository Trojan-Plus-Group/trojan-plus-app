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
    using Android.App.Job;
    using Android.Runtime;
    using Android.Util;
    using Newtonsoft.Json;
    using TrojanPlusApp.Models;

    [Register("com.trojan_plus.android.TrojanPlusCellurJobService")]
    [Service(Permission = "android.permission.BIND_JOB_SERVICE")]
    public class TrojanPlusCellurJobService : JobService, TrojanPlusStarter.IActivityCommunicator
    {
        public static readonly int JobId = 1002;

        private static readonly string TAG = typeof(TrojanPlusCellurJobService).Name;
        private TrojanPlusStarter starter = null;
        private JobParameters jobParam;
        private SettingsModel settings;

        public override bool OnStartJob(JobParameters parm)
        {
            Log.Debug(TAG, "OnStartJob");

            jobParam = parm;
            settings = JsonConvert.DeserializeObject<SettingsModel>(jobParam.Extras.GetString("settings"));

            if (starter == null)
            {
                starter = new TrojanPlusStarter(this, this);
            }

            starter.OnJobServiceStart();

            // Return true from this method if your job needs to continue running.
            return true;
        }

        public override bool OnStopJob(JobParameters parm)
        {
            Log.Debug(TAG, "OnStopJob");

            if (starter != null)
            {
                starter.OnJobServiceStop();
            }

            // return true to indicate to the JobManager whether you'd like to reschedule
            // this job based on the retry criteria provided at job creation-time;
            return true;
        }

        public void SetStartBtnEnabled(bool enable)
        {
        }

        public void OnVpnIsRunning(bool running)
        {
            Log.Debug(TAG, "OnVpnIsRunning " + running);

            if (!running)
            {
                starter.Start(settings); // start the service
            }
            else
            {
                starter.OnJobServiceStop();
                JobFinished(jobParam, true);
            }
        }

        public string GetConfigPath()
        {
            return MainActivity.PrepareConfigPath;
        }
    }
}