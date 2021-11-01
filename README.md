# Monitor IOTA Node
Cross platform .NET 5 util, which is very easy to use and requires no external dependencies, that regularly monitors one or more GoShimmer nodes and send push notifications to one or more devices when it goes unsynced. It uses the great paid Push notification service [PushOver](https://pushover.net/) (only $5 one-time purchase per platform).

# Features
-Monitor one or more nodes  
-Configurable time interval for status checking and sync checking  
-Get Push notification when a node goes unsynced  
-Get Push notification about the status of a node  
-Status and/or Sync checking can be set per node  
-Auto reload settings.json when it's changed (currently only supported on Windows)  
-Logging to a file and console  
-Push notification to one or more devices (Android, iPhone, iPad, and Desktop (Android Wear and Apple Watch, too!)  
-Everything is configurable and optional, see `settings.json` (after first time run)  

<img src="https://user-images.githubusercontent.com/13236774/139660066-e8650529-42a3-442e-9dc6-79de4ddda25c.PNG" alt="push notification" width="250"/>

# Installation
1. Pick the correct build (For `Synology DS720+ NAS` use `linux-x64-net5.0` and ignore the `no version information available` warning, for `Ubuntu` use `linux-x64-net5.0`)
2. Run `MonitorIotaNode` will create an empty `settings.json` file with default values. Review it and fill in the placeholder texts.
3. Run again

# Settings

```json
{
  "StatusCheckInterval": {
    "IntervalInMinutes": 1440,
    "Enabled": true
  },
  "SyncCheckInterval": {
    "IntervalInMinutes": 1,
    "Enabled": true
  },
  "IotaNodes": [
    {
      "DashboardUrl": "http://localhost:8081/dashboard",
      "InfoUrl": "http://localhost:8080/info",
      "IncludeForStatusCheck": true,
      "IncludeForSyncCheck": true
    }
  ],
  "PushOver": {
    "Enabled": true,
    "ApiKey": "...",

    "UserKey": "...",
    "Devices": [
      {
        "Name": "iPhoneXJan",
        "Include": true
      }
    ]
  }
}

```
# Current limitations
On Unix/Linux machines auto reload when settings are changed doesn't work because of a limitation of `FileSystemWatcher`.

# Next release / roadmap / todo
- finding a workaround for Unix/Linux limitation of `FileSystemWatcher`  
- beside push notification also support sending a notification email  
- adding configurable SeriLog settings in `settings.json`
- ...
