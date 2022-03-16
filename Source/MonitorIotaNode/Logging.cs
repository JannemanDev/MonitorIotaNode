using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorIotaNode
{
    public class Logging
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public LogEventLevel MinimumLevel { get; set; }
        public FileLogging File { get; set; }
        public ConsoleLogging Console { get; set; }
    }

    public class FileLogging
    {
        public bool Enabled { get; set; }
        public string Path { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public RollingInterval RollingInterval { get; set; }
        public bool RollOnFileSizeLimit { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public LogEventLevel RestrictedToMinimumLevel { get; set; }
    }

    public class ConsoleLogging
    {
        public bool Enabled { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public LogEventLevel RestrictedToMinimumLevel { get; set; }
    }
}
