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
using static LazyBuilder.PathFactory;
using static LazyBuilder.ServerManager;

namespace LazyBuilder
{

    public class BuilderWindow : EditorWindow
    {
        private BuilderPreferences preferences;
        private Dictionary<string, Server> serverList;

        private string selectedItem;
        private string selectedFile;

        private bool onlyLocalSearch;

        private VisualElement _root;

        private TextField _searchBar;

        //Servers DropDown
        private DropdownField _serversDropdown;
        private VisualElement _serversDropContainer;
        private TextElement _serverDropSelected;
        private VisualElement _serverDropIcon;

        private VisualElement _mainTabContainer;

        //Servers Tab
        private VisualElement _serversTabContainer;
        private Button _serversBackBttn;
        private VisualTreeAsset _serverTemplate;
        private VisualElement _serversListContainer;
        private Button _addServerBttn;
        private Button _removeServerBttn;
        private Button _saveServersBttn;
        private VisualElement _saveServersIcon;
        private VisualElement _selectedListServer;


        private TextElement _mainTitle;
        private VisualElement _mainImg;

        private VisualElement _itemTypeIcon;
        private DropdownField _itemTypeDropdown;
        private TextElement _itemTypeSelected;

        private VisualElement _colorPallete;

        private Button _zoomInBttn;
        private Button _zoomOutBttn;
        private Button _searchBttn;

        private VisualElement _grid;
        private VisualElement _searchBttnIcon;
        private VisualElement _generateBttnIcon;

        private Button _generateBttn;


        private StyleColor defaultButtonColor;
        private StyleColor activeButtonColor;

        //private Material fallbackMat;

        private PreviewRenderUtility previewRenderUtility;
        private Transform previewTransform;
        private bool renderPreview;

        private GameObject previewObj;
        private Mesh previewMesh;
        private Vector3 previewGroundPos;

        private Mesh groundMesh;
        private Material groundMat;


        //Temporary Items
        private List<string> tempFiles;
        private const int tempArraySize = 5;

        //Stored Items
        private List<string> storedFiles;


        private Vector3 initPos;
        bool setDist;
        bool isSearchFocused;

        Material[] previewMats;
        List<Color> previewColors;

        private VisualTreeAsset _itemTemplate;

        //Messages
        const string LOCALPOOL_MSG = "Local";
        const string MAINPOOL_MSG = "Main";
        const string NEWPOOL_MSG = "Edit servers...";


        #region Unity Functions
        private async void OnEnable()
        {
            InitVariables();
            InitPreferences();

            SetupPreviewUtils();

            SetupStoredFiles();
            SetupTempFiles();

            SetupBaseUI();
            SetupBindings();
            SetupCallbacks();
            SetupInputCallbacks();

            SwitchPanel();
            SetupIcons();
            SetupCamera();

            SetupServers();

            //This infinite async loop ensures that the preview's camera is always rendered
            RepaintCycle();

            //Debug.Log("Searcbar focus");
            this.Focus();
            _searchBar.Focus();
        }


        private void OnDisable()
        {
            previewRenderUtility?.Cleanup();
        }
        private void OnDestroy()
        {
            previewRenderUtility?.Cleanup();
            UpdatePreferences();
        }

        private void OnGUI()
        {
            RenderItemPreview();
        }

        #endregion Unity Functions



        private void InitVariables()
        {
            initPos = Vector3.zero;
            defaultButtonColor = new Color(0.345098f, 0.345098f, 0.345098f, 1);
            activeButtonColor = new Color(0.27f, 0.38f, 0.49f);

            MainController.Init(this);
        }


        #region Base UI

        private void SetupBaseUI()
        {
            _root = rootVisualElement;
            // root.styleSheets.Add(Resources.Load<StyleSheet>("qtStyles"));

            // Loads and clones our VisualTree (eg. our UXML structure) inside the root.
            var quickToolVisualTree = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath(PathFactory.BuildUiFilePath(PathFactory.BUILDER_LAYOUT_FILE), typeof(VisualTreeAsset));
            //var quickToolVisualTree = Resources.Load<VisualTreeAsset>("MainLayout");
            quickToolVisualTree.CloneTree(_root);
        }

