using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using static LazyBuilder.ServerManager;
using System;

namespace LazyBuilder
{
    [Serializable]
    public class BuilderPreferences
    {

        public string LastServer;
        public string LastItem;
        public string LastItemType;
        public List<ServerType> Servers_Type;
        public List<string> ServersId;
        public List<string> ServersSrc;
        public List<string> ServersBranch;
        public bool PropCol;
        public bool PropRb;
        public int PageSize;

        public void SetDefaultValues()
        {
            LastServer = "";
            LastItem = "";
            LastItemType = "";

            ServersId = new List<string>();
            ServersId.Add("Main");
            Servers_Type = new List<ServerType>();
            Servers_Type.Add(ServerType.GIT);
            ServersSrc = new List<string>();
            ServersSrc.Add(Server_Git.defaultRepo);
            ServersBranch = new List<string>();
            ServersBranch.Add(Server_Git.defaultBranch);

            PropCol = false;
            PropRb = false;

            PageSize = 50;
        }
    }

    [Serializable]
    public class ManagerPreferences
    {
        public string LastServer;
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

            return JsonUtility.FromJson<T>(data);
        }

        public static void SavePreference(string fileName, object pref)
        {
            // Convert class data to a JSON string with pretty print
            var json = JsonUtility.ToJson(pref, prettyPrint:true);

            // Write that JSON string to the specified file.
            var filePath = $"{PathFactory.absoluteToolPath}\\{PathFactory.PREFS_PATH.AbsoluteFormat()}\\{fileName}.json";
            File.WriteAllText(filePath, json);

        }

        public static void ResetPreference(string filename)
        {

        }
    }
}
