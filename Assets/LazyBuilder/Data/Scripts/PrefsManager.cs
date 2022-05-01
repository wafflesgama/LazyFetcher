using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace LazyBuilder
{
    public class Preferences
    {
        public int TempBufferSize { get; set; }

    }
    public class PrefsManager
    {
        public static Preferences preferences;

        private const string FILE_NAME = "General";


        public static void SavePreferences()
        {
            throw new UnityException("Needs adjust path");
            // Convert the instance ('this') of this class to a JSON string with "pretty print" (nice indenting).
            string json = JsonUtility.ToJson(preferences, true);

            // Write that JSON string to the specified file.
            File.WriteAllText($"{Application.dataPath}/LazyBuilder/Data/Preferences/{FILE_NAME}.json", json);
        }
    }
}
