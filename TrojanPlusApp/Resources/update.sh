curl https://raw.githubusercontent.com/cokebar/gfwlist2dnsmasq/master/gfwlist2dnsmasq.sh -o gfwlist2dnsmasq.sh
chmod +x gfwlist2dnsmasq.sh
./gfwlist2dnsmasq.sh -l -o gfwlist.txt
curl https://raw.githubusercontent.com/17mon/china_ip_list/master/china_ip_list.txt -o cn_mainland_ips.txt
