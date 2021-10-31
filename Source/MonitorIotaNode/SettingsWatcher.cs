using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorIotaNode
{
    public class SettingsWatcher
    {
        public DateTime LastChange { get; private set; }
        public string Filename { get; private set; }

        private const int minimumFileAgeInSeconds = 5;
        private readonly FileSystemWatcher watcher;
        private Action<SettingsWatcher> eventHandler;

        public SettingsWatcher(string filename, Action<SettingsWatcher> eventHandler)
        {
            Filename = filename;
            LastChange = DateTime.MinValue;
            this.eventHandler = eventHandler;

            if (File.Exists(filename))
            {
                watcher = new FileSystemWatcher(Path.GetDirectoryName(filename), Path.GetFileName(filename));
                watcher.EnableRaisingEvents = true;
                watcher.Changed += Watcher_Changed;
            }
            else throw new ArgumentException($"File {filename} does not exist!");
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if ((DateTime.Now - LastChange).Seconds > minimumFileAgeInSeconds && !IsFileLocked(e.FullPath)) //Avoid code executing multiple times  
            {
                Log.Logger.Information($"Settings in {watcher.Filter} changed!");
                LastChange = DateTime.Now;

                if (eventHandler != null) eventHandler(this);
            }
        }

        private bool IsFileLocked(string filePath)
        {
            FileStream stream = null;
            try
            {
                stream = File.OpenRead(filePath);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
            return false;
        }
    }
}
