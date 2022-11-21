using UnityEditor;
using UnityEngine;

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
            if (Application.isPlaying) return;
            GetSceneWindow();
            var window = GetWindow<BuilderWindow>();
            window.titleContent = new GUIContent("Lazy Builder");
            window.Show();
        }



        [MenuItem("Tools/Lazy Builder/Server Manager #m")]
        private static void ManageWindow()
        {
            if (Application.isPlaying) return;

            GetSceneWindow();
            var window = GetWindow<ManageWindow>();
            window.titleContent = new GUIContent("Lazy Server Manager");
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