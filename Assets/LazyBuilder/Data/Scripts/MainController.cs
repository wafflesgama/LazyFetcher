using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace LazyBuilder
{

    public class MainController : EditorWindow
    {

        public static Fetcher fetcher;
        public static ServerData lazyData { get; private set; }
        public static SceneView sceneWindow { get; private set; }


        public static void Init(ScriptableObject scriptSource)
        {
            fetcher = new Fetcher_Local($"C:\\Projects\\LazyBuilder\\LazyBuilderData");
            PathFactory.Init(scriptSource);
        }


        [MenuItem("Tools/Lazy Builder/Build #b")]
        private static void BuildWindow()
        {
            GetSceneWindow();
            var window = GetWindow<BuilderWindow>();
            window.titleContent = new GUIContent("Lazy Builder");
            window.Show();

        }



        [MenuItem("Tools/Lazy Builder/Lazy Data Manager #r")]
        private static void ManageWindow()
        {
            GetSceneWindow();
            var window = GetWindow<ManageWindow>();
            window.titleContent = new GUIContent("Lazy Data Manager");
            window.Show();
        }

        private static void GetSceneWindow()
        {
            if (sceneWindow != null) return;

            sceneWindow = GetWindow<SceneView>();
        }



        public static async Task<bool> FetchServerData()
        {
            try
            {
                var rawData = await fetcher.GetRawString("", "main", "json");

                if (rawData == null) return false;

                //Debug.Log(rawData);
                lazyData = JsonConvert.DeserializeObject<ServerData>(rawData);
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }


        public static async Task<string> GetRawFile(string src, string savePath, string fileName, string fileType)
        {
            return await fetcher.GetRawFile(src, savePath, fileName, fileType);
        }

        public static async Task<Texture2D> GetImage(string src, string imgName, string imgType)
        {
            return await fetcher.GetImage(src, imgName, imgType);
        }

        public static string GetServerPath() => fetcher.GetSrcPath();












    }
}