        private void SetupBindings()
        {
            _mainTitle = (TextElement)_root.Q("TitleText");
            _mainImg = _root.Q("ItemThumbnail");
            _searchBar = (TextField)_root.Q("SearchBar");
            _grid = _root.Q("ItemsColumn1");

            _mainTabContainer = _root.Q("TabContent_Main");
            _serversTabContainer = _root.Q("TabContent_Servers");

            _serversBackBttn = (Button)_root.Q("Back_servers");

            _generateBttn = (Button)_root.Q("GenerateBttn");
            _generateBttnIcon = _root.Q("GenerateBttnIcon");

            _zoomInBttn = (Button)_root.Q("ZoomIn");
            _zoomOutBttn = (Button)_root.Q("ZoomOut");

            //Servers Dropdown
            _serversDropContainer = _root.Q("PoolsContainer");
            _serversDropdown = (DropdownField)_root.Q("PoolsList");
            _serverDropSelected = (TextElement)_root.Q("PoolSelected");
            _serverDropIcon = _root.Q("ServerDropIcon");

            //Servers Panel
            _serversListContainer = _root.Q("ServersList");
            _addServerBttn = (Button)_root.Q("AddServer");
            _removeServerBttn = (Button)_root.Q("RemoveServer");
            _saveServersBttn = (Button)_root.Q("SaveServersBttn");
            _saveServersIcon = _root.Q("SaveServersIcon");

            _itemTypeIcon = _root.Q("ItemTypeIcon");
            _itemTypeSelected = (TextElement)_root.Q("ItemTypeSelected");
            _itemTypeDropdown = (DropdownField)_root.Q("ItemTypeDropdown");


            _colorPallete = _root.Q("Pallete");

            _searchBttn = (Button)_root.Q("SearchBttn");
            _searchBttnIcon = _root.Q("SearchBttnIcon");
        }

        private void SetupIcons()
        {
            _searchBttnIcon.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("d_Search Icon").image;
            _generateBttnIcon.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("GameObject Icon").image;
            _itemTypeIcon.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("icon dropdown").image;
            _zoomInBttn.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("d_Search Icon").image;
            _zoomOutBttn.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("d_Search Icon").image;
            _serverDropIcon.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("d_icon dropdown@2x").image;
            _saveServersIcon.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("d_SaveAs").image;
        }

        private void SetupCallbacks()
        {
            //Event.KeyboardEvent.Add()
            //Event.current.mousePosition

            _serversBackBttn.clicked += () => SwitchPanel(true);


            _addServerBttn.clicked += AddServer;
            _removeServerBttn.clicked += RemoveServer;
            _saveServersBttn.clicked += SaveEditServers;

            _generateBttn.clicked += Generate;
            //_searchBar.RegisterValueChangedCallback(SearchChanged);
            _itemTypeDropdown.RegisterValueChangedCallback(ItemTypeChanged);
            _serversDropdown.RegisterValueChangedCallback(ServerChanged);

            _searchBttn.clicked += Search;
            _searchBar.RegisterCallback<FocusInEvent>(OnSearchFocusIn);
            _searchBar.RegisterCallback<FocusOutEvent>(OnSearchFocusOut);
            _zoomInBttn.clicked += ZoomIn;
            _zoomOutBttn.clicked += ZoomOut;
        }

        private void SetupItems()
        {
            _grid.Clear();

            var items = ServerManager.data.Items;

            for (int i = 0; i < items.Count; i++)
            {
                if (onlyLocalSearch && !HasItemAnyType(items[i].Id)) continue; 
                if (_itemTemplate == null)
                    _itemTemplate = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath(PathFactory.BuildUiFilePath(PathFactory.BUILDER_ITEM_LAYOUT_FILE), typeof(VisualTreeAsset));

                var element = _itemTemplate.CloneTree();
                SetupItem(element, items[i].Id, i, PathFactory.BuildItemPath(items[i].Id));
                _grid.Add(element);
                //await Task.Delay(1);
            }

            SelectDefaultItem();
        }

