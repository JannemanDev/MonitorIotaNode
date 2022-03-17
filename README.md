# Monitor IOTA Node
Cross platform .NET 6 util, which is very easy to use and requires no external dependencies, that regularly monitors one or more GoShimmer nodes and send push notifications to one or more devices when it goes unsynced. It uses the great paid Push notification service [PushOver](https://pushover.net/) (only $5 one-time purchase per platform).

Tested on Ubuntu 20.10, 21.10 and 22.04 and Windows 11

On Ubuntu you can get this error:
```console
Process terminated. Couldn't find a valid ICU package installed on the system. Please install libicu using your package manager and try again. Alternatively you can set the configuration flag System.Globa
lization.Invariant to true if you want to run with no globalization support. Please see https://aka.ms/dotnet-missing-libicu for more information. 
```

Use this shell script as solution:
```console
#!/bin/bash
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
./MonitorIotaNode
```

# Features
-Monitor one or more nodes  
-Configurable time interval for status checking and sync checking  
-Get Push notification when a node goes unsynced  
-Get Push notification about the status of a node  
-Status and/or Sync checking can be set per node  
-Auto reload `settings.json` when it's changed (currently only supported on Windows)  
-Logging to a file and console  
-Push notification to one or more devices (Android, iPhone, iPad, and Desktop (Android Wear and Apple Watch, too!)  
-Everything is configurable and optional, see `settings.json` (after first time run)  

<img src="https://user-images.githubusercontent.com/13236774/139660066-e8650529-42a3-442e-9dc6-79de4ddda25c.PNG" alt="push notification" width="250"/>

# Installation
1. Pick the correct build (For `Synology DS720+ NAS` use `linux-x64-net6.0`, also for `Ubuntu` use `linux-x64-net6.0`)
2. Run `MonitorIotaNode` will create an empty `settings.json` file with default values. Review it and fill in the placeholder texts. You can find the Pushover `UserKey` when you login on Pushover. For the `ApiKey` you first have to [Create an Application/API Token](https://pushover.net/apps/build).
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
  },
  "Logging": {
        "MinimumLevel": "Verbose",
        "File": {
            "Enabled": true,
            "Path": "Logs/log.txt",
            "RollingInterval": "Day",
            "RollOnFileSizeLimit": true,
            "RestrictedToMinimumLevel": "Verbose"
        },
        "Console": {
            "Enabled": true,
            "RestrictedToMinimumLevel": "Information"
        }
    }  
}

```
# Current limitations
-On Unix/Linux machines auto reload when settings are changed doesn't work because of a limitation of `FileSystemWatcher`.  

# Next release / roadmap / todo
- finding a workaround for Unix/Linux limitation of `FileSystemWatcher`  
- beside push notification also support sending a notification email  
- seperate (un)sync event log  
- add extra stats in status message like nr of sync losses, min/max/avg time to resync 
- ...

# Donation

I appreciate any donation: `iota1qrl7m2y7cn829lwx47tvxpzrelfk5amvxphyretdd2n5spxctpsfzmqe8pv`
