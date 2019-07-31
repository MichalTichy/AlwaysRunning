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
    }
}