        private async void SetupItem(VisualElement element, string name, int index, string path)
        {
            element.name = name;
            var label = element.Query<Label>().First();
            label.text = name;

            var button = element.Q<Button>();
            var shadow = element.Q<VisualElement>("Shadow");

            BeginItemAnimation(button, shadow);

            // var buttonIcon = button.Q(className: "quicktool-button-icon");

            var texture = await ServerManager.server.GetImage(path, PathFactory.THUMBNAIL_FILE, PathFactory.THUMBNAIL_TYPE);

            var imgHolder = button.Q("Img");
            imgHolder.style.backgroundImage = texture;

            var iconOverlay = button.Q("Icon");
            iconOverlay.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("d_Valid").image;

            // Sets a basic tooltip to the button itself.
            button.tooltip = name;

            //Add Callback
            button.clicked += () => ItemSelected(name, index, texture);

            if (!HasItemAnyType(name))
            {
                var overlay = button.Q("Overlay");
                overlay.visible = false;
            }


        }



        private void SwitchPanel(bool mainView = true)
        {
            _mainTabContainer.style.display = mainView ? DisplayStyle.Flex : DisplayStyle.None;
            renderPreview = mainView;

            _serversTabContainer.style.display = !mainView ? DisplayStyle.Flex : DisplayStyle.None;

            if (!mainView)
            {
                SetupEditServers();
            }
        }

        #endregion Base UI

        #region Item Render & Preview

        private void SetupPreviewUtils()
        {
            GameObject groundObj = AssetDatabase.LoadAssetAtPath(PathFactory.BuildMeshFilePath(PathFactory.GROUND_FILE), typeof(UnityEngine.Object)) as GameObject;

            if (groundObj == null)
            {
                Debug.LogError("Ground Mesh couldn't be fetched");
                return;
            }
            groundMesh = groundObj.GetComponent<MeshFilter>().sharedMesh;
            groundMat = AssetDatabase.LoadAssetAtPath<Material>(PathFactory.BuildMaterialFilePath(PathFactory.GROUND_FILE));
        }

        private void SetupCamera()
        {
            //Debug.Log("Camera Setup");
            if (previewRenderUtility != null)
            {
                previewRenderUtility.Cleanup();
                previewRenderUtility = null;
            }
            renderPreview = true;

            previewRenderUtility = new PreviewRenderUtility();
            previewTransform = previewRenderUtility.camera.transform;
            previewRenderUtility.camera.nearClipPlane = .001f;
            previewRenderUtility.camera.farClipPlane = 6000f;
            previewRenderUtility.camera.fieldOfView = 3;

            previewRenderUtility.lights[0].intensity = 1;
            previewRenderUtility.lights[1].intensity = 1;
            previewRenderUtility.ambientColor = Color.white;
            //previewRenderUtility.ambientColor = Color.white;
            //previewRenderUtility.camera.transform.position = new Vector3(0, 10.5f, -18);
            //previewRenderUtility.camera.transform.eulerAngles = new Vector3(30, 0, 0);

            previewTransform.position = new Vector3(0, 10.5f, -18);
            previewRenderUtility.camera.transform.position += (new Vector3(0, -.5835f, 1) * 17);

            //previewRenderUtility.camera.clearFlags = CameraClearFlags.Skybox;
            previewRenderUtility.camera.clearFlags = CameraClearFlags.SolidColor;
            previewRenderUtility.camera.backgroundColor = Color.red;

            previewTransform.eulerAngles = new Vector3(30, 0, 0);

            //previewRenderUtility.camera.orthographic = true;
        }



        private void RenderItemPreview()
        {
            if (previewRenderUtility == null || !renderPreview) return;

            Rect rect = _mainImg.worldBound;

            //previewRenderUtility.BeginStaticPreview(r);
            previewRenderUtility.BeginPreview(rect, GUIStyle.none);
            //Debug.Log($"OnGUI {tCube} / {tMat}");

            if (setDist)
            {
                setDist = false;
                if (initPos == Vector3.zero)
                    initPos = previewTransform.position;

                previewTransform.position = initPos;
            }


            if (previewMesh != null)
            {
                //Debug.Log("Drawing preview Mesh");

                for (int i = 0; i < previewMats.Length; i++)
                {
                    previewRenderUtility.DrawMesh(previewMesh, Vector3.zero, Quaternion.Euler(0, 0, 0), previewMats[i], i);
                    previewRenderUtility.camera.transform.RotateAround(previewMesh.bounds.center, Vector3.up, Time.deltaTime);

                }

                previewRenderUtility.DrawMesh(groundMesh, previewGroundPos, Vector3.one * 1000, Quaternion.Euler(90, 0, 0), groundMat, 0, new MaterialPropertyBlock(), null, false);


            }

            previewRenderUtility.camera.Render();
            //var rTexture = previewRenderUtility.EndStaticPreview();
            var image = previewRenderUtility.EndPreview();
            GUI.DrawTexture(rect, image);
        }

