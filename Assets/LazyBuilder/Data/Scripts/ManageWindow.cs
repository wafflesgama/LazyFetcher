using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LazyBuilder
{
    public class ManageWindow : EditorWindow
    {
        private VisualElement _root;

        private VisualElement _list;

        private TextElement _serverPath;

        private VisualTreeAsset _itemTemplate;
        private StyleSheet _itemStyleTemplate;

        private DropdownField _pools;

        private Button _addBtn;
        private Button _revertBtn;
        private Button _saveBtn;
        private Button _autoFillBtn;

        #region Unity Functions

        private async void OnEnable()
        {
            MainController.Init(this);
            await MainController.FetchLazyData();

            SetupBaseUI();
            SetupBindings();
            SetupCallbacks();

            SetupPools();

            _serverPath.text = MainController.GetSrcPath();

            _pools.value = MainController.lazyData.Pools[0].Id.Capitalize();

            _addBtn.SetEnabled(false);
            _revertBtn.SetEnabled(false);
            _saveBtn.SetEnabled(false);
            //_pools.set
            //SetupItems();
        }

        #endregion Unity Functions



        private void SetupBaseUI()
        {
            //Shader.PropertyToID
            _root = rootVisualElement;
            // root.styleSheets.Add(Resources.Load<StyleSheet>("qtStyles"));

            // Loads and clones our VisualTree (eg. our UXML structure) inside the root.
            var quickToolVisualTree = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath($"{MainController.relativePath}/{MainController.UI_PATH}/ManagerLayout.uxml", typeof(VisualTreeAsset));
            //var quickToolVisualTree = Resources.Load<VisualTreeAsset>("MainLayout");
            quickToolVisualTree.CloneTree(_root);
        }

        private void SetupBindings()
        {
            //_list = (ListView)_root.Q("List");
            _list = _root.Q("List");
            _serverPath = (TextElement)_root.Q("Path");
            _pools = (DropdownField)_root.Q("Pools");
            _autoFillBtn = (Button)_root.Q("Autofill");
            _addBtn = (Button)_root.Q("Add");
            _saveBtn = (Button)_root.Q("Save");
            _revertBtn = (Button)_root.Q("Revert");
        }

        private void SetupCallbacks()
        {
            _pools.RegisterValueChangedCallback(SetupItems);
        }
        private void SetupPools()
        {
            _pools.choices = MainController.lazyData.Pools.Select(x => x.Id.Capitalize()).ToList();
        }
        private void SetupItems(ChangeEvent<string> pool)
        {
            _list.Clear();
            var poolIndex = _pools.choices.IndexOf(pool.newValue);
            for (int i = 0; i < 5; i++)
            {

                foreach (var item in MainController.lazyData.Pools[poolIndex].Items)
                {

                    if (_itemTemplate == null)
                        _itemTemplate = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath($"{MainController.relativePath}/{MainController.UI_PATH}/ManagerItemLayout.uxml", typeof(VisualTreeAsset));

                    if (_itemStyleTemplate == null)
                        _itemStyleTemplate = (StyleSheet)AssetDatabase.LoadAssetAtPath($"{MainController.relativePath}/{MainController.UI_PATH}/ManagerItemStyle.uss", typeof(StyleSheet));

                    var element = _itemTemplate.CloneTree();
                    element.styleSheets.Add(_itemStyleTemplate);

                    var status = element.Q("Status");
                    status.style.backgroundColor = Color.red;

                    var id = (TextElement)element.Q("Id");
                    id.text = item.Id.Capitalize();

                    var types = (TextElement)element.Q("Types");
                    types.text = item.TypeIds.Count.ToString();

                    var tags = (TextElement)element.Q("Tags");
                    tags.text = item.Tags.Count.ToString();

                    _list.Add(element);

                }
            }
        }


    }
}
