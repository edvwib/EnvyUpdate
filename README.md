![Logo](https://github.com/fyr77/envyupdate/blob/master/res/banner.png?raw=true)

# EnvyUpdate
 A small portable update checker application for Nvidia GPUs
 
 ![License](https://img.shields.io/github/license/fyr77/envyupdate?style=for-the-badge)
 ![Issues](https://img.shields.io/github/issues/fyr77/envyupdate?style=for-the-badge)
 ![Version](https://img.shields.io/github/v/release/fyr77/envyupdate?style=for-the-badge)
 
## How to use

 Download the [latest release](https://github.com/fyr77/EnvyUpdate/releases/latest/download/EnvyUpdate.exe) (or as a [.zip](https://github.com/fyr77/EnvyUpdate/releases/latest/download/EnvyUpdate.zip)) and run it. Windows SmartScreen Messages can be safely ignored. They only happen because this project is not digitally signed.

## Compatibility

The application should be compatible with all Nvidia GPUs that have their drivers available on the nvidia.com download page and runs on Windows 7 and up.

It is tested with GeForce Series GPUs. Generally others should work, but they are (currently) untested.

Studio drivers are currently unsupported.

## Development

This application is currently maintained and developed by me (fyr77) alone in my free time. 

I always try to implement critical fixes as fast as I can, but other features and minor bug fixes may take a few days or weeks to implement. 

If you want to help me develop EnvyUpdate, you can start by creating issues with your bug reports and/or feature requests. Pull requests are also welcome, especially regarding translations.

## Other interesting tools

* [TinyNvidiaUpdateChecker](https://github.com/ElPumpo/TinyNvidiaUpdateChecker) - a command line update checker and installer. Inspired EnvyUpdate to begin with.
* [Disable-Nvidia-Telemtry](https://github.com/NateShoffner/Disable-Nvidia-Telemetry) - does pretty much what the name says. It disables Nvidia Telemetry.
* [NVCleanInstall](https://www.techpowerup.com/nvcleanstall/) - a closed-source application by TechPowerUp which does quite a lot of cool things.

EnvyUpdate is not a replacement for any of these tools. I will still try to implement as many features in EnvyUpdate as possible while keeping the simple interface and as little settings as possible.

## Licenses

* This project: [MIT](https://github.com/fyr77/EnvyUpdate/blob/master/LICENSE)
* Fody (depency of Costura.Fody): [MIT](https://github.com/Fody/Fody/blob/master/License.txt)
* Costura.Fody (for embedding DLLs into the main executable): [MIT](https://github.com/Fody/Costura/blob/develop/LICENSE)
* wpf-notifyicon (for showing an icon in the system tray): [CPOL](https://github.com/hardcodet/wpf-notifyicon/blob/master/LICENSE)
* Notifications.Wpf: [MIT](https://github.com/Federerer/Notifications.Wpf/blob/master/LICENSE)
* Resource Embedder: [MIT](https://github.com/MarcStan/resource-embedder/blob/master/LICENSE)
* Icon made by Freepik from www.flaticon.com