        async void RepaintCycle()
        {
            for (int i = 0; i < 9000; i++)
            {
                Repaint();
                await Task.Delay(2);
            }

        }

        private void Zoom(float factor)
        {
            previewRenderUtility.camera.transform.position += previewRenderUtility.camera.transform.forward * 1f * factor;
        }
        private void ZoomIn()
        {
            //previewRenderUtility.camera.transform.position += new Vector3(1, 1, 1);
            //previewRenderUtility.camera.transform.position += new Vector3(0, -.5835f, 1);
            previewRenderUtility.camera.transform.position += previewRenderUtility.camera.transform.forward * .7f;
        }

        private void ZoomOut()
        {
            //previewRenderUtility.camera.transform.position += new Vector3(-1, -1, -1);
            //previewRenderUtility.camera.transform.position += new Vector3(0, -.5835f, 1) * -1;
            previewRenderUtility.camera.transform.position += previewRenderUtility.camera.transform.forward * -.7f;
        }

        #endregion Item Render & Preview 

        #region IO

        private void SetupInputCallbacks()
        {
            _root.RegisterCallback<KeyDownEvent>(OnKeyboardKeyDown, TrickleDown.TrickleDown);
            _root.RegisterCallback<MouseDownEvent>(OnMouseKeyDown, TrickleDown.TrickleDown);
            _root.RegisterCallback<WheelEvent>(OnMouseWheelDown, TrickleDown.TrickleDown);
        }

        private void OnKeyboardKeyDown(KeyDownEvent e)
        {
            if (e.keyCode == KeyCode.Escape)
                this.Close();

            else if (e.keyCode == KeyCode.Return && isSearchFocused)
            {
                Search();
                _searchBar.Focus();
            }

            else if ((e.keyCode == KeyCode.Return || e.character == '\n') && selectedFile != "")
            {
                Generate();
            }
        }

        private void OnMouseKeyDown(MouseDownEvent e)
        {
            //Debug.Log($"On mouse down {e.button}");
        }

        private void OnMouseWheelDown(WheelEvent e)
        {
            if (e.delta == Vector3.zero) return;

            //Debug.Log($"On mouse wheel scroll {e.delta}");

            if (CheckIfMouseOverSreen(_mainImg.worldBound, true, new Vector2(0, 20)))
            {
                Zoom(e.delta.y);
            }



        }

        private static bool CheckIfMouseOverSreen(Rect r, bool relativePos = true, Vector2? offset = null)
        {
            Vector2 mousePos = relativePos ? Event.current.mousePosition : GUIUtility.GUIToScreenPoint(Event.current.mousePosition);

            Debug.Log($"Mouse position: {mousePos.x},{mousePos.y}");
            Debug.Log($"ScreenMin: {r.xMin},{r.yMin} ScreenMax: {r.xMax},{r.yMax}");

            var xMin = r.xMin + (offset.HasValue ? offset.Value.x : 0);
            var xMax = r.xMax + (offset.HasValue ? offset.Value.x : 0);
            var yMin = r.yMin + (offset.HasValue ? offset.Value.y : 0);
            var yMax = r.yMax + (offset.HasValue ? offset.Value.y : 0);

            var condition = mousePos.x >= xMin && mousePos.x <= xMax &&
                             mousePos.y >= yMin && mousePos.y <= yMax;

            Debug.Log($"Is over screen? - {condition}");
            return condition;
        }
        #endregion IO


        private async Task<string> GetItem(string itemId, string itemTypeId)
        {
            var filename = $"{itemId}_{itemTypeId}";
            selectedFile = $"{filename}.{PathFactory.MESH_TYPE}";

            bool storedResult = HasStoredFile(selectedFile);
            bool tempResult = HasTempFile(selectedFile);

            string path;

            if (storedResult)
            {
                path = $"{PathFactory.relativeToolPath}/{PathFactory.STORED_ITEMS_PATH}/{selectedFile}";
            }
            //If Temp file does not exist
            else
            {
                if (!tempResult)
                {
                    Debug.Log("Fetching item name" + filename);

                    //Fetch File from server and add to Temp list
                    await ServerManager.server.GetRawFile(PathFactory.BuildItemPath(selectedItem), PathFactory.TEMP_ITEMS_PATH, filename, PathFactory.MESH_TYPE);
                    AddTempFile(selectedFile);
                }
                path = $"{PathFactory.relativeToolPath}/{PathFactory.TEMP_ITEMS_PATH}/{selectedFile}";
            }

            AssetDatabase.Refresh();
            return path;
        }


