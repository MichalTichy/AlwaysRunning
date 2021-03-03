using System;
using System.Configuration;

namespace AlwaysRunning
{

    public class AppInfo : ConfigurationElement
    {
        [ConfigurationProperty("Path", DefaultValue = null, IsRequired = true, IsKey = true)]
        public string Path
        {
            get { return (string)this["Path"]; }
            set { this["Path"] = value; }
        }

        [ConfigurationProperty("WorkSpace", DefaultValue = null, IsRequired = false, IsKey = false)]
        public string WorkSpace
        {
            get { return (string)this["WorkSpace"]; }
            set { this["WorkSpace"] = value; }
        }

        [ConfigurationProperty("RestartIntervalInMinutes", DefaultValue = null, IsRequired = false, IsKey = false)]
        public int? TimeBetweenRestartsInMinutes
        {
            get { return (int?)this["RestartIntervalInMinutes"]; }
            set { this["RestartIntervalInMinutes"] = value; }
        }

        [ConfigurationProperty("RestartVarianceInMinutes", DefaultValue = null, IsRequired = false, IsKey = false)]
        public int? RestartVarianceInMinutes
        {
            get { return (int?)this["RestartVarianceInMinutes"]; }
            set { this["RestartVarianceInMinutes"] = value; }
        }

        internal DateTime? NextRestart { get; set; }
        internal int ProcessId { get; set; }
    }
}