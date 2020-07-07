using System;
using System.ComponentModel;
using System.IO;
using TrojanPlusApp.Models;
using TrojanPlusApp.ViewModels;
using Xamarin.Forms;

namespace TrojanPlusApp.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class HostsPage : ContentPage
    {
        private readonly HostsViewModel viewModel;

        public HostsPage()
        {
            InitializeComponent();
            BindingContext = viewModel = new HostsViewModel();
        }

        public void OnItemSelected(object sender, EventArgs args)
        {
            var layout = (BindableObject)sender;
            var item = (HostModel)layout.BindingContext;
            viewModel.SelectedHostItem(item.HostName);
        }

        public async void OnItemEdit(object sender, EventArgs args)
        {
            var layout = (BindableObject)sender;
            var item = (HostModel)layout.BindingContext;
            await Navigation.PushModalAsync(new NavigationPage(new HostEditPage(viewModel, item)));
        }

        public async void AddItem_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new NavigationPage(new HostEditPage(viewModel, null)));
        }

        private static readonly string ConfigTemplate = "{\n" +
        "    \"run_type\": \"client_tun\",\n" +
        "    \"local_addr\": \"0.0.0.0\",\n" +
        "    \"local_port\": 2062,\n" +
        "    \"remote_addr\": \"10.254.240.104\",\n" +
        "    \"remote_port\": 4650,\n" +
        "    \"target_addr\": \"114.114.114.114\",\n" +
        "    \"target_port\": 53,\n" +
        "    \"udp_timeout\" : 10, \n" +
        "    \"password\": [\n" +
        "        \"88888888\"\n" +
        "    ],\n" +
        "    \"log_level\": ${output_logcat},\n" +
        "    \"ssl\": {\n" +
        "        \"verify\": false,\n" +
        "        \"verify_hostname\": false,\n" +
        "        \"cert\": \"\",\n" +
        "        \"cipher\": \"ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-CHACHA20-POLY1305:ECDHE-RSA-CHACHA20-POLY1305:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384:ECDHE-ECDSA-AES256-SHA:ECDHE-ECDSA-AES128-SHA:ECDHE-RSA-AES128-SHA:ECDHE-RSA-AES256-SHA:DHE-RSA-AES128-SHA:DHE-RSA-AES256-SHA:AES128-SHA:AES256-SHA:DES-CBC3-SHA\",\n" +
        "        \"cipher_tls13\": \"TLS_AES_128_GCM_SHA256:TLS_CHACHA20_POLY1305_SHA256:TLS_AES_256_GCM_SHA384\",\n" +
        "        \"sni\": \"\",\n" +
        "        \"alpn\": [\n" +
        "            \"h2\",\n" +
        "            \"http/1.1\"\n" +
        "        ],\n" +
        "        \"reuse_session\": true,\n" +
        "        \"session_ticket\": false,\n" +
        "        \"curves\": \"\"\n" +
        "    },\n" +
        "    \"tcp\": {\n" +
        "        \"no_delay\": true,\n" +
        "        \"keep_alive\": true,\n" +
        "        \"reuse_port\": true,\n" +
        "        \"fast_open\": false,\n" +
        "        \"fast_open_qlen\": 20,\n" +
        "        \"connect_time_out\": 1\n" +
        "    },\n" +
        "    \"experimental\": {\n" +
        "        \"pipeline_num\": ${pipeline},\n" +
        "        \"pipeline_ack_window\": 200\n" +
        "    },\n" +
        "    \"tun\" : {\n" +
        "        \"tun_name\" : \"tun0\",\n" +
        "        \"net_ip\" : \"10.0.0.2\",\n" +
        "        \"net_mask\" : \"255.255.255.0\",\n" +
        "        \"mtu\" : 1500,\n" +
        "        \"tun_fd\" : ${fd}\n" +
        "    }\n" +
        "}\n";

        public void OnConnectBtnClicked(object sender, EventArgs e)
        {
            // TODO generate config file
            if (viewModel.CurretSelectHost == null)
            {
                // TODO popup a dialog to warning
                // return;
            }

            var config = ConfigTemplate.Replace("${pipeline}", "0");
            config = config.Replace("${output_logcat}", "5");

            try
            {
                File.WriteAllText(App.Instance.ConfigPath, config);
                App.Instance.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}