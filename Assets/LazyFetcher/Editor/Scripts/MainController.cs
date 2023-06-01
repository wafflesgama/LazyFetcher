using UnityEditor;
using UnityEngine;

namespace LazyFetcher
{

    public class MainController : EditorWindow
    {

        public static SceneView sceneWindow { get; private set; }


        public static void Init(ScriptableObject scriptSource)
        {
            PathFactory.Init(scriptSource);
        }


        [MenuItem("Tools/Lazy Fetcher/Fetch &f")]
        private static void FetchWindow()
        {
            if (Application.isPlaying) return;
            GetSceneWindow();
            var window = GetWindow<FetcherWindow>();
            window.titleContent = new GUIContent("Lazy Fetcher");
            window.Show();
        }



        [MenuItem("Tools/Lazy Fetcher/Manage &m")]
        private static void ManageWindow()
        {
            if (Application.isPlaying) return;

            GetSceneWindow();
            var window = GetWindow<ManagerWindow>();
            window.titleContent = new GUIContent("Lazy Server Manager");
            window.Show();
        }


        private static void GetSceneWindow()
        {
            if (sceneWindow != null) return;

            sceneWindow = GetWindow<SceneView>();
        }

    }
}