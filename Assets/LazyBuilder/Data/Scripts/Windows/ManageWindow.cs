using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LazyBuilder
{
    public class ManageWindow : EditorWindow
    {
        private ManagerPreferences preferences;
        private ServerData cachedData;

        private Item selectedItem;

        private VisualElement _root;

        private VisualElement _list;

        private TextField _serverPath;

        private VisualTreeAsset _itemTemplate;
        private StyleSheet _itemStyleTemplate;


        private Button _loadBtn;
        private Button _addBtn;
        private Button _revertBtn;
        private Button _saveBtn;
        private Button _autoFillBtn;
        private Button _openFolderBtn;
        private Button _singleThumbBtn;


        private TextElement _id;
        private VisualElement _thumbnail;
        private VisualElement _descriptionContainer;
        private TextElement _description;
        private VisualElement _tags;
        private VisualElement _types;

        private string loadedPath;

        const string NOENTRY_MSG = "This item has no entry in the JSON file";
        const string NODATA_MSG = "This JSON entry has no data";
        const string NOTAGS_MSG = "This item does not have any tags";
        const string NOTYPES_MSG = "This item does not have any types";
        const string NOMATCHTYPES_MSG = "The item types do not match the JSON entry";
        const string OK_MSG = "All fields are correct";

        #region Unity Functions

        private async void OnEnable()
        {
            MainController.Init(this);

            InitPreferences();

            SetupBaseUI();
            SetupBindings();
            SetupCallbacks();
            SetupInputCallbacks();

            if (preferences.LastServerSrc != null)
            {

                ServerManager.SetServer(ServerManager.CreateServer(ServerManager.ServerType.LOCAL, preferences.LastServerSrc, null));
                await ServerManager.FetchServerData();
                ReadCachedData();

                loadedPath = ServerManager.server.GetFullPath();
                _serverPath.value = loadedPath;
            }

            SetupItems();

            _addBtn.SetEnabled(false);
            _revertBtn.SetEnabled(false);
            _saveBtn.SetEnabled(false);
        }

        #endregion Unity Functions



        private void SetupBaseUI()
        {
            _root = rootVisualElement;

            // Loads and clones our VisualTree (eg. our UXML structure) inside the root.
            var quickToolVisualTree = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath(PathFactory.BuildUiFilePath(PathFactory.MANAGER_LAYOUT_FILE), typeof(VisualTreeAsset));
            quickToolVisualTree.CloneTree(_root);

        }

        private void SetupBindings()
        {
            //_list = (ListView)_root.Q("List");
            _list = _root.Q("List");
            _serverPath = (TextField)_root.Q("Path");

            _loadBtn = (Button)_root.Q("Load");
            _autoFillBtn = (Button)_root.Q("Autofill");
            _addBtn = (Button)_root.Q("Add");
            _saveBtn = (Button)_root.Q("Save");
            _revertBtn = (Button)_root.Q("Revert");
            _revertBtn = (Button)_root.Q("Revert");
            _openFolderBtn = (Button)_root.Q("OpenFolder");
            _singleThumbBtn = (Button)_root.Q("SingleThumb");

            _id = (TextElement)_root.Q("Id");
            _thumbnail = _root.Q("Thumbnail");
            _descriptionContainer = _root.Q("DescriptionContainer");
            _description = (TextElement)_root.Q("Description");
            _tags = _root.Q("Tags");
            _types = _root.Q("Types");
        }

        private void SetupCallbacks()
        {
            _loadBtn.RegisterCallback<ClickEvent>((x) => LoadNewServer());
            _openFolderBtn.RegisterCallback<ClickEvent>((x) => OpenFolder());
            _singleThumbBtn.RegisterCallback<ClickEvent>((x) => SingleThumbnail());
            _autoFillBtn.RegisterCallback<ClickEvent>((x) => AutoWriteData());
        }

        private void SetupItems()
        {
            _list.Clear();

            //Duplicating list
            var mergedItems = ServerManager.data.Items.ToList();
            mergedItems.AddRange(cachedData.Items);

            //Removing duplicate (same Id) Items between cached and Server Data & sorts alphab
            mergedItems = mergedItems.GroupBy(X => X.Id).Select(x => x.FirstOrDefault()).OrderBy(x => x.Id).ToList();


            foreach (var item in mergedItems)
            {
                SetupItem(item);
            }
        }

        private void SetupItem(Item item)
        {
            var itemState = CheckItemState(item);

            if (_itemTemplate == null)
                _itemTemplate = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath(PathFactory.BuildUiFilePath(PathFactory.MANAGER_ITEM_LAYOUT_FILE), typeof(VisualTreeAsset));

            if (_itemStyleTemplate == null)
                _itemStyleTemplate = (StyleSheet)AssetDatabase.LoadAssetAtPath(PathFactory.BuildUiFilePath(PathFactory.MANAGER_ITEM_LAYOUT_FILE, false), typeof(StyleSheet));

            VisualElement element = _itemTemplate.CloneTree();
            element.styleSheets.Add(_itemStyleTemplate);

            var status = element.Q("Status");


            Color color = itemState.Item1 switch
            {
                1 => Color.red,
                2 => Color.yellow,
                3 => Color.gray,
                _ => Color.green
            };

            status.style.backgroundColor = color;


            var id = (TextElement)element.Q("Id");
            id.text = item.Id.Capitalize();

            var types = (TextElement)element.Q("Types");
            types.text = item.TypeIds != null ? item.TypeIds.Count.ToString() : "---";


            var tags = (TextElement)element.Q("Tags");
            tags.text = item.Tags != null ? item.Tags.Count.ToString() : "---";

            (element).RegisterCallback<ClickEvent>((x) => OnItemCliked(item));

            _list.Add(element);
        }

        private async void OnItemCliked(Item item)
        {

            var itemState = CheckItemState(item);

            _id.text = item.Id.Capitalize();

            selectedItem = item;

            //Null savePath to not store image
            _thumbnail.style.backgroundImage = await ServerManager.server.GetImage(PathFactory.BuildItemPath(item.Id),null ,PathFactory.THUMBNAIL_FILE);

            _description.text = itemState.Item2;

            Color descColor = itemState.Item1 switch
            {
                1 => new Color(0.41f, .22f, .22f),
                2 => new Color(0.41f, .37f, .22f),
                3 => new Color(0.11f, .11f, .11f),
                _ => new Color(0.26f, .41f, .22f),
            };

            _descriptionContainer.style.backgroundColor = descColor;

            _types.Clear();
            if (item.TypeIds != null)
            {
                var cachedItem = cachedData.Items.Where(x => x.Id == item.Id).FirstOrDefault();

                foreach (var type in item.TypeIds)
                {
                    var tagElement = new TextElement();
                    tagElement.text = type;

                    if (cachedItem != null && !cachedItem.TypeIds.Contains(type))
                        tagElement.style.color = Color.red;

                    tagElement.style.marginBottom = 2;
                    _types.Add(tagElement);
                }

                if (itemState.Item1 == 2 && cachedItem != null)
                {
                    foreach (var type in cachedItem.TypeIds)
                    {
                        //If its already in the list - skip
                        if (item.TypeIds.Contains(type)) continue;

                        //if (_types.Children().Where(x => ((TextElement)x).text == type).Any()) continue;

                        var tagElement = new TextElement();
                        tagElement.text = type;
                        tagElement.style.color = Color.yellow;
                        tagElement.style.marginBottom = 2;
                        _types.Add(tagElement);
                    }
                }
            }


            _tags.Clear();
            if (item.Tags != null)
            {
                foreach (var tag in item.Tags)
                {
                    var tagElement = new TextElement();
                    tagElement.text = tag;
                    tagElement.style.marginBottom = 2;
                    _tags.Add(tagElement);
                }
            }


        }

        private void SetupInputCallbacks()
        {
            _root.RegisterCallback<KeyDownEvent>(OnKeyboardKeyDown, TrickleDown.TrickleDown);
        }

        private void OnKeyboardKeyDown(KeyDownEvent e)
        {
            Debug.Log($"Key down {e.keyCode}");

            if (e.keyCode == KeyCode.Escape)
                this.Close();
        }




        //Returning 0 OK, 1 Missing Server Entry, 2 Missing Server Sub-entries, 3 Missing Data of Entry 
        private (int, string) CheckItemState(Item item)
        {

            if (!ServerManager.data.Items.Contains(item))
                return (1, NOENTRY_MSG);

            var cachedItem = cachedData.Items.Where(x => x.Id == item.Id).FirstOrDefault();
            if (cachedItem == null)
                return (3, NODATA_MSG);

            if (item.Tags == null || item.Tags.Count == 0)
                return (2, NOTAGS_MSG);

            if (item.TypeIds == null || item.TypeIds.Count == 0)
                return (2, NOTYPES_MSG);



            if (item.TypeIds.Count != cachedItem.TypeIds.Count)
                return (2, NOMATCHTYPES_MSG);

            return (0, OK_MSG);
        }



        #region Button Clicks

        private async void LoadNewServer()
        {
            if (!Directory.Exists(_serverPath.value))
            {
                _serverPath.value = loadedPath;
                return;
            }

            ServerManager.SetServer(ServerManager.CreateServer(ServerManager.ServerType.LOCAL, _serverPath.value, null));
            var result = await ServerManager.FetchServerData();
            if (!result)
            {
                _serverPath.value = loadedPath;
                return;
            }

            ReadCachedData();

            loadedPath = _serverPath.value;

        }
        private void OpenFolder()
        {
            if (selectedItem == null) return;

            var path = $"{loadedPath}\\{PathFactory.DATA_FOLDER}\\{selectedItem.Id}";
            System.Diagnostics.Process.Start("explorer.exe", "/open, " + path);

        }

        private async void SingleThumbnail()
        {
            if (selectedItem == null) return;

            var cachedItem = cachedData.Items.Where(x => x.Id == selectedItem.Id).FirstOrDefault();

            if (cachedItem == null) return;

            var firstType = cachedItem.TypeIds.FirstOrDefault();

            if (firstType == null) return;

            await Utils.CreateThumbnailFromModelAsync($"{loadedPath}\\{PathFactory.DATA_FOLDER}\\{selectedItem.Id}", $"{selectedItem.Id}_{firstType}");

            //Refresh Item Info
            OnItemCliked(selectedItem);
        }

        #endregion Button Clicks

        #region Read Cached Folder Data

        private void ReadCachedData()
        {
            cachedData = new ServerData();



            cachedData.Id = ServerManager.data.Id;
            cachedData.Items = new List<Item>();

            var items = Directory.GetDirectories($"{loadedPath}/{PathFactory.DATA_FOLDER}");
            foreach (var itemNameFull in items)
            {
                var itemName = Path.GetFileName(itemNameFull);

                Item item = new Item();
                item.Id = itemName;
                item.TypeIds = new List<string>();

                // Adds Tags from correspoding server's Item
                var crspServerItem = ServerManager.data.Items.Where(x => x.Id == itemName).FirstOrDefault();
                item.Tags = crspServerItem == null ? new List<string>() : crspServerItem.Tags;

                var types = Directory.GetFiles($"{loadedPath}/{PathFactory.DATA_FOLDER}/{itemName}");
                foreach (var typeNameComplete in types)
                {
                    //Removing prefix ItemName in -> ItemName_TypeName.format
                    var typeName = Path.GetFileName(typeNameComplete);

                    if (typeName.StartsWith(PathFactory.THUMBNAIL_FILE)) continue;

                    if (!typeName.StartsWith(itemName))
                    {
                        Debug.LogError($"Error in file format in {itemName} - it should be item_type");
                        continue;
                    }

                    //Removing "_" in -> _TypeName.format
                    var typeNameTrimmed = typeName.Replace(itemName + "_", "");

                    //Removing .format in -> TypeName.format
                    typeNameTrimmed = typeNameTrimmed.Remove(typeNameTrimmed.IndexOf('.'));

                    item.TypeIds.Add(typeNameTrimmed);
                }
                cachedData.Items.Add(item);
            }

        }

        void AutoWriteData()
        {
            this.ShowNotification(new GUIContent("Data filled successfully!"), 1);
            return;


            var filePath = $"{loadedPath.AbsoluteFormat()}\\{PathFactory.MAIN_FILE}.{PathFactory.MAIN_TYPE}";

            var rawData = JsonConvert.SerializeObject(cachedData, Formatting.Indented);

            if (File.Exists(filePath))
                File.Delete(filePath);

            File.WriteAllText(filePath, rawData);

        }



        #endregion Read Cached Folder Data


        #region Preferences

        private void InitPreferences()
        {
            preferences = new ManagerPreferences();
            preferences = PreferenceManager.LoadPreference<ManagerPreferences>(PathFactory.MANAGER_PREFS_FILE);

            if (preferences == null)
                preferences = new ManagerPreferences();

        }

        private void UpdatePreferences()
        {
            PreferenceManager.SavePreference(PathFactory.MANAGER_PREFS_FILE, preferences);

        }

        #endregion Preferences
    }
}