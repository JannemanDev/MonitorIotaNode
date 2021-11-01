using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Events;
using System.Linq;
using RestSharp;
using System.Threading;

namespace MonitorIotaNode
{
    class Program
    {
        public static Settings settings;
        private static string settingsFile;
        private static Timer timerStatusCheck = null;
        private static Timer timerSyncCheck = null;

        private static Dictionary<Uri, NodeInfo> previousNodeInfos = new Dictionary<Uri, NodeInfo>();

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose() //send all events to sinks
                .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Debug) //Todo: be configurable
                .WriteTo.File("Logs/log.txt", //Todo: be configurable
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    restrictedToMinimumLevel: LogEventLevel.Verbose) //Todo: be configurable
                .CreateLogger();

            Log.Logger.Information(("Monitor IOTA Nodes v0.1\n"));
            Log.Logger.Information(" <Escape> to quit");
            Log.Logger.Information(" <L> force reload of settings");

            settingsFile = ParseArguments(args);

            if (!File.Exists(settingsFile))
            {
                Log.Logger.Error($"Settingsfile {settingsFile} not found!"); ;
                Log.Logger.Information($"Creating new one... Please review it and fill in the blanks!"); ;
                Settings.SaveSettings(settingsFile, Settings.DefaultSettings());
                Environment.Exit(1);
            }

            //settingsFile = $"c:\temp\settings.json"; //override for testing purposes

            Log.Logger.Information($"Loading settings from {settingsFile}\n");

            ReloadSettings();
            SettingsWatcher settingsWatcher = new SettingsWatcher(settingsFile, SettingsChanged);

