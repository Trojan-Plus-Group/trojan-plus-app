# Trojan Plus App

![](https://raw.githubusercontent.com/wiki/Trojan-Plus-Group/trojan-plus//trojan_plus_logo.png)

## Introduction

This is a project helping you bypass some firewalls in your mobile devices, it just supports [Trojan Plus](https://github.com/Trojan-Plus-Group/trojan-plus) and [original Trojan](https://github.com/trojan-gfw/trojan), we don't have any plan to support other protocols such as Shadowsocks, V2ray and so on. Of cause this client app also need  a server host to running Trojan Plus or Original Trojan's program as server type to receive client's connections.

According we designing, all Trojan Plus mobile client app should be in same UI with same user experience, that's why this project exists. Beside the core library [Trojan Plus](https://github.com/Trojan-Plus-Group/trojan-plus), we also use [Xamarin Framework](https://dotnet.microsoft.com/apps/xamarin) to build this app including Android, iOS, UWP (Windows). 

We don't have a user manual to guide you, because all operations are simple with note and familiar for [Shadowsocks Android](https://github.com/shadowsocks/shadowsocks-android)'s users. But if you have any good idea, suggestion or need some help, please let us know by [report a issue](https://github.com/Trojan-Plus-Group/trojan-plus-app/issues).

## Why do we need a mobile client?

In one hand, [original Trojan](https://github.com/trojan-gfw/trojan) provides a mobile (Android) client [igniter](https://github.com/trojan-gfw/igniter), it uses GoLang to process data from Android native system, so... not effective, we wrote many basic function by C/C++ process data to get 2 times speed than igniter. 

In the other hand, [Trojan Plus](https://github.com/Trojan-Plus-Group/trojan-plus) provides a lot of experimental function to increase speed and bandwidth but no mobile app, so we wrote this.

**Performance comparing test is welcome!**

## Android

You can [download](https://github.com/Trojan-Plus-Group/trojan-plus-app/releases/tag/0.0.1) the **beta version** APK file to help us to improve this app. If you has any issue, please let us know by this project's [issue reporting page](https://github.com/Trojan-Plus-Group/trojan-plus-app/issues).

Here's a known "issue", becuase users can select route rules in this app same as other Shadowsocks' client so that all network data of whole device's app have to be transferred to this app from native system.

So in Android settings battery options page, you can see **Trojan Plus consumed a lot of battery** after runing for a while if you used a lot of wireless network data in a short time (watching a streaming video).

Maybe, it's not a issue, because each mobile proxy app used for bypass some firewalls have to do this. They are also battery killer.

## iOS && UWP

*To be done.*


## License 

All codes are under protection of [GPLv3](https://github.com/Trojan-Plus-Group/trojan-plus-app/blob/master/LICENSE) which is same as [Trojan Plus](https://github.com/Trojan-Plus-Group/trojan-plus) and [original Trojan](https://github.com/trojan-gfw/trojan).

## Dependencies
* [Trojan Plus Open Source Project](https://github.com/Trojan-Plus-Group/trojan-plus)
* [Xamarin.Forms](https://xamarin.com)
* [China Mainland IP list](https://github.com/17mon/china_ip_list)
* [GFW Domain List](https://github.com/cokebar/gfwlist2dnsmasq)


