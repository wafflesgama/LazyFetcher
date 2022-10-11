using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using static LazyBuilder.ServerManager;

namespace LazyBuilder
{
    public class BuilderPreferences
    {
        [JsonProperty("LastServer")]
        public string LastServer { get; set; }

        [JsonProperty("LastItem")]
        public string LastItem { get; set; }

        [JsonProperty("LastItemType")]
        public string LastItemType { get; set; }

        [JsonProperty("ServersType")]
        public List<ServerType> Servers_Type { get; set; }

        [JsonProperty("ServersId")]
        public List<string> Servers_Id { get; set; }

        [JsonProperty("ServersSrc")]
        public List<string> Servers_src { get; set; }

        [JsonProperty("ServersBranch")]
        public List<string> Servers_branch { get; set; }

        [JsonProperty("PropCol")]
        public bool Prop_Col { get; set; }

        [JsonProperty("PropRb")]
        public bool Prop_Rb { get; set; }

        [JsonProperty("PageSize")]
        public int PageSize { get; set; }

    }

    public class ManagerPreferences
    {
        [JsonProperty("LastServer")]
        public string LastServerSrc { get; set; }
    }

    //public class Test
    //{
    //    void teste()
    //    {
    //        Preferences a = new ManagerPreferences();
    //        PrefsManager.LoadPreference(ref a);
    //    }
    //}
    public class PreferenceManager
    {

        public static T LoadPreference<T>(string fileName)
        {

            var filePath = $"{PathFactory.absoluteToolPath}\\{PathFactory.PREFS_PATH}\\{fileName}.json";

            if (!File.Exists(filePath))
                File.Create(filePath).Close();

            var data = File.ReadAllText(filePath);

            return JsonConvert.DeserializeObject<T>(data);
        }

        public static void SavePreference(string fileName, object pref)
        {
            // Convert class data to a JSON string with pretty print
            var json = JsonConvert.SerializeObject(pref, Formatting.Indented);

            // Write that JSON string to the specified file.
            var filePath = $"{PathFactory.absoluteToolPath}\\{PathFactory.PREFS_PATH.AbsoluteFormat()}\\{fileName}.json";
            File.WriteAllText(filePath, json);

        }
    }
}
