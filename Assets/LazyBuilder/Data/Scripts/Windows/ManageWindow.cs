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
    public class ManageWindow : EditorWindow
    {
        private ManagerPreferences preferences;
        private ServerData cachedData;

        private string loadedPath;
        private Item selectedItem;
        private List<Item> items;

        private VisualElement _root;
        private VisualElement _list;

        //Style Templates
        private VisualTreeAsset _itemTemplate;
        private StyleSheet _itemStyleTemplate;

        //Header Area
        private TextField _serverPath;
        private Button _loadBttn;


        //Footer Area
        private Button _allDataBttn;
        private Button _allThumbBttn;
        private Button _missingThumbBttn;
        private Button _markdownBttn;


        //Side Tab Area - Header
        private TextElement _id;
        private VisualElement _thumbnail;
        private VisualElement _descriptionContainer;
        private TextElement _description;


        //Side Tab Area - Properties
        private VisualElement _types;
        private PopupField<string> _typesScreen;
        private VisualElement _tags;
        private List<TextField> _editableTags;
        private int tagInEdit;
        private Button _addTagBttn, _removeTagBttn, _saveTagsBttn;

        //Side Tab Area - Properties - Thumbnail Gen
        private TextField _posOffsetX, _posOffsetY, _posOffsetZ;
        private TextField _rotOffsetX, _rotOffsetY, _rotOffsetZ;


        //Side Tab Area - Footer
        private Button _openFolderBttn;
        private Button _singleThumbBttn;

        //Messages
        const string NOENTRY_MSG = "This item has no entry in the JSON file";
        const string NODATA_MSG = "This JSON entry has no data";
        const string NOTAGS_MSG = "This item does not have any tags";
        const string NOTYPES_MSG = "This item does not have any types";
        const string NOMATCHTYPES_MSG = "The item types do not match the JSON entry";
        const string OK_MSG = "All fields are correct";
        const string NEW_TAG_MSG = "New Tag";

        #region Unity Functions

        private async void OnEnable()
        {
            MainController.Init(this);

            InitPreferences();

            InitVars(); ;

            SetupBaseUI();
            SetupBindings();
            SetupCallbacks();
            SetupInputCallbacks();

            if (preferences.LastServer != null)
            {

                ServerManager.SetServer(ServerManager.CreateServer(ServerManager.ServerType.LOCAL, preferences.LastServer, null));
                await ServerManager.FetchServerData();

                loadedPath = ServerManager.GetServerPath();
                _serverPath.value = loadedPath;

                ReadCachedData();
                SetupItems();
            }


        }

        #endregion Unity Functions


        private void InitVars()
        {
            tagInEdit = -1;
            _editableTags = new List<TextField>();
        }
        private void SetupBaseUI()
        {
            _root = rootVisualElement;

            // Loads and clones our VisualTree (eg. our UXML structure) inside the root.
            var quickToolVisualTree = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath(PathFactory.BuildUiFilePath(PathFactory.MANAGER_LAYOUT_FILE), typeof(VisualTreeAsset));
            quickToolVisualTree.CloneTree(_root);

        }

        private void SetupBindings()
        {
            _list = _root.Q("List");

            //Header Area
            _serverPath = (TextField)_root.Q("Path");
            _loadBttn = (Button)_root.Q("Load");

            //Footer Area
            _allDataBttn = (Button)_root.Q("Autofill");
            _allThumbBttn = (Button)_root.Q("FillThumb");
            _missingThumbBttn = (Button)_root.Q("FillMThumb");
            _markdownBttn = (Button)_root.Q("FillMd");

            //Side Tab Area - Header
            _id = (TextElement)_root.Q("Id");
            _thumbnail = _root.Q("Thumbnail");
            _descriptionContainer = _root.Q("DescriptionContainer");
            _description = (TextElement)_root.Q("Description");

            //Side Tab Area - Properties
            _types = _root.Q("Types");
            _typesScreen = Utils.CreateDropdownField(_root.Q("ItemTypeScreen"));
            _tags = _root.Q("Tags");
            _addTagBttn = (Button)_root.Q("AddTag");
            _removeTagBttn = (Button)_root.Q("RemoveTag");
            _saveTagsBttn = (Button)_root.Q("SaveTags");

            //Side Tab Area - Properties - Thumbnail Gen
            _posOffsetX = (TextField)_root.Q("PosX");
            _posOffsetY = (TextField)_root.Q("PosY");
            _posOffsetZ = (TextField)_root.Q("PosZ");
            _rotOffsetX = (TextField)_root.Q("RotX");
            _rotOffsetY = (TextField)_root.Q("RotY");
            _rotOffsetZ = (TextField)_root.Q("RotZ");

            //Side Tab Area - Footer
            _openFolderBttn = (Button)_root.Q("OpenFolder");
            _singleThumbBttn = (Button)_root.Q("SingleThumb");

        }

        private void SetupCallbacks()
        {
            //Header Area
            _loadBttn.RegisterCallback<ClickEvent>((x) => LoadServer());

            //Footer Area
            _allThumbBttn.RegisterCallback<ClickEvent>((x) => GenerateThumbnails(false));
            _missingThumbBttn.RegisterCallback<ClickEvent>((x) => GenerateThumbnails(true));
            _allDataBttn.RegisterCallback<ClickEvent>((x) => AutoWriteData());
            _markdownBttn.RegisterCallback<ClickEvent>((x) => CreateMarkdownList());

            //Side Area Properties
            _addTagBttn.RegisterCallback<ClickEvent>((x) => AddTag());
            _removeTagBttn.RegisterCallback<ClickEvent>((x) => RemoveTag());
            _saveTagsBttn.RegisterCallback<ClickEvent>((x) => SaveTags());

            //Side Area - Footer
            _openFolderBttn.RegisterCallback<ClickEvent>((x) => OpenFolder());
            _singleThumbBttn.RegisterCallback<ClickEvent>(async (x) => await GenerateSingleThumbnail());
        }

        private void SetupItems()
        {
            _list.Clear();

            //Duplicating list
            items = ServerManager.data.Items.ToList();
            items.AddRange(cachedData.Items);

            //Removing duplicate (same Id) Items between cached and Server Data & sorts alphab
            items = items.GroupBy(X => X.Id).Select(x => x.FirstOrDefault()).OrderBy(x => x.Id).ToList();


            foreach (var item in items)
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
            id.text = item.Id.Capitalize().SeparateCase();

            var types = (TextElement)element.Q("Types");
            types.text = item.TypeIds != null ? item.TypeIds.Count.ToString() : "---";


            var tags = (TextElement)element.Q("Tags");
            tags.text = item.Tags != null ? item.Tags.Count.ToString() : "---";

            (element).RegisterCallback<ClickEvent>((x) => ItemSelected(item));

            _list.Add(element);
        }

        private async void ItemSelected(Item item)
        {

            var itemState = CheckItemState(item);

            _id.text = item.Id.Capitalize().SeparateCase();

            if (selectedItem != item)
            {
                _typesScreen.choices = item.TypeIds;

                if (item.TypeIds.Count > 0)
                    _typesScreen.value = item.TypeIds[0];
            }

            selectedItem = item;



            //Null savePath to not store image
            Texture2D result = await ServerManager.GetImage(PathFactory.BuildItemPath(item.Id), null, PathFactory.THUMBNAIL_FILE);
            _thumbnail.style.backgroundImage = result;

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

                //First Add Type Ids of the list
                foreach (var type in item.TypeIds)
                {
                    var typeIdElement = new TextElement();
                    typeIdElement.text = type;

                    //If item in list is not present in folder data - gray it
                    if (cachedItem != null && !cachedItem.TypeIds.Contains(type))
                        typeIdElement.style.opacity = .5f;

                    typeIdElement.style.marginBottom = 2;
                    _types.Add(typeIdElement);
                }

                //Then Add Type Ids of the read folder data
                if (itemState.Item1 == 2 && cachedItem != null)
                {
                    foreach (var type in cachedItem.TypeIds)
                    {
                        //If its already in the list - skip
                        if (item.TypeIds.Contains(type)) continue;

                        var typeIdElement = new TextElement();
                        typeIdElement.text = type;
                        typeIdElement.style.color = Color.yellow;
                        typeIdElement.style.marginBottom = 2;
                        _types.Add(typeIdElement);
                    }
                }
            }


            _tags.Clear();
            _editableTags.Clear();
            _saveTagsBttn.SetEnabled(false);
            _removeTagBttn.SetEnabled(false);
            if (item.Tags != null)
            {
                foreach (var tag in item.Tags)
                {
                    CreateTag(tag);
                }
            }


        }

        private void SetupInputCallbacks()
        {
            _root.RegisterCallback<KeyDownEvent>(OnKeyboardKeyDown, TrickleDown.TrickleDown);
        }

        private void OnKeyboardKeyDown(KeyDownEvent e)
        {

            if (e.keyCode == KeyCode.Escape)
                this.Close();

            if (e.keyCode == KeyCode.DownArrow)
                MoveItemSelection(false);

            if (e.keyCode == KeyCode.UpArrow)
                MoveItemSelection(true);
        }


        private void MoveItemSelection(bool up)
        {
            if (selectedItem == null) return;

            var index = items.TakeWhile(x => x.Id != selectedItem.Id).Count();

            index += !up ? 1 : -1;

            if (index < 0 || index >= items.Count) return;


            ItemSelected(items[index]);
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

        private async void LoadServer()
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

            loadedPath = _serverPath.value;


            ReadCachedData();
            SetupItems();


            preferences.LastServer = loadedPath;
            UpdatePreferences();
        }
        private void OpenFolder()
        {
            if (selectedItem == null) return;

            var path = $"{loadedPath}\\{PathFactory.DATA_FOLDER}\\{selectedItem.Id}";
            System.Diagnostics.Process.Start("explorer.exe", "/open, " + path);

        }

        private async void GenerateThumbnails(bool missing)
        {

            for (int i = 0; i < items.Count; i++)
            {
                var img = await ServerManager.GetImage(PathFactory.BuildItemPath(items[i].Id), null, PathFactory.THUMBNAIL_FILE);
                if (missing && img != null) continue;
                ItemSelected(items[i]);
                await GenerateSingleThumbnail();
            }

            this.ShowNotification(new GUIContent("Missing Thumbnails generated successfully!"), 1);
        }

        private async Task GenerateSingleThumbnail()
        {
            if (selectedItem == null) return;

            var cachedItem = cachedData.Items.Where(x => x.Id == selectedItem.Id).FirstOrDefault();

            if (cachedItem == null) return;

            var itemType = _typesScreen.value;

            Vector3 posOffset = Vector3.zero;
            posOffset.x = float.Parse(_posOffsetX.text);
            posOffset.y = float.Parse(_posOffsetY.text);
            posOffset.z = float.Parse(_posOffsetZ.text);
            Vector3 rotOffset = Vector3.zero;
            rotOffset.x = float.Parse(_rotOffsetX.text);
            rotOffset.y = float.Parse(_rotOffsetY.text);
            rotOffset.z = float.Parse(_rotOffsetZ.text);

            await Utils.CreateThumbnailFromModelAsync($"{loadedPath}\\{PathFactory.DATA_FOLDER}\\{selectedItem.Id}", $"{selectedItem.Id}_{itemType}", posOffset, rotOffset);

            //Refresh Item Info
            ItemSelected(selectedItem);
        }

        private void CreateTag(string tag)
        {
            var tagElement = new TextField();
            tagElement.value = tag;
            tagElement.style.marginBottom = 2;

            tagElement.RegisterCallback<FocusInEvent>(x =>
            {
                tagInEdit = _editableTags.IndexOf(tagElement);
                _removeTagBttn.SetEnabled(true);
            });
            tagElement.RegisterValueChangedCallback(x =>
            {
                if (!_saveTagsBttn.enabledSelf)
                    _saveTagsBttn.SetEnabled(true);
            });

            _tags.Add(tagElement);
            _editableTags.Add(tagElement);
        }

        private void AddTag()
        {
            CreateTag(NEW_TAG_MSG);
            _saveTagsBttn.SetEnabled(true);
        }

        private void RemoveTag()
        {
            if (tagInEdit == -1 || _editableTags.Count <= 0 || tagInEdit > _editableTags.Count - 1) return;

            _tags.Remove(_editableTags[tagInEdit]);

            //Not remove at to prevent removing wrong item (in case of auto deletion) 
            _editableTags.Remove(_editableTags[tagInEdit]);

            _saveTagsBttn.SetEnabled(true);
        }

        private void SaveTags()
        {
            var newTagsText = _editableTags.Select(x => x.value).ToList();

            if (selectedItem.Tags == newTagsText) return;

            var index = cachedData.Items.TakeWhile(x => x.Id != selectedItem.Id).Count();

            selectedItem.Tags = newTagsText;

            cachedData.Items[index] = selectedItem;

            SaveData(cachedData);

            this.ShowNotification(new GUIContent("Tags saved successfully!"), 1);

            LoadServer();
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

        private void AutoWriteData()
        {

            SaveData(cachedData);

            this.ShowNotification(new GUIContent("Data filled successfully!"), 1);

            LoadServer();
        }

        private void SaveData(ServerData serverData)
        {
            var rawData = JsonUtility.ToJson(serverData, prettyPrint: true);

            var filePath = $"{loadedPath.AbsoluteFormat()}\\{PathFactory.MAIN_FILE}.{PathFactory.MAIN_TYPE}";

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

        private void CreateMarkdownList()
        {
            string text = "# Asset List\n";

            text += "|Thumbnail |Item Id |Types|\n";
            text += "|-|-|-:|\n";
            foreach (var item in items)
            {
                text += $"|![{item.Id}](./content/{item.Id}/thumbnail.png)| {item.Id}| {item.TypeIds.Count}|\n";
            }

            var filePath = $"{loadedPath.AbsoluteFormat()}\\{PathFactory.MARKDOWN_FILE}.md";
            File.WriteAllText(filePath, text);

            this.ShowNotification(new GUIContent("Asset List Markdown created successfully!"), 1);

        }
    }
}
