using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LazyBuilder
{

    public class MainController : EditorWindow
    {

        public static ServerData lazyData { get; set; }
        public static SceneView sceneWindow { get; private set; }
        public static string appPath { get; set; }
        public static string basePath { get; set; }


        public const string TEMP_ITEMS_PATH = "LazyBuilder/Data/Temp/Items";
        public const string STORED_ITEMS_PATH = "LazyBuilder/Data/Items";
        public const string UI_PATH = "LazyBuilder/Data/UI";
        public const string MESHES_PATH = "LazyBuilder/Data/Meshes";
        public const string MATERIALS_PATH = "LazyBuilder/Data/Materials";


        [MenuItem("Tools/Lazy Builder/Build #b")]
        private static void BuildWindow()
        {
            GetSceneWindow();
            var window = GetWindow<BuilderWindow>();
            window.titleContent = new GUIContent("Lazy Build");
            window.Show();

        }

        [MenuItem("Tools/Lazy Builder/Lazy Data Manager")]
        private static void ManageWindow()
        {
            GetSceneWindow();
            var window = GetWindow<ManageWindow>();
            window.titleContent = new GUIContent("Lazy Darta Manager");
            window.Show();
        }

        private static void GetSceneWindow()
        {
            if (sceneWindow != null) return;

            sceneWindow = GetWindow<SceneView>();
        }

        public static async Task FetchLazyData()
        {
            var rawData = await Fetcher.GitWebRawString("", "/", "main", "json");
            //Debug.Log(rawData);
            lazyData = JsonConvert.DeserializeObject<ServerData>(rawData);



        }













    }
}