        #region Stored Files Buffer

        private bool HasItemAnyType(string itemId)
        {
            return storedFiles.Where(x => x.StartsWith(itemId)).Any();
        }
        private void StoreTempFile(string id)
        {
            var dir = $"{Application.dataPath}/{PathFactory.STORED_ITEMS_PATH}";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            AssetDatabase.MoveAsset($"{PathFactory.relativeToolPath}/{PathFactory.TEMP_ITEMS_PATH}/{id}", $"{PathFactory.relativeToolPath}/{PathFactory.STORED_ITEMS_PATH}/{id}");

            RemoveTempFile(id);
        }


        private bool HasStoredFile(string id)
        {
            return storedFiles.Contains(id);
        }
        private void SetupStoredFiles()
        {
            string tmpPath = $"{PathFactory.absoluteToolPath}\\{PathFactory.STORED_ITEMS_PATH}";

            storedFiles = new List<string>(Utils.GetFiles(tmpPath, true));

        }

        #endregion Stored Files Buffer

        #region Temporary Files Buffer

        private void SetupTempFiles()
        {
            string tmpPath = $"{PathFactory.absoluteToolPath}\\{PathFactory.TEMP_ITEMS_PATH}";
            string[] allFiles = Utils.GetFiles(tmpPath, true);


            if (tempFiles == null)
                tempFiles = new List<string>();
            tempFiles.Clear();

            //Add items to Temp files list at the maximum size of 'tempArraySize'
            int minArrSize = Mathf.Min(tempArraySize, allFiles.Length);
            for (int i = 0; i < minArrSize; i++)
                tempFiles.Add(allFiles[i]);

        }

        private bool HasTempFile(string id)
        {
            bool hasFile = tempFiles.Contains(id);
            //int index = hasFile ? tempItems.IndexOf(id) : -1;

            return hasFile;
        }

        private void AddTempFile(string id)
        {
            // If exceeds the buffer limit - delete the 1st element
            if (tempFiles.Count >= tempArraySize)
                RemoveTempFile(tempFiles[0]);

            tempFiles.Add(id);
        }

        private void RemoveTempFile(string id)
        {
            var filePath = $"{PathFactory.absoluteToolPath}\\{PathFactory.TEMP_ITEMS_PATH}\\{id}";

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                File.Delete(filePath + ".meta");
            }

            tempFiles.Remove(id);
        }



        #endregion Temporary Items Buffer

        #region Items Search

        private void Search()
        {
            var initList = ServerManager.data.Items;
            var searchVal = _searchBar.value;

            if (String.IsNullOrWhiteSpace(searchVal))
                SetupItems();
            else
            {
                var searchedList = FuzzySearch(initList, searchVal);
                SetupItems();
            }
        }

        private List<Item> FuzzySearch(List<Item> source, string key)
        {
            return source.Where(x => FuzzyItemMatch(x, key) > 1).OrderByDescending(x => FuzzyItemMatch(x, key)).ToList();
        }

        private bool FuzzyItem(Item item, string key)
        {
            return FuzzyItemMatch(item, key) == 0;
        }
        private int FuzzyItemMatch(Item source, string key)
        {
            int maxMatches = FuzzyStringMatch(source.Id, key);
            foreach (var tag in source.Tags)
                maxMatches = Mathf.Max(maxMatches, FuzzyStringMatch(tag, key));
            return maxMatches;
        }

        private int FuzzyStringMatch(string source, string key)
        {
            int maxMatches = 0, currentMatches = 0;

            var charactersBase = source.ToCharArray();
            var charactersKey = key.ToCharArray();
            for (int i = 0; i < charactersBase.Length; i++)
            {
                if (currentMatches < charactersKey.Length && charactersKey[currentMatches] == charactersBase[i])
                    currentMatches++;
                else if (currentMatches > 0)
                {
                    maxMatches = currentMatches > maxMatches ? currentMatches : maxMatches;
                    currentMatches = 0;
                }

            }

            maxMatches = currentMatches > maxMatches ? currentMatches : maxMatches;
            return maxMatches;
        }

