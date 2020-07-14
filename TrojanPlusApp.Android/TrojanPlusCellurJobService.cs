using Android.App;
using Android.App.Job;
using Android.Content;
using Android.Runtime;
using Android.Util;

namespace TrojanPlusApp.Droid
{
    [Register("com.trojan_plus.android.TrojanPlusCellurJobService")]
    [Service(Permission = "android.permission.BIND_JOB_SERVICE")]
    public class TrojanPlusCellurJobService : JobService, TrojanPlusStarter.IActivityCommunicator
    {
        public static readonly int JobId = 1002;

        private static readonly string TAG = typeof(TrojanPlusCellurJobService).Name;
        private TrojanPlusStarter starter = null;
        private JobParameters jobParam;

        public override bool OnStartJob(JobParameters parm)
        {
            Log.Debug(TAG, "OnStartJob");

            jobParam = parm;

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
                starter.Start(); // start the service
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