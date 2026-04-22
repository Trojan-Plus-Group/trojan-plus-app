chmod +x gfwlist2dnsmasq.sh
./gfwlist2dnsmasq.sh -l -o gfwlist.txt
curl https://raw.githubusercontent.com/mayaxcn/china-ip-list/refs/heads/master/chnroute.txt -o cn_mainland_ips.txt
