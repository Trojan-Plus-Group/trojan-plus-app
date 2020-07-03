using System;
using System.Collections.Generic;

namespace TrojanPlusApp.Models
{
    public class HostModel
    {
        public enum RouteType
        {
            Route_all = 0, // controlled by route table
            Route_bypass_local = 1, // controlled by route table
            Route_bypass_cn_mainland = 2,
            Route_bypass_local_and_cn_mainland = 3,
            Route_gfwlist = 4,
            Route_cn_mainland = 5,
        }

        public string HostName { get; set; }
        public string HostAddress { get; set; }
        public int HostPort { get; set; }
        public string Password { get; set; }
        public bool SSLVerify { get; set; }
        public RouteType Route { get; set; }

        // Experimental Options
        public bool EnablePipeline { get; set; }
        public string PipelineLoadbalance { get; set; }

        // For UI display
        public string UI_Description
        {
            get
            {
                string res = string.Format("{0}:{1}", HostAddress, HostPort);
                if (EnablePipeline)
                {
                    res += " (Pipeline Mode)";
                }

                return res;
            }
        }
    }
}