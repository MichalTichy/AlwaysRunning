using System.Configuration;

namespace AlwaysRunning
{
    public class AppInfoCollection : ConfigurationElementCollection
    {
        public AppInfoCollection()
        {
        }

        public AppInfo this[int index]
        {
            get { return (AppInfo)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(AppInfo serviceConfig)
        {
            BaseAdd(serviceConfig);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new AppInfo();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((AppInfo)element).Path;
        }

        public void Remove(AppInfo serviceConfig)
        {
            BaseRemove(serviceConfig.Path);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Remove(string name)
        {
            BaseRemove(name);
        }
    }
}