using System;
using System.Collections.Generic;
using System.Text;

namespace TrojanPlusApp.Models
{
    public class SettingsModel : NotificationModel
    {
        public int HostSelectedIdx { get; set; }
        public string HostRunningName { get; set; }
    }
}
