using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace LazyBuilder
{
    public class ServerData
    {
        [JsonProperty("Id")]
        public string Id { get; set; }

        [JsonProperty("Items")]
        public List<Item> Items { get; set; }
    }

    public class Item
    {
        [JsonProperty("Id")]
        public string Id { get; set; }

        [JsonProperty("TypeIds")]
        public List<string> TypeIds { get; set; }

        [JsonProperty("Tags")]
        public List<string> Tags { get; set; }
    }
}