        private void OnSearchFocusIn(FocusInEvent focus)
        {
            isSearchFocused = true;
            _serversDropContainer.style.backgroundColor = activeButtonColor;
            _searchBttn.style.backgroundColor = activeButtonColor;
        }

        private void OnSearchFocusOut(FocusOutEvent focus)
        {
            isSearchFocused = false;
            _serversDropContainer.style.backgroundColor = defaultButtonColor;
            _searchBttn.style.backgroundColor = defaultButtonColor;
        }
        #endregion Items Search

        #region Animation


        private async void BeginItemAnimation(Button button, VisualElement shadow)
        {
            button.style.opacity = 0;
            shadow.style.opacity = 0;

            await Task.Delay(50);
            button.style.opacity = 1;
            shadow.style.opacity = 1;
        }


        #endregion Animation

        #region Object Generation

        private void Generate()
        {
            StoreTempFile(selectedFile);
            var gObj = AssetDatabase.LoadAssetAtPath($"{PathFactory.relativeToolPath}/{PathFactory.STORED_ITEMS_PATH}/{selectedFile}", typeof(UnityEngine.Object)) as GameObject;

            if (gObj == null) return;
            var newObj = GameObject.Instantiate(gObj);
            PlaceObjectInView(newObj);
            //PlaceObjectInView(test);
            Close();
        }

        private void PlaceObjectInView(GameObject gObject)
        {
            var gMesh = gObject.GetComponent<MeshRenderer>();
            //test.position=  sceneWindow.camera.ScreenToWorldPoint(mPos)+ sceneWindow.camera.transform.forward*2;
            var hits = Physics.RaycastAll(MainController.sceneWindow.camera.transform.position, MainController.sceneWindow.camera.transform.forward);
            Vector3? position = null, upVector = null;
            foreach (var hit in hits)
            {
                if (hit.transform.GetInstanceID() == gObject.transform.GetInstanceID()) continue;
                //position = hit.point;
                position = hit.point + new Vector3(0, gMesh.bounds.extents.y, 0) - gMesh.localBounds.center;
                upVector = hit.normal;
                break;
            }

            if (!position.HasValue)
                position = MainController.sceneWindow.camera.transform.forward * 1.5f;

            if (!upVector.HasValue)
                upVector = Vector3.up;

            gObject.transform.position = position.Value;
            //gObject.transform.position = Vector3.zero;
            //gObject.transform.position = sceneWindow.camera.transform.position;
            //gObject.transform.localPosition -= gMesh.localBounds.center;
            gObject.transform.up = upVector.Value;
        }

        private void CreatePrimitive(string primitiveTypeName)
        {
            var pt = (PrimitiveType)Enum.Parse
                         (typeof(PrimitiveType), primitiveTypeName, true);
            var go = ObjectFactory.CreatePrimitive(pt);
            go.transform.position = Vector3.zero;
        }

        #endregion Object Generation

        #region Colour Picker
        private VisualElement CreateColorPicker(Color color)
        {
            var field = new ColorField();
            //field.hdr = true;
            field.style.width = 60;
            field.value = color;
            field.RegisterValueChangedCallback(ColorChanged);

            var container = new VisualElement();
            container.Add(field);
            container.style.width = 20;
            container.style.height = 14;
            //container.style.marginRight = 5;
            container.style.overflow = Overflow.Hidden;

            var borderRadius = 10;
            StyleScale s = new Scale(new Vector3(1, 1.2f, 1));
            container.style.scale = s;
            container.style.borderBottomLeftRadius = borderRadius;
            container.style.borderBottomRightRadius = borderRadius;
            container.style.borderTopLeftRadius = borderRadius;
            container.style.borderTopRightRadius = borderRadius;
            return container;
        }

        private void ColorChanged(ChangeEvent<Color> value)
        {
            var index = previewColors.IndexOf(value.previousValue);
            //Debug.Log($"Chaning mat  at index {index} mats have arr size of {previewMats.Length}");

            if (index == -1) return;
            previewColors[index] = value.newValue;
            previewMats[index].color = value.newValue;
        }
        #endregion Colour Picker

