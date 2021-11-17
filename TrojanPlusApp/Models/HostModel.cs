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

namespace TrojanPlusApp.Models
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Newtonsoft.Json;
    using TrojanPlusApp.Behaviors;
    using TrojanPlusApp.ViewModels;
    using Xamarin.Essentials;
    using Xamarin.Forms;

    public class HostModel : NotificationModel
    {
        public const string TunGateWayIP = "10.233.233.1";
        public const string TunNetIP = "10.233.233.2";
        public const int TunMtu = 1500;

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
        public ushort HostPort { get; set; } = 443;
        public string Password { get; set; }
        public bool SSLVerify { get; set; } = true;
        public RouteType Route { get; set; } = RouteType.Route_all;
        public string UpStreamNS { get; set; } = "114.114.114.114";
        public string GFWUpStreamNS { get; set; } = "8.8.8.8";

        // Experimental Options
        public bool EnablePipeline { get; set; }
        public List<string> LoadBalance { get; set; } = new List<string>();

        public bool EnableTCPFastOpen { get; set; }

        // Debug Options
        public bool EnableDebugLog { get; set; } = false;

        // For UI display
        [JsonIgnore]
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

        [JsonIgnore]
        private bool uiSelected = false;

        [JsonIgnore]
        public bool UI_Selected
        {
            get { return uiSelected; }
            set { SetProperty(ref uiSelected, value, "UI_SelectedColor"); }
        }

        [JsonIgnore]
        public Color UI_SelectedColor => UI_Selected ? Color.Black : Color.LightGray;

        [JsonIgnore]
        public int UI_Route
        {
            get { return (int)Route; }
            set { Route = (RouteType)value; }
        }

        [JsonIgnore]
        private bool uiNotRunning = true;

        [JsonIgnore]
        public bool UI_NotRunning
        {
            get
            {
                return uiNotRunning;
            }
            set
            {
                if (SetProperty(ref uiNotRunning, value))
                {
                    OnPropertyChanged("UI_NotRunningOpacity");
                }
            }
        }

        public double UI_NotRunningOpacity
        {
            get { return UI_NotRunning ? 1 : 0.3; }
        }

        // Utils
        public HostModel Duplicate(string newHostName)
        {
            var newOne = (HostModel)MemberwiseClone();
            newOne.HostName = newHostName;
            newOne.uiSelected = false;
            newOne.uiNotRunning = true;
            newOne.LoadBalance = new List<string>();
            newOne.LoadBalance.AddRange(LoadBalance);
            return newOne;
        }

        public bool IsValid()
        {
            return HostAddressValidation.IsValid(HostAddress)
                && HostPort > 0
                && !string.IsNullOrEmpty(Password)
                && IPAddressValidation.IsValid(UpStreamNS)
                && IPAddressValidation.IsValid(GFWUpStreamNS);
        }

        private static readonly string ConfigTemplate = "{\n" +
        "    \"run_type\": \"${run_type}\",\n" +
        "    \"local_addr\": \"0.0.0.0\",\n" +
        "    \"local_port\": 2062,\n" +
        "    \"remote_addr\": \"${remote_addr}\",\n" +
        "    \"remote_port\": ${remote_port},\n" +
        "    \"udp_timeout\" : ${udp_timeout}, \n" +
        "    \"udp_socket_buf\": ${udp_socket_buf},\n" +
        "    \"udp_recv_buf\": ${udp_recv_buf},\n" +
        "    \"password\": [\n" +
        "        \"${password}\"\n" +
        "    ],\n" +
        "    \"log_level\": ${log_level},\n" +
        "    \"ssl\": {\n" +
        "        \"verify\": ${ssl.verify},\n" +
        "        \"verify_hostname\": ${ssl.verify_hostname},\n" +
        "        \"cert\": \"${ssl.cert}\",\n" +
        "        \"cipher\": \"ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-CHACHA20-POLY1305:ECDHE-RSA-CHACHA20-POLY1305:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384:ECDHE-ECDSA-AES256-SHA:ECDHE-ECDSA-AES128-SHA:ECDHE-RSA-AES128-SHA:ECDHE-RSA-AES256-SHA:DHE-RSA-AES128-SHA:DHE-RSA-AES256-SHA:AES128-SHA:AES256-SHA:DES-CBC3-SHA\",\n" +
        "        \"cipher_tls13\": \"TLS_AES_128_GCM_SHA256:TLS_CHACHA20_POLY1305_SHA256:TLS_AES_256_GCM_SHA384\",\n" +
        "        \"sni\": \"${ssl.sni}\",\n" +
        "        \"alpn\": [\n" +
        "            \"h2\",\n" +
        "            \"http/1.1\"\n" +
        "        ],\n" +
        "        \"reuse_session\": true,\n" +
        "        \"ssl_shutdown_wait_time\": ${ssl.ssl_shutdown_wait_time},\n" +
        "        \"session_ticket\": false,\n" +
        "        \"curves\": \"\"\n" +
        "    },\n" +
        "    \"tcp\": {\n" +
        "        \"no_delay\": true,\n" +
        "        \"keep_alive\": true,\n" +
        "        \"reuse_port\": true,\n" +
        "        \"fast_open\": ${tcp.fast_open},\n" +
        "        \"fast_open_qlen\": 20,\n" +
        "        \"connect_time_out\": ${tcp.connect_time_out}\n" +
        "    },\n" +
        "    \"experimental\": {\n" +
        "        \"pipeline_num\": ${experimental.pipeline_num},\n" +
        "        \"pipeline_timeout\": ${experimental.pipeline_timeout},\n" +
        "        \"pipeline_ack_window\":  ${experimental.pipeline_ack_window},\n" +
        "        \"pipeline_loadbalance_configs\": [\n" +
        "            ${experimental.pipeline_loadbalance_configs}\n" +
        "        ]" +
        "    },\n" +
        "    \"tun\" : {\n" +
        "        \"tun_name\" : \"tun0\",\n" +
        "        \"net_ip\" : \"${tun.net_ip}\",\n" +
        "        \"net_mask\" : \"255.255.255.0\",\n" +
        "        \"mtu\" : ${tun.mtu},\n" +
        "        \"tun_fd\" : ${tun.tun_fd}\n" +
        "    },\n" +
        "    \"dns\": {\n" +
        "        \"enabled\": true,\n" +
        "        \"enable_cached\": true,\n" +
        "        \"port\": 53,\n" +
        "        \"udp_timeout\": 5,\n" +
        "        \"udp_socket_buf\": ${dns.udp_socket_buf},\n" +
        "        \"udp_recv_buf\": ${dns.udp_recv_buf},\n" +
        "        \"up_dns_server\": [\n" +
        "            \"${dns.up_dns_server}\"\n" +
        "        ],\n" +
        "        \"up_gfw_dns_server\": [\n" +
        "            \"${dns.up_gfw_dns_server}\"\n" +
        "        ],\n" +
        "        \"gfwlist\": \"${dns.gfwlist}\"\n" +
        "    },\n" +
        "    \"route\": {\n" +
        "        \"enabled\": true,\n" +
        "        \"proxy_type\": ${route.proxy_type},\n" +
        "        \"cn_mainland_ips_file\": \"${route.cn_mainland_ips_file}\",\n" +
        "        \"proxy_ips\": \"${route.proxy_ips}\",\n" +
        "        \"white_ips\": \"${route.white_ips}\"\n" +
        "    }\n" +
        "}\n";


        public string PrepareConfig(HostsViewModel hosts, bool isLoadBalancePrepare = false)
        {
            string config = ConfigTemplate;

            string appVersion = VersionTracking.CurrentVersion;

            string version_path = Path.Combine(App.Instance.DataPathParent, "version");
            string gfwlist_path = Path.Combine(App.Instance.DataPathParent, "gfw_list");
            string cn_ips_path = Path.Combine(App.Instance.DataPathParent, "cn_ips_list");
            string cert_path = Path.Combine(App.Instance.DataPathParent, "cacert");

            if (!File.Exists(version_path) || File.ReadAllText(version_path) != appVersion)
            {
                File.WriteAllText(version_path, appVersion);

                File.WriteAllText(gfwlist_path, Resx.TextResource.gfwlist);
                File.WriteAllText(cn_ips_path, Resx.TextResource.cn_mainland_ips);
                File.WriteAllText(cert_path, Resx.TextResource.cacert);
            }

            config = config.Replace("${run_type}", "client_tun");
            config = config.Replace("${remote_addr}", HostAddress);
            config = config.Replace("${remote_port}", HostPort.ToString());
            config = config.Replace("${password}", Password);

            config = config.Replace("${udp_timeout}", "10");
            config = config.Replace("${udp_socket_buf}", "16384");
            config = config.Replace("${udp_recv_buf}", "4096");

            config = config.Replace("${log_level}", EnableDebugLog ? "0" : "5");

            config = config.Replace("${ssl.verify}", SSLVerify.ToLowerString());
            config = config.Replace("${ssl.verify_hostname}", SSLVerify.ToLowerString());
            config = config.Replace("${ssl.ssl_shutdown_wait_time}", "3");
            config = config.Replace("${ssl.sni}", HostAddress);
            config = config.Replace("${ssl.cert}", cert_path);

            config = config.Replace("${tcp.fast_open}", EnableTCPFastOpen ? "true" : "false");
            config = config.Replace("${tcp.connect_time_out}", "5");

            config = config.Replace("${experimental.pipeline_num}", EnablePipeline ? "5" : "0");
            config = config.Replace("${experimental.pipeline_timeout}", "60");
            config = config.Replace("${experimental.pipeline_ack_window}", "100");

            if (!isLoadBalancePrepare && LoadBalance.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < LoadBalance.Count; i++)
                {
                    var h = hosts.FindHostByName(LoadBalance[i]);
                    if (h != null && h.EnablePipeline)
                    {
                        string loadConfig = h.PrepareConfig(hosts, true);
                        string path = Path.Combine(App.Instance.DataPathParent, "balance_config" + i);
                        File.WriteAllText(path, loadConfig);

                        if (sb.Length > 0)
                        {
                            sb.Append(",");
                        }

                        sb.Append($"\"{path}\"");
                    }
                }

                config = config.Replace("${experimental.pipeline_loadbalance_configs}", sb.ToString());
            }
            else
            {
                config = config.Replace("${experimental.pipeline_loadbalance_configs}", string.Empty);
                if (isLoadBalancePrepare)
                {
                    config = config.Replace("${tun.tun_fd}", "-1");
                }
            }

            config = config.Replace("${tun.net_ip}", TunNetIP);
            config = config.Replace("${tun.mtu}", TunMtu.ToString());

            config = config.Replace("${dns.udp_socket_buf}", "8192");
            config = config.Replace("${dns.udp_recv_buf}", "1024");

            config = config.Replace("${dns.up_dns_server}", UpStreamNS);
            config = config.Replace("${dns.up_gfw_dns_server}", GFWUpStreamNS);

            config = config.Replace("${dns.gfwlist}", gfwlist_path);
            config = config.Replace("${route.cn_mainland_ips_file}", cn_ips_path);
            config = config.Replace("${route.proxy_type}", ((int)Route).ToString());

            string proxy_ips_path = Path.Combine(App.Instance.DataPathParent, "proxy_ips");
            string white_ips_path = Path.Combine(App.Instance.DataPathParent, "white_ips");
            File.WriteAllText(proxy_ips_path, string.Empty);
            File.WriteAllText(white_ips_path, string.Empty);

            config = config.Replace("${route.proxy_ips}", proxy_ips_path);
            config = config.Replace("${route.white_ips}", white_ips_path);

            return config;
        }
    }
}