            while (true)
            {
                Thread.Sleep(1000);

                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo cki = Console.ReadKey(true);
                    if (cki.Key == ConsoleKey.Escape) Environment.Exit(0);
                    if (cki.Key == ConsoleKey.L) ReloadSettings();
                }
            }
        }

        private static void ReloadSettings()
        {
            Log.Logger.Information($"Reloading settings");
            settings = Settings.LoadSettings(settingsFile);
            SetTimers();
        }

        private static void SetTimers()
        {
            Log.Logger.Debug($"Deleting previous timer(s)");
            //clear timers because enabled and interval settings can be changed
            timerStatusCheck?.Dispose();
            timerSyncCheck?.Dispose();

            if (settings.StatusCheckInterval.Enabled)
            {
                timerStatusCheck = new Timer(TimerStatusCheck_Interval, null, 0, settings.StatusCheckInterval.IntervalInMinutes * 60 * 1000);
                Log.Logger.Information($"Setting Status Check timer to {settings.StatusCheckInterval.IntervalInMinutes} minute(s)");

            }
            if (settings.SyncCheckInterval.Enabled)
            {
                timerSyncCheck = new Timer(TimerSyncCheck_Interval, null, 0, settings.SyncCheckInterval.IntervalInMinutes * 60 * 1000);
                Log.Logger.Information($"Setting Sync Check timer to {settings.SyncCheckInterval.IntervalInMinutes} minute(s)");

            }
        }

        private static void CheckStatus()
        {
            foreach (IotaNode iotaNode in settings.IncludedNodesForStatusCheck)
            {
                NodeEndpoint nodeEndpoint = new NodeEndpoint(iotaNode.InfoUrl);
                NodeInfo nodeInfo = nodeEndpoint.RetrieveNodeInfo();

                NodeInfo previousNodeInfo;
                bool previousAvailable = previousNodeInfos.TryGetValue(iotaNode.InfoUrl, out previousNodeInfo);

                string deltaManaMessage = "";
                string deltaTotalMessageCountMessage = "";
                if (previousAvailable)
                {
                    Mana deltaMana = nodeInfo.Mana - previousNodeInfo.Mana;
                    Mana deltaManaPercentage = (deltaMana / previousNodeInfo.Mana) * 100;

                    deltaManaMessage = $"Change in Access Mana={deltaMana.Access:0}({deltaManaPercentage.Access:0.##}%)\nChange in Consensus Mana={deltaMana.Consensus:0}({deltaManaPercentage.Consensus:0.##}%)\n";
                    Log.Logger.Information(deltaManaMessage);

                    int deltaTotalMessageCount = nodeInfo.TotalMessageCount - previousNodeInfo.TotalMessageCount;
                    decimal deltaTotalMessageCountPercentage = ((decimal)deltaTotalMessageCount / previousNodeInfo.TotalMessageCount) * 100;
                    deltaTotalMessageCountMessage = $"Change in Total Message Count={deltaTotalMessageCount}({deltaTotalMessageCountPercentage:0.###}%)\n";
                    Log.Logger.Information(deltaTotalMessageCountMessage);


                    previousNodeInfos[iotaNode.InfoUrl] = nodeInfo; //does exist so update
                }
                else previousNodeInfos.Add(iotaNode.InfoUrl, nodeInfo); //is new so add

                string title = $"Status of Node {nodeInfo.IdentityIdShort}";
                string message = $"Version {nodeInfo.Version}\nNetworkVersion {nodeInfo.NetworkVersion}\n" +
                                 $"{nodeInfo.TangleTime}\n" +
                                 $"Messages (solid/total): {nodeInfo.SolidMessageCount}/{nodeInfo.TotalMessageCount}\n" +
                                 $"{deltaTotalMessageCountMessage}" +
                                 $"{nodeInfo.Mana}\n" +
                                 $"{deltaManaMessage}" +
                                 $"Info: {iotaNode.InfoUrl.AbsoluteUri}";

                if (iotaNode.DashboardUrl?.AbsoluteUri.Length > 0) message += $"\nDashboard: {iotaNode.DashboardUrl.AbsoluteUri}";

                Log.Logger.Information($"Sending a notification:\n{title}\n{message}");

                SendNotifications(settings.PushOver.ApiKey, settings.PushOver.UserKey, settings.IncludedDeviceNames, title, message);
            }
        }

        private static void CheckSync()
        {
            foreach (IotaNode iotaNode in settings.IncludedNodesForSyncCheck)
            {
                NodeEndpoint nodeEndpoint = new NodeEndpoint(iotaNode.InfoUrl);
                NodeInfo nodeInfo = nodeEndpoint.RetrieveNodeInfo();

                if (!nodeInfo.TangleTime.Synced)
                {
                    string title = $"Node {nodeInfo.IdentityIdShort} NOT synced!";
                    string message = $"Version {nodeInfo.Version}\nNetworkVersion {nodeInfo.NetworkVersion}\n" +
                                     $"{nodeInfo.TangleTime}\n" +
                                     $"Messages (solid/total): {nodeInfo.SolidMessageCount}/{nodeInfo.TotalMessageCount}\n" +
                                     $"{nodeInfo.Mana}\n" +
                                     $"Info: {iotaNode.InfoUrl.AbsoluteUri}";

                    if (iotaNode.DashboardUrl?.AbsoluteUri.Length > 0) message += $"\nDashboard: {iotaNode.DashboardUrl.AbsoluteUri}";

                    Log.Logger.Information($"Sending a notification:\n{title}\n{message}");

                    SendNotifications(settings.PushOver.ApiKey, settings.PushOver.UserKey, settings.IncludedDeviceNames, title, message);
                }
            }
        }

        private static void TimerStatusCheck_Interval(Object o)
        {
            Log.Logger.Debug("In TimerStatusCheck_Interval: " + DateTime.Now);
            CheckStatus();
        }

        private static void TimerSyncCheck_Interval(Object o)
        {
            Log.Logger.Debug("In TimerSyncCheck_Interval: " + DateTime.Now);
            CheckSync();
        }

        private static void SettingsChanged(SettingsWatcher settingsWatcher)
        {
            Log.Logger.Information($"Settings in {settingsWatcher.Filename} changed!");
            ReloadSettings();
            Log.Logger.Information($"New settings are:\n{settings}");
        }

        private static string ParseArguments(string[] args)
        {
            if (args.Length > 1)
            {
                Log.Logger.Fatal("Error in arguments!");
                Log.Logger.Information("Syntax: <program> [settingsfile]");
                Environment.Exit(1);
            }

            string settingsFile;
            if (args.Length == 0)
            {
                string currentFolder = Directory.GetCurrentDirectory();
                settingsFile = Settings.DefaultSettingsFile(currentFolder);
            }
            else
            {
                settingsFile = args[0];
            }

            return settingsFile;
        }

        static bool SendNotifications(string token, string user, List<string> devices, string title, string message)
        {
            if (!settings.PushOver.Enabled) return true;

            bool sendAllSucceeded = true;

            foreach (var device in devices)
            {
                var client = new RestClient($"https://api.pushover.net/1/messages.json?token={token}&user={user}&device={device}&title={title}&message={message}");
                var request = new RestRequest(Method.POST);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                IRestResponse response = null;
                try
                {
                    response = client.Execute(request);
                }
                finally
                {
                    if (response != null && !response.IsSuccessful)
                    {
                        Log.Logger.Error(response.ErrorMessage);
                        sendAllSucceeded = false;
                    }
                }
            }
            return sendAllSucceeded;
        }

        public static long SecondsSinceEpoch(DateTime utcDateTime)
        {
            TimeSpan t = utcDateTime - new DateTime(1970, 1, 1);
            long secondsSinceEpoch = (long)t.TotalSeconds;
            return secondsSinceEpoch;
        }

        public static long SecondsSinceEpoch()
        {
            return SecondsSinceEpoch(DateTime.UtcNow);
        }
    }
}
