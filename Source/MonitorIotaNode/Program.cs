using System;
using System.Collections.Generic;
using System.IO;
using Serilog;
using Serilog.Events;
using System.Linq;
using RestSharp;
using System.Threading;
using System.Net;

namespace MonitorIotaNode
{
    //Todo: if node goes offline do handle exceptions!
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

            //Todo: temporary solution for error: "The SSL connection could not be established, see inner exception."
            //       when using RestSharp.
            //      Postman generates a warning: "Unable to verify the first certificate"
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, certificate, chain, sslPolicyErrors) => true;

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
                    if (cki.Key == ConsoleKey.Escape)
                    {
                        ClearTimers();
                        Environment.Exit(0);
                    }
                    if (cki.Key == ConsoleKey.L) ReloadSettings();
                }
            }
        }

        private static void ReloadSettings()
        {
            Log.Logger.Information($"Reloading settings");

            Settings prevSettings = settings;
            settings = Settings.LoadSettings(settingsFile);

            //only copy settings when available
            if (prevSettings != null)
            {
                //copy previous nodes states from previous settings to new settings if node still exist in new settings
                for (int i = 0; i < settings.IotaNodes.Count; i++)
                {
                    IotaNode currentIotaNode = settings.IotaNodes[i];
                    IotaNode previousIotaNode = prevSettings.IotaNodes.SingleOrDefault(node => node.InfoUrl == currentIotaNode.InfoUrl);
                    if (previousIotaNode != null)
                    {
                        Log.Logger.Debug($"Found same node {previousIotaNode.InfoUrl} from previous settings in the nodes from the new settings. Copying state: PrevStateNodeDown:{previousIotaNode.PrevStateNodeDown} PrevStateSynced:{previousIotaNode.PrevStateSynced}");
                        currentIotaNode.PrevStateNodeDown = previousIotaNode.PrevStateNodeDown;
                        currentIotaNode.PrevStateSynced = previousIotaNode.PrevStateSynced;
                    }
                }

                //keep previous nodes which are not in new settings and set include to false
                List<IotaNode> previousNodesToKeep = prevSettings.IotaNodes
                    .Where(prevNode => !settings.IotaNodes.Exists(node => node.InfoUrl == prevNode.InfoUrl))
                    .ToList();

                if (previousNodesToKeep.Count > 0)
                {
                    Log.Logger.Debug($"Keeping {previousNodesToKeep.Count} old nodes which are not in the new settings");

                    previousNodesToKeep.ForEach(node =>
                    {
                        node.IncludeForStatusCheck = false;
                        node.IncludeForSyncCheck = false;
                    });

                    settings.IotaNodes.AddRange(previousNodesToKeep);
                }
                else Log.Logger.Debug($"No nodes from previous settings needed to keep");
            }

            SetTimers();
        }

        private static void ClearTimers()
        {
            Log.Logger.Debug($"Deleting previous timer(s)");
            //clear timers because enabled and interval settings can be changed
            timerStatusCheck?.Dispose();
            timerSyncCheck?.Dispose();
        }

        private static void SetTimers()
        {
            ClearTimers();

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
                NodeInfo nodeInfo = null;
                bool success = true;
                try
                {
                    //node can be down so catch exception
                    nodeInfo = nodeEndpoint.RetrieveNodeInfo();
                }
                catch (Exception)
                {
                    success = false;
                    Log.Logger.Error($"Error retrieving node info from {iotaNode.InfoUrl}");
                }

                if (success)
                {
                    NodeInfo previousNodeInfo;
                    bool previousNodeInfoAvailable = previousNodeInfos.TryGetValue(iotaNode.InfoUrl, out previousNodeInfo);

                    string title = $"Status of Node {nodeInfo.IdentityIdShort}";
                    string message = $"Version {nodeInfo.Version}\nNetworkVersion {nodeInfo.NetworkVersion}\n" +
                                     $"{nodeInfo.TangleTime}\n" +
                                     $"Messages (solid/total): {nodeInfo.SolidMessageCount}/{nodeInfo.TotalMessageCount}\n";

                    if (previousNodeInfoAvailable)
                    {
                        Mana deltaMana = nodeInfo.Mana - previousNodeInfo.Mana;
                        //Access mana delta
                        string deltaManaAccessPercentageAsStr = "n/a";
                        if (previousNodeInfo.Mana.Access != 0)
                        {
                            decimal deltaManaAccessPercentage = (deltaMana.Access / previousNodeInfo.Mana.Access) * 100;
                            deltaManaAccessPercentageAsStr = $"{deltaManaAccessPercentage:0.##}%";
                        }

                        //Consensus mana delta
                        string deltaManaConsensusPercentageAsStr = "n/a";
                        if (previousNodeInfo.Mana.Consensus != 0)
                        {
                            decimal deltaManaConsensusPercentage = (deltaMana.Consensus / previousNodeInfo.Mana.Consensus) * 100;
                            deltaManaConsensusPercentageAsStr = $"{deltaManaConsensusPercentage:0.##}%";
                        }

                        string deltaManaMessage = $"Change in Access Mana={deltaMana.Access:0}({deltaManaAccessPercentageAsStr})\nChange in Consensus Mana={deltaMana.Consensus:0}({deltaManaConsensusPercentageAsStr})\n";

                        //Total Messages Count delta
                        int deltaTotalMessageCount = nodeInfo.TotalMessageCount - previousNodeInfo.TotalMessageCount;
                        string deltaTotalMessageCountPercentageAsStr = "n/a";
                        if (previousNodeInfo.TotalMessageCount > 0)
                        {
                            decimal deltaTotalMessageCountPercentage = ((decimal)deltaTotalMessageCount / previousNodeInfo.TotalMessageCount) * 100;
                            deltaTotalMessageCountPercentageAsStr = $"{deltaTotalMessageCountPercentage:0.###}%";
                        }

                        string deltaTotalMessageCountMessage = $"Change in Total Message Count={deltaTotalMessageCount}({deltaTotalMessageCountPercentageAsStr})\n";

                        message += $"{deltaTotalMessageCountMessage}" +
                                         $"{nodeInfo.Mana}\n" +
                                         $"{deltaManaMessage}\n";

                        previousNodeInfos[iotaNode.InfoUrl] = nodeInfo; //does exist so update
                    }
                    else
                    {
                        message += $"{nodeInfo.Mana}\n";

                        previousNodeInfos.Add(iotaNode.InfoUrl, nodeInfo); //is new so add
                    }

                    message += $"Info: {iotaNode.InfoUrl.AbsoluteUri}";

                    if (iotaNode.DashboardUrl?.AbsoluteUri.Length > 0) message += $"\nDashboard: {iotaNode.DashboardUrl.AbsoluteUri}";

                    if (iotaNode.PrevStateNodeDown)
                    {
                        iotaNode.PrevStateNodeDown = false; //reset because now node is back online
                        message = $"Node is back online!\n" + message;
                    }

                    Log.Logger.Information($"Sending a notification:\n{title}\n{message}");

                    SendNotifications(settings.PushOver.ApiKey, settings.PushOver.UserKey, settings.IncludedDeviceNames, title, message);

                }
                else //node is down
                {
                    if (!iotaNode.PrevStateNodeDown) //only sent notification when node first offline
                    {
                        SendNotificationWhenNodeIsDown(iotaNode);
                    }
                    iotaNode.PrevStateNodeDown = true;
                }
            }
        }

        private static void SendNotificationWhenNodeIsDown(IotaNode iotaNode)
        {
            string title = $"Status: Node not reachable";
            string message = $"Info: {iotaNode.InfoUrl.AbsoluteUri}";

            if (iotaNode.DashboardUrl?.AbsoluteUri.Length > 0) message += $"\nDashboard: {iotaNode.DashboardUrl.AbsoluteUri}";

            Log.Logger.Information($"Sending a notification:\n{title}\n{message}");

            SendNotifications(settings.PushOver.ApiKey, settings.PushOver.UserKey, settings.IncludedDeviceNames, title, message);
        }

        private static void CheckSync()
        {
            foreach (IotaNode iotaNode in settings.IncludedNodesForSyncCheck)
            {
                NodeEndpoint nodeEndpoint = new NodeEndpoint(iotaNode.InfoUrl);
                NodeInfo nodeInfo = null;
                bool success = true;
                try
                {
                    //node can be down so catch exception
                    nodeInfo = nodeEndpoint.RetrieveNodeInfo();
                }
                catch (Exception)
                {
                    success = false;
                    Log.Logger.Error($"Error retrieving node info from {iotaNode.InfoUrl}");
                }

                if (success)
                {
                    string title;
                    string message;

                    message = $"Version {nodeInfo.Version}\nNetworkVersion {nodeInfo.NetworkVersion}\n" +
                                     $"{nodeInfo.TangleTime}\n" +
                                     $"Messages (solid/total): {nodeInfo.SolidMessageCount}/{nodeInfo.TotalMessageCount}\n" +
                                     $"{nodeInfo.Mana}\n" +
                                     $"Info: {iotaNode.InfoUrl.AbsoluteUri}";

                    if (iotaNode.DashboardUrl?.AbsoluteUri.Length > 0) message += $"\nDashboard: {iotaNode.DashboardUrl.AbsoluteUri}";

                    if (!nodeInfo.TangleTime.Synced) //out of sync...
                    {
                        if (iotaNode.PrevStateSynced) //...and not out of sync last time (to avoid sending repeatedly)
                        {
                            iotaNode.PrevStateSynced = false;

                            if (iotaNode.PrevStateNodeDown) title = $"Node {nodeInfo.IdentityIdShort} back online but NOT synced!";
                            else title = $"Node {nodeInfo.IdentityIdShort} NOT synced!";

                            Log.Logger.Information($"Sending a notification:\n{title}\n{message}");

                            SendNotifications(settings.PushOver.ApiKey, settings.PushOver.UserKey, settings.IncludedDeviceNames, title, message);
                        }
                        else
                        {
                            //still unsynced so do not send again notification
                        }
                    }
                    else //in sync...
                    {
                        if (!iotaNode.PrevStateSynced) //...but last time not in sync
                        {
                            iotaNode.PrevStateSynced = true;
                            if (iotaNode.PrevStateNodeDown) title = $"Node {nodeInfo.IdentityIdShort} back online and synced again!";
                            else title = $"Node {nodeInfo.IdentityIdShort} synced again!";

                            Log.Logger.Information($"Sending a notification:\n{title}\n{message}");

                            SendNotifications(settings.PushOver.ApiKey, settings.PushOver.UserKey, settings.IncludedDeviceNames, title, message);
                        }
                        else //...and also previously or last time checked in sync
                        {
                            if (iotaNode.PrevStateNodeDown) //..node is currently up but was it down previous?
                            {
                                title = $"Node {nodeInfo.IdentityIdShort} back online and still synced!";

                                Log.Logger.Information($"Sending a notification:\n{title}\n{message}");

                                SendNotifications(settings.PushOver.ApiKey, settings.PushOver.UserKey, settings.IncludedDeviceNames, title, message);
                            }
                            else
                            {
                                //don't send notification since it's still reachable and in sync
                            }
                        }
                    }

                    iotaNode.PrevStateNodeDown = false; //reset because now node is (back) online (although could be unsynced)

                }
                else //node is down
                {
                    if (!iotaNode.PrevStateNodeDown) //only sent notification when node first offline
                    {
                        SendNotificationWhenNodeIsDown(iotaNode);
                    }
                    iotaNode.PrevStateNodeDown = true;
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
                catch (Exception e)
                {
                    Log.Logger.Error($"Error sending notification: {e.Message}{Environment.NewLine}{e.InnerException.Message}");
                }
                finally
                {
                    if (response != null && !response.IsSuccessful)
                    {
                        Log.Logger.Error($"Error sending notification: {response.ErrorMessage}{Environment.NewLine}{response.ErrorException.InnerException.Message}");
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