        #region Item & Type Change
        private void ItemSelected(string itemId, int index, Texture2D icon = null, bool manualTypeSelect = false)
        {
            if (selectedItem == itemId) return;

            preferences.LastItem = itemId;


            selectedItem = itemId;
            //lastSessionItem = itemId;
            _mainImg.style.backgroundImage = icon;
            _mainTitle.text = itemId.Capitalize();

            var choices = ServerManager.data.Items[index].TypeIds;

            for (int i = 0; i < choices.Count; i++)
            {
                if (HasStoredFile($"{itemId}_{choices[i]}.{PathFactory.MESH_TYPE}"))
                    choices[i] = $"{choices[i]}\t✓";
            }

            _itemTypeDropdown.choices = choices;

            if (_itemTypeDropdown.choices.Count > 0 && !manualTypeSelect)
                _itemTypeDropdown.value = _itemTypeDropdown.choices[0];


            setDist = true;
        }

        private void ItemTypeChanged(ChangeEvent<string> value) => ItemTypeSelected(value.newValue);
        private async void ItemTypeSelected(string value)
        {

            //If Type contains a symbol - trim it to get the true Id
            if (value.Contains('\t'))
            {
                value = value.Substring(0, value.IndexOf('\t'));
            }

            if (string.IsNullOrEmpty(value)) return;

            //Update Preferences
            preferences.LastItemType = value;
            UpdatePreferences();

            //Reset Mesh Preview
            previewObj = null;
            previewMesh = null;
            previewMats = null;

            _itemTypeSelected.text = value;

            string objPath = await GetItem(selectedItem, value);
            previewObj = AssetDatabase.LoadAssetAtPath(objPath, typeof(UnityEngine.Object)) as GameObject;

            if (previewObj == null)
            {
                Debug.LogError("Item couldn't be loaded");
                return;
            }


            var meshRend = previewObj.gameObject.GetComponent<MeshRenderer>();
            previewMats = meshRend.sharedMaterials;
            previewColors = new List<Color>();

            //Make all materials not glossy
            foreach (var material in previewMats)
            {
                material.SetColor("_EmissionColor", material.color);
                material.SetFloat("_Glossiness", 0f);
                previewColors.Add(material.color);

            }

            //Add respective color pickers
            _colorPallete.Clear();
            for (int i = 0; i < previewColors.Count; i++)
            {
                var field = CreateColorPicker(previewColors[i]);
                _colorPallete.Add(field);
            }

            previewMesh = previewObj.gameObject.GetComponent<MeshFilter>().sharedMesh;
            previewGroundPos = new Vector3(previewMesh.bounds.center.x, previewMesh.bounds.min.y, previewMesh.bounds.center.z);

            var bounds = previewMesh.bounds;
            var objectSizes = bounds.max - bounds.min;
            var objectSize = Mathf.Max(objectSizes.x, objectSizes.y, objectSizes.z);


            // Visible height 1 meter in front
            var cameraView = 2.0f * Mathf.Tan(0.5f * Mathf.Deg2Rad * previewRenderUtility.camera.fieldOfView);
            // Combined wanted distance from the object (1 is the distance factor)
            var distance = 1 * objectSize / cameraView;
            // Estimated offset from the center to the outside of the object
            distance += 0.5f * objectSize;

            previewRenderUtility.camera.transform.RotateAround(previewMesh.bounds.center, Vector3.up, Time.deltaTime);
            await Task.Delay(2);

            //If camera has been destroyed while waiting - return
            if (previewRenderUtility == null || previewRenderUtility.camera == null) return;

            var targetPosition = bounds.center - distance * previewRenderUtility.camera.transform.forward;
            previewRenderUtility.camera.transform.position = targetPosition;

            //Debug.Log($"Max bounds {maxSize}");
        }


        private void SelectDefaultItem()
        {
            if (preferences.LastItem != null && preferences.LastItemType != null)
            {
                var index = ServerManager.data.Items.TakeWhile(x => x.Id != preferences.LastItem).Count();

                //Manual selection set to 'true' to avoid auto selecting a typeId 
                ItemSelected(preferences.LastItem, index, null, true);
                ItemTypeSelected(preferences.LastItemType);
            }
        }
        #endregion Item & Type Change

