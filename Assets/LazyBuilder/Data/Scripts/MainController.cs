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

        public static SceneView sceneWindow { get; private set; }


        public static void Init(ScriptableObject scriptSource)
        {
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

        //[MenuItem("Tools/Lazy Builder/Test #t")]
        //private static void Test()
        //{
          
        //}

        private static void GetSceneWindow()
        {
            if (sceneWindow != null) return;

            sceneWindow = GetWindow<SceneView>();
        }

    }
}