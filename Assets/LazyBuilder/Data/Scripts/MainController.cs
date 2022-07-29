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
        public static ServerData lazyData { get; set; }
        public static SceneView sceneWindow { get; private set; }
        public static string absolutePath { get; set; }
        public static string relativePath { get; set; }


        public const string TEMP_ITEMS_PATH = "LazyBuilder/Data/Temp/Items";
        public const string STORED_ITEMS_PATH = "LazyBuilder/Data/Items";
        public const string UI_PATH = "LazyBuilder/Data/UI";
        public const string MESHES_PATH = "LazyBuilder/Data/Meshes";
        public const string MATERIALS_PATH = "LazyBuilder/Data/Materials";



        public static void Init(ScriptableObject scriptSource)
        {
            fetcher = new Fetcher_Local($"C:\\Projects\\LazyBuilder\\LazyBuilderData");

            absolutePath = Path.GetDirectoryName(Application.dataPath);
            var currentPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(scriptSource)));
            relativePath = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(currentPath)));
        }

        [MenuItem("Tools/Lazy Builder/Build #b")]
        private static void BuildWindow()
        {
            GetSceneWindow();
            var window = GetWindow<BuilderWindow>();
            window.titleContent = new GUIContent("Lazy Build");
            window.Show();

        }
        [MenuItem("Tools/Lazy Builder/Screenshot test")]
        private static async void TakeScreen()
        {
            IconFactory.MarkTextureNonReadable = false;
            IconFactory.OrthographicMode = true;

            var t = GameObject.Find("Test");
            var currentPath = Application.dataPath;
            //var currentPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject()));
            var tex = await IconFactory.GenerateModelPreviewFromPath($"Assets/model.fbx",512,512);
            //var tex = IconFactory.GenerateModelPreview(t.transform,512,512);
            var bytes = tex.EncodeToPNG();
            File.WriteAllBytes(currentPath + "/test.png", bytes);
            Debug.Log(currentPath);

        }


        [MenuItem("Tools/Lazy Builder/Lazy Data Manager #r")]
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
            var rawData = await fetcher.GetRawString("", "main", "json");
            //Debug.Log(rawData);
            lazyData = JsonConvert.DeserializeObject<ServerData>(rawData);

        }


        public static async Task<string> GetRawFile(string src, string savePath, string fileName, string fileType)
        {
            return await fetcher.GetRawFile(src, savePath, fileName, fileType);
        }

        public static async Task<Texture2D> GetImage(string src, string imgName, string imgType)
        {
            return await fetcher.GetImage(src, imgName, imgType);
        }

        public static string GetSrcPath() => fetcher.GetSrcPath();












    }
}