using System.Configuration;

namespace AlwaysRunning
{
    public class AppInfoConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("Apps", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(AppInfoCollection),
            AddItemName = "add",
            ClearItemsName = "clear",
            RemoveItemName = "remove")]
        public AppInfoCollection Apps
        {
            get
            {
                return (AppInfoCollection)base["Apps"];
            }
        }
    }
}