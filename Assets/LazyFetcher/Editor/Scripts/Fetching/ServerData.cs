using System;
using System.Collections.Generic;

namespace LazyFetcher
{
    [Serializable]
    public class ServerData
    {
        public string Id;

        public ServerVersion Version;

        public List<Item> Items;
    }

    [Serializable]
    public class Item
    {
        public string Id;

        public List<string> TypeIds;

        public List<string> Tags;
    }
}