        #region Manage Servers
        private void SetupServers()
        {
            serverList = new Dictionary<string, Server>();

            serverList.Add(LOCALPOOL_MSG, null);

            //If Server preferences are not empty
            if (preferences.Servers_src != null && preferences.Servers_Id != null)
            {
                //Load Servers from preferences
                for (int i = 0; i < preferences.Servers_Id.Count; i++)
                {
                    var server = ServerManager.CreateServer(preferences.Servers_Type[i], preferences.Servers_src[i], preferences.Servers_branch[i]);
                    serverList.Add(preferences.Servers_Id[i], server);
                }
            }
            else
            {
                //Add Default server
                var server = ServerManager.CreateServer(ServerType.GIT, Server_Git.defaultRepo, Server_Git.defaultBranch);

                //Add preferency entry
                preferences.Servers_Id = new List<string>() { MAINPOOL_MSG };
                preferences.Servers_Type = new List<ServerType>() { ServerType.GIT };
                preferences.Servers_src = new List<string> { Server_Git.defaultRepo };
                preferences.Servers_branch = new List<string> { Server_Git.defaultBranch };

                //Add server
                serverList.Add(MAINPOOL_MSG, server);
            }

            //If last server not set - set it to the Main server
            if (preferences.LastServer == null || !serverList.ContainsKey(preferences.LastServer))
            {
                preferences.LastServer = MAINPOOL_MSG;
            }

            serverList.Add(NEWPOOL_MSG, null);
            _serversDropdown.choices = serverList.Keys.ToList();
            _serversDropdown.value = preferences.LastServer;
        }



        private void SetupEditServers()
        {
            _serversListContainer.Clear();

            foreach (var server in serverList)
            {
                if (server.Value == null) continue;

                ServerType type = ServerType.GIT;
                if (server.Value is Server_Local)
                    type = ServerType.LOCAL;

                CreateServerEntry(type, server.Key, server.Value.GetSrc(), server.Value.GetBranch());
            }
        }

        private void SaveEditServers()
        {

        }
        private void AddServer()
        {
            CreateServerEntry(ServerType.GIT, null, null);
        }

        private void CreateServerEntry(ServerType type, string id = null, string src = null, string branch = null)
        {
            if (_serverTemplate == null)
                _serverTemplate = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath(PathFactory.BuildUiFilePath(PathFactory.BUILDER_SERVER_ITEM_FILE), typeof(VisualTreeAsset));

            var element = _serverTemplate.CloneTree();

            var idField = (TextField)element.Q("Id");
            if (id != null) idField.value = id;
            idField.RegisterCallback<FocusEvent>((x) => { _selectedListServer = element; });

            var branchField = (TextField)element.Q("Branch");
            if (branch != null) branchField.value = branch;

            var typesField = (DropdownField)element.Q("Type");
            typesField.choices = GetServerTypes();
            typesField.RegisterValueChangedCallback(x =>
            {
                branchField.visible = x.newValue == ServerType.LOCAL.ToString();

            });

            var srcField = (TextField)element.Q("Src");
            if (src != null) srcField.value = src;
            srcField.RegisterCallback<FocusEvent>((x) => { _selectedListServer = element; });

            typesField.value = type.ToString();

            _serversListContainer.Add(element);
        }

        private void RemoveServer()
        {
            if (_selectedListServer == null) return;

            _serversListContainer.Remove(_selectedListServer);

            _selectedListServer = null;
        }


        private async void ServerChanged(ChangeEvent<string> value)
        {
            var newServer = value.newValue;

            if (newServer == NEWPOOL_MSG)
            {
                SwitchPanel(false);
                return;
            }

           Server currentServer= serverList[newServer];  
           ServerManager.SetServer(currentServer);

            await ServerManager.FetchServerData();


            SwitchPanel(true);
            _serverDropSelected.text = newServer;

            
            onlyLocalSearch = newServer == LOCALPOOL_MSG;
            SetupItems();

            preferences.LastServer = newServer;
            UpdatePreferences();
        }

        #endregion Manage Servers

        #region Preferences

        private void InitPreferences()
        {
            preferences = new BuilderPreferences();
            preferences = PreferenceManager.LoadPreference<BuilderPreferences>(PathFactory.BUILDER_PREFS_FILE);

            if (preferences == null)
                preferences = new BuilderPreferences();
        }

        private void UpdatePreferences()
        {
            PreferenceManager.SavePreference(PathFactory.BUILDER_PREFS_FILE, preferences);

        }

        #endregion Preferences
    }
}
