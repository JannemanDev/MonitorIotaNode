using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorIotaNode
{
    public class Settings
    {
        [JsonProperty(Required = Required.Always)]
        public CheckInterval StatusCheckInterval { get; set; }

        [JsonProperty(Required = Required.Always)]
        public CheckInterval SyncCheckInterval { get; set; }

        [JsonProperty(Required = Required.Always)]
        public List<IotaNode> IotaNodes { get; set; }

        [JsonProperty(Required = Required.Always)]
        public PushOver PushOver { get; set; }

        [JsonIgnore]
        public List<string> IncludedDeviceNames => PushOver
                .Devices
                .Where(device => device.Include)
                .Select(device => device.Name).ToList();

        [JsonIgnore]
        public List<IotaNode> IncludedNodesForStatusCheck => IotaNodes.Where(node => node.IncludeForStatusCheck).ToList();

        [JsonIgnore]
        public List<IotaNode> IncludedNodesForSyncCheck => IotaNodes.Where(node => node.IncludeForSyncCheck).ToList();

        public static Settings LoadSettings(string filename)
        {
            string settingsJson = File.ReadAllText(filename);

            return JsonConvert.DeserializeObject<Settings>(settingsJson);
        }

        public static void SaveSettings(string filename, Settings settings)
        {
            string settingsJson = JsonConvert.SerializeObject(settings, Formatting.Indented);

            File.WriteAllText(filename, settingsJson);
        }

        public static string DefaultSettingsFile(string path = "")
        {
            path = Path.Combine(path, " ").TrimEnd();
            return @$"{path}settings.json";
        }

        public static Settings DefaultSettings()
        {
            Settings settings = new Settings();
            settings.StatusCheckInterval = new CheckInterval
            {
                IntervalInMinutes = 24 * 60, //every day
                Enabled = true
            };
            settings.SyncCheckInterval = new CheckInterval
            {
                IntervalInMinutes = 1, //every minute
                Enabled = true
            };
            settings.IotaNodes = new List<IotaNode>() { new IotaNode() { DashboardUrl = new Uri("http://localhost:8081/dashboard"), InfoUrl = new Uri("http://localhost:8080/info"), IncludeForStatusCheck = true, IncludeForSyncCheck = true } };
            settings.PushOver = new PushOver() { Enabled=true, ApiKey = "Fill me in, login into https://pushover.net/", UserKey = "Fill me in, login into https://pushover.net/", Devices = new List<Device>() { new Device() { Name = "Fill me in, login into https://pushover.net/", Include = true } } };
            return settings;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }

    public class CheckInterval
    {
        private int intervalInMinutes;

        [JsonProperty(Required = Required.Always)]
        public int IntervalInMinutes
        {
            get => intervalInMinutes; 
            set
            {
                if (value < 1) throw new FormatException("IntervalInMinutes must be >=1");
                intervalInMinutes = value;
            }
        }

        [JsonProperty(Required = Required.Always)]
        public bool Enabled { get; set; }
    }

    public class IotaNode
    {
        [JsonProperty(Required = Required.Default)] //optional
        public Uri DashboardUrl { get; set; }

        [JsonProperty(Required = Required.Always)]
        public Uri InfoUrl { get; set; }

        [JsonProperty(Required = Required.Always)]
        public bool IncludeForStatusCheck { get; set; }

        [JsonProperty(Required = Required.Always)]
        public bool IncludeForSyncCheck { get; set; }
    }

    public class PushOver
    {
        [JsonProperty(Required = Required.Always)]
        public bool Enabled { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string ApiKey { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string UserKey { get; set; }

        [JsonProperty(Required = Required.Always)]
        public List<Device> Devices { get; set; }
    }

    public class Device
    {
        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }

        [JsonProperty(Required = Required.Always)]
        public bool Include { get; set; }

        public override string ToString()
        {
            return $"Device name {Name}";
        }
    }
}
