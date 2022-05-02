using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LazyBuilder
{
    public class ManageWindow : EditorWindow
    {

        private VisualElement _root;


        #region Unity Functions

        private void OnEnable()
        {
            SetupBaseUI();
        }

        #endregion Unity Functions



        private void SetupBaseUI()
        {
            _root = rootVisualElement;
            // root.styleSheets.Add(Resources.Load<StyleSheet>("qtStyles"));

            // Loads and clones our VisualTree (eg. our UXML structure) inside the root.
            var quickToolVisualTree = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath($"{MainController.basePath}/{MainController.UI_PATH}/ManagerLayout.uxml", typeof(VisualTreeAsset));
            //var quickToolVisualTree = Resources.Load<VisualTreeAsset>("MainLayout");
            quickToolVisualTree.CloneTree(_root);
        }

    }
}
