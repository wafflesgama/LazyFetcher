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
using static LazyBuilder.ServerManager;

namespace LazyBuilder
{
    public class BuilderWindow : EditorWindow
    {
        private BuilderPreferences preferences;
        private Dictionary<string, Server> serverList;

        private ServerData storedData;
        List<Item> currentItems;
        List<Item> currentItemsInPage;

        private int currentPageIndex;

        private string selectedItemId;
        private string selectedFile;

        private bool onlyLocalSearch;

        private VisualElement _root;

        private TextField _searchBar;

        //Servers DropDown
        private PopupField<string> _serversDropdown;
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

        //Data Pages (Items & Error Pages)
        private VisualElement _mainPage;
        private VisualElement _errorPage;
        private Label _errorMessage;

        //Pagination
        private TextElement _pageIndexMssg;
        private Button _prevPageBttn;
        private Button _nextPageBttn;
        private PopupField<string> _pageSizeDropdown;

        private TextElement _mainTitle;
        private VisualElement _mainImg;

        private VisualElement _itemTypeIcon;
        private PopupField<string> _itemTypeDropdown;
        private TextElement _itemTypeSelected;

        private VisualElement _colorPallete;

        private Button _searchBttn;

        private Button _randomSortBttn;
        private Button _alphaSortBttn;
        private Button _similarSortBttn;


        //Generation Props
        private TextField _propName;
        private Toggle _propCol;
        private Toggle _propRb;

        private VisualElement _grid;
        private VisualElement _searchBttnIcon;

        private VisualElement _randomSortBttnIcon;
        private VisualElement _alphaSortBttnIcon;
        private VisualElement _similarSortBttnIcon;

        private VisualElement _generateBttnIcon;

        private Button _generateBttn;

        private StyleColor defaultButtonColor;
        private StyleColor activeButtonColor;

        //private Material fallbackMat;

        private PreviewRenderUtility previewRenderUtility;
        private Vector3 previewRotation;
        private Transform previewTransform;
        private Label _previewLoaderText;
        private bool renderPreview;

        private GameObject previewObj;
        private List<(Mesh, Material[])> previewMeshes;
        private Bounds previewBounds;
        private Vector3 previewGroundPos;
        private List<Material> previewMaterials;

        private Mesh groundMesh;
        private Material groundMat;


        //Temporary Items
        private List<string> tempFiles;
        private const int tempArraySize = 5;

        //-----Footer Area

        //Debug Messager
        private TextElement _debugMssg;

        //Settings
        private PopupField<string> _settingdDrop;
        private VisualElement _settingsIcon;

        //About
        private PopupField<string> _aboutDrop;
        private VisualElement _aboutIcon;

        private Vector3 initPos;
        bool setDist;
        bool isSearchFocused;


        private VisualTreeAsset _itemTemplate;

        private Vector2 lastMousePos;

        private bool isLoadingPrev;
        private int loadingPrevFrame;
        private const int loadingPrevFrameInterval = 20;

        private bool isRotatingPrev;
        private bool isTranslatingPrev;

        //Messages
        const string LOCALPOOL_MSG = "Local";
        const string MAINPOOL_MSG = "Main";
        const string NEWPOOL_MSG = "Edit servers...";
        const string GROUNDMESHNOT_MSG = "Ground mesh couldn't be fetched";
        const string NO_CONNECTION_MSG = "No connection to this library";
        const string NOT_FOUND_MSG = "The library URL appears to broken or incomplete";
        const string INVALID_DATA_MSG = "The library does not follow the standard structre";

        const string SETTINGS_RESETPREF_MSG = "Reset Preferences";
        const string SETTINGS_CLEARTEMP_MSG = "Delete Temp Items";
        const string SETTINGS_CLEARSTORED_MSG = "Delete Stored Items";

        const string ABOUT_TOOL_REPO_MSG = "About Lazy Builder";

        const string ABOUT_LIB_REPO_MSG = "About Lazy Builder Library";
        const string ABOUT_REPORT_MSG = "Report an error";
        const string ABOUT_DONATE_MSG = "Donate or support the Project";

        const string SETTING_RESETPREF_TITLE = "Reset Preferences";
        const string SETTING_RESETPREF_DESC = "Are you sure you want to reset this window's preferences";
        const string SETTING_DELETETEMP_TITLE = "Delete Temp Items";
        const string SETTING_DELETETEMP_DESC = "Are you sure you want to delete all the fetched temporary items?";
        const string SETTING_DELETESTORED_TITLE = "Delete Stored Items";
        const string SETTING_DELETESTORED_DESC = "Are you sure you want to delete all the fetched built/stored items?";

        const string CONFIRM_MSG = "Go Ahead";
        const string CANCEL_MSG = "Cancel";

        int setup_ProcessId;

        #region Unity Functions

        private void OnEnable()
        {
            InitVariables();
            InitPreferences();

            SetupPreviewUtils();

            SetupStoredItems();
            SetupTempFiles();

            SetupBaseUI();
            SetupBindings();
            SetupPatchedDropdowns();

            _generateBttn.SetEnabled(false);
            _similarSortBttn.SetEnabled(false);

            SetupCallbacks();
            SetupInputCallbacks();

            SetupPagination();
            SwitchSidePanel();
            SetupIcons();
            SetupFooterMenuItems();
            SetupCamera();

            SetupServers();

            RefreshPageIndexMessage();

            //This infinite async loop ensures that the preview's camera is always rendered
            //RepaintCycle();

            //await Task.Delay(1);
            this.Focus();
            _searchBar.Focus();

            LogMessage("Setup finished");
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
            PreviewLoadAnimFrame();
            RenderItemPreview();

        }

        #endregion Unity Functions


        private void InitVariables()
        {
            initPos = Vector3.zero;
            setup_ProcessId = 0;

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
            var quickToolVisualTree = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath(
                PathFactory.BuildUiFilePath(PathFactory.BUILDER_LAYOUT_FILE), typeof(VisualTreeAsset));
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

            //Servers Dropdown
            _serversDropContainer = _root.Q("PoolsContainer");
            _serverDropSelected = (TextElement)_root.Q("PoolSelected");
            _serverDropIcon = _root.Q("ServerDropIcon");

            //Servers Panel
            _serversListContainer = _root.Q("ServersList");
            _addServerBttn = (Button)_root.Q("AddServer");
            _removeServerBttn = (Button)_root.Q("RemoveServer");
            _saveServersBttn = (Button)_root.Q("SaveServersBttn");
            _saveServersIcon = _root.Q("SaveServersIcon");

            //Data Pages
            _mainPage = _root.Q("MainPage");
            _errorPage = _root.Q("ErrorPage");
            _errorMessage = (Label)_root.Q("Error_Subtitle");

            //Pagination
            _prevPageBttn = (Button)_root.Q("PrevPageBttn");
            _nextPageBttn = (Button)_root.Q("NextPageBttn");
            _pageIndexMssg = (TextElement)_root.Q("PageIndexMssg");

            _itemTypeIcon = _root.Q("ItemTypeIcon");
            _itemTypeSelected = (TextElement)_root.Q("ItemTypeSelected");


            //Generation Props
            _propName = (TextField)_root.Q("Prop_Name");
            _propCol = (Toggle)_root.Q("Prop_Col");
            _propRb = (Toggle)_root.Q("Prop_Rb");

            _generateBttn = (Button)_root.Q("GenerateBttn");
            _generateBttnIcon = _root.Q("GenerateBttnIcon");
            _previewLoaderText = (Label)_root.Q("PreviewLoaderT");

            _colorPallete = _root.Q("Pallete");

            _searchBttn = (Button)_root.Q("SearchBttn");
            _searchBttnIcon = _root.Q("SearchBttnIcon");

            _randomSortBttn = (Button)_root.Q("RandomSortBttn");
            _randomSortBttnIcon = _root.Q("RandomSortBttnIcon");
            _alphaSortBttn = (Button)_root.Q("AlphaSortBttn");
            _alphaSortBttnIcon = _root.Q("AlphaSortBttnIcon");
            _similarSortBttn = (Button)_root.Q("SimilarSortBttn");
            _similarSortBttnIcon = _root.Q("SimilarSortBttnIcon");


            //-----Footer Area

            //Debug Messager
            _debugMssg = (TextElement)_root.Q("Debug");

            //Settings
            _settingsIcon = _root.Q("SettingsIcon");

            //About
            _aboutIcon = _root.Q("AboutIcon");

        }


        private void SetupPatchedDropdowns()
        {
            //Servers Dropdown
            _serversDropdown = Utils.CreateDropdownField(_root.Q("PoolsList"));

            //Pagination
            _pageSizeDropdown = Utils.CreateDropdownField(_root.Q("PageSizeDropdown"));
            _itemTypeDropdown = Utils.CreateDropdownField(_root.Q("ItemTypeDropdown"));

            //Settings
            _settingdDrop = Utils.CreateDropdownField(_root.Q("SettingsDrop"));

            //About
            _aboutDrop = Utils.CreateDropdownField(_root.Q("AboutDrop"));



            ////Servers Dropdown
            //_serversDropdown = (PopupField)_root.Q("PoolsList");

            ////Pagination
            //_pageSizeDropdown = (PopupField)_root.Q("PageSizeDropdown");
            //_itemTypeDropdown = (PopupField)_root.Q("ItemTypeDropdown");

            ////Settings
            //_settingdDrop = (PopupField)_root.Q("SettingsDrop");

            ////About
            //_aboutDrop = (PopupField)_root.Q("AboutDrop")
        }


        private void SetupIcons()
        {
            _searchBttnIcon.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("d_Search Icon").image;

            _randomSortBttnIcon.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("d_UnityEditor.Graphs.AnimatorControllerTool").image;
            _similarSortBttnIcon.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("d_FilterByLabel").image;
            _alphaSortBttnIcon.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("AlphabeticalSorting").image;

            _generateBttnIcon.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("GameObject Icon").image;
            _itemTypeIcon.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("icon dropdown").image;
            _serverDropIcon.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("d_icon dropdown@2x").image;
            _saveServersIcon.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("d_SaveAs").image;
            _settingsIcon.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("d__Popup@2x").image;
            _aboutIcon.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("d_console.infoicon.sml").image;

        }

        private void SetupCallbacks()
        {
            //Server State Callbacks
            ServerManager.On404 += () => SwitchToErrorPage(NOT_FOUND_MSG);
            ServerManager.OnDisconnect += () => SwitchToErrorPage(NO_CONNECTION_MSG);
            ServerManager.OnInvalidData += () => SwitchToErrorPage(INVALID_DATA_MSG);



            //Server Choose
            _serversDropdown.RegisterValueChangedCallback(x => ServerChanged(x.newValue));

            //Servers Edit
            _addServerBttn.clicked += AddServer;
            _removeServerBttn.clicked += RemoveServer;
            _saveServersBttn.clicked += SaveEditServers;
            _serversBackBttn.clicked += () => SwitchSidePanel(true);

            //Pagination
            _prevPageBttn.clicked += () => ChangePage(false);
            _nextPageBttn.clicked += () => ChangePage(true);
            _pageSizeDropdown.RegisterValueChangedCallback(x => ChangePageSize(x.newValue));
            //Generation Props
            _propCol.RegisterValueChangedCallback(x => preferences.PropCol = x.newValue);
            _propRb.RegisterValueChangedCallback(x => preferences.PropRb = x.newValue);

            //Generation
            _generateBttn.clicked += Generate;

            _itemTypeDropdown.RegisterValueChangedCallback(ItemTypeChanged);

            //Search
            _searchBttn.clicked += Search;
            _searchBar.RegisterCallback<FocusInEvent>(OnSearchFocusIn);
            _searchBar.RegisterCallback<FocusOutEvent>(OnSearchFocusOut);
            _randomSortBttn.clicked += SortItems_Random;
            _alphaSortBttn.clicked += SortItems_Alpha;
            _similarSortBttn.clicked += SortItems_Similar;


            //Footer Menu Items
            _aboutDrop.RegisterValueChangedCallback(x => OnAboutMenuChanged(x.newValue));
            _settingdDrop.RegisterValueChangedCallback(x => OnSettingsMenuChanged(x.newValue));

            //Animations
            SetupElementHoverAnim(_randomSortBttn);
            SetupElementHoverAnim(_searchBttn);
            SetupElementHoverAnim(_alphaSortBttn);
            SetupElementHoverAnim(_generateBttn);
            SetupElementHoverAnim(_serversDropContainer);
        }

        private async void SetupItems(List<Item> items = null)
        {
            setup_ProcessId++;
            int currentProcessId = setup_ProcessId;

            string initServerId = ServerManager.GetServerPath();
            _grid.Clear();

            if (items == null)
                items = GetItemList();


            //Pagination
            currentItems = items;
            currentItemsInPage = currentItems.Skip(currentPageIndex * preferences.PageSize).Take(preferences.PageSize).ToList();

            for (int i = 0; i < currentItemsInPage.Count; i++)
            {
                //If window is destroyed - stop setting up 
                if (this == null) return;

                //If server changed mid Seup - stop setting up
                if (initServerId != ServerManager.GetServerPath()) return;

                if (setup_ProcessId != currentProcessId) return;

                if (_itemTemplate == null)
                    _itemTemplate = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath(
                        PathFactory.BuildUiFilePath(PathFactory.BUILDER_ITEM_LAYOUT_FILE), typeof(VisualTreeAsset));

                var element = _itemTemplate.CloneTree();
                SetupItem(element, currentItemsInPage[i].Id, i, PathFactory.BuildItemPath(currentItemsInPage[i].Id));
                _grid.Add(element);
                await Task.Delay(1);
            }

            RefreshPageButtonsState();
            RefreshPageIndexMessage();

            if (items == null)
                SelectDefaultItem();
        }

        private async void SetupItem(VisualElement element, string name, int index, string path)
        {
            element.name = name;
            var label = element.Query<Label>().First();
            label.text = name.Capitalize().SeparateCase();

            var button = element.Q<Button>();
            var shadow = element.Q<VisualElement>("Shadow");

            BeginItemAnimation(button, shadow);

            // var buttonIcon = button.Q(className: "quicktool-button-icon");

            Texture2D img = await GetItemThumbnail(name, path);

            var imgHolder = button.Q("Img");
            imgHolder.style.backgroundImage = img;

            var iconOverlay = button.Q("Icon");
            iconOverlay.style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("d_Valid").image;

            // Sets a basic tooltip to the button itself.
            button.tooltip = name;

            //Add Callback
            button.clicked += () => ItemSelected(name, img);

            if (HasStoredItem(name))
            {
                var overlay = button.Q("Overlay");
                overlay.visible = true;
            }
        }


        private void SwitchSidePanel(bool mainView = true)
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
            GameObject groundObj = AssetDatabase.LoadAssetAtPath(
                PathFactory.BuildMeshFilePath(PathFactory.MESHES_GROUND_FILE),
                typeof(UnityEngine.Object)) as GameObject;

            if (groundObj == null)
            {
                LogMessage(GROUNDMESHNOT_MSG, 2);
                return;
            }

            groundMesh = groundObj.GetComponent<MeshFilter>().sharedMesh;

            previewMeshes = new List<(Mesh, Material[])>();

            groundMat = AssetDatabase.LoadAssetAtPath<Material>(
              PathFactory.BuildMaterialFilePath(PathFactory.MATERIALS_GROUND_FILE));
            Shader defaultShader;
            if (UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset == null)
                defaultShader = AssetDatabase.GetBuiltinExtraResource<Shader>("Standard.shader");
            else
                defaultShader = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset.defaultShader;

            groundMat = groundMat.ConvertToDefault();
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
            previewRenderUtility.lights[0].intensity = 1.5f;
            previewRenderUtility.lights[0].bounceIntensity = 5;
            previewRenderUtility.lights[0].bounceIntensity = 5;
            previewRenderUtility.lights[0].areaSize = Vector2.one * 5;
            previewRenderUtility.lights[0].transform.forward = Vector3.down;

            previewRenderUtility.lights[1].intensity = .7f;
            previewRenderUtility.lights[1].bounceIntensity = 5;
            previewRenderUtility.lights[1].color = Color.white;
            previewRenderUtility.lights[1].areaSize = Vector2.one * 5;

            previewRenderUtility.lights[0].shadows = LightShadows.Hard;
            previewRenderUtility.lights[1].shadows = LightShadows.Hard;
             previewRenderUtility.ambientColor = Color.white;

            previewTransform.position = new Vector3(0, 10.5f, -18);
            previewRenderUtility.camera.transform.position += (new Vector3(0, -.5835f, 1) * 17);

            previewRenderUtility.camera.clearFlags = CameraClearFlags.SolidColor;
            previewRenderUtility.camera.backgroundColor = new Color(0, 0, 0, 0);

            previewTransform.eulerAngles = new Vector3(30, 0, 0);
        }


        private void RenderItemPreview()
        {
            if (previewRenderUtility == null || !renderPreview) return;

            Rect rect = _mainImg.worldBound;

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Pan);

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


            if (previewMeshes.Count > 0)
            {
                //Debug.Log("Drawing preview Mesh");
                foreach (var previewMesh in previewMeshes)
                {
                    for (int i = 0; i < previewMesh.Item2.Length; i++)
                    {
                        previewRenderUtility.DrawMesh(previewMesh.Item1, Vector3.zero, Quaternion.Euler(0, 0, 0), previewMesh.Item2[i], i);
                    }
                }
                //previewRenderUtility.lights[0].color = Color.white;
                //previewRenderUtility.lights[1].color = Color.white;
                previewRenderUtility.DrawMesh(groundMesh, previewGroundPos, Vector3.one * 1000,
                    Quaternion.Euler(270, 0, 0), groundMat, 0, new MaterialPropertyBlock(), null, false);
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
            previewRenderUtility.camera.transform.position +=
                previewRenderUtility.camera.transform.forward * 1f * factor;
            Repaint();
        }

        #endregion Item Render & Preview

        #region IO

        private void SetupInputCallbacks()
        {
            _root.RegisterCallback<KeyDownEvent>(OnKeyboardKeyDown, TrickleDown.TrickleDown);
            _root.RegisterCallback<MouseDownEvent>(OnMouseKeyDown, TrickleDown.TrickleDown);
            _root.RegisterCallback<MouseUpEvent>(OnMouseKeyUp, TrickleDown.TrickleDown);
            _root.RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
            _root.RegisterCallback<WheelEvent>(OnMouseWheelDown, TrickleDown.TrickleDown);
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (isRotatingPrev || isTranslatingPrev)
            {
                var posDif = evt.mousePosition - lastMousePos;
                if (lastMousePos.x != 0)
                {
                    if (isRotatingPrev)
                    {
                        previewRenderUtility.camera.transform.RotateAround(previewBounds.center,
                            previewRenderUtility.camera.transform.right, posDif.y * 8 * Time.deltaTime);
                        previewRenderUtility.camera.transform.RotateAround(previewBounds.center, Vector3.up,
                            posDif.x * 8 * Time.deltaTime);


                        previewRenderUtility.lights[0].transform.RotateAround(previewBounds.center,
                            previewRenderUtility.camera.transform.right, posDif.y * 8 * Time.deltaTime);
                        previewRenderUtility.lights[0].transform.RotateAround(previewBounds.center, Vector3.up,
                            posDif.x * 8 * Time.deltaTime);

                        previewRenderUtility.lights[1].transform.RotateAround(previewBounds.center,
                            previewRenderUtility.camera.transform.right, posDif.y * 8 * Time.deltaTime);
                        previewRenderUtility.lights[1].transform.RotateAround(previewBounds.center, Vector3.up,
                            posDif.x * 8 * Time.deltaTime);

                        //previewRotation += new Vector3(posDif.y * 8 * Time.deltaTime, 0, posDif.x * 8 * Time.deltaTime);
                    }

                    if (isTranslatingPrev)
                    {
                        previewRenderUtility.camera.transform.position -= previewRenderUtility.camera.transform.right *
                                                                          posDif.x * 0.07f * Time.deltaTime;
                        previewRenderUtility.camera.transform.position += previewRenderUtility.camera.transform.up *
                                                                          posDif.y * 0.07f * Time.deltaTime;
                    }
                    Repaint();
                }

                lastMousePos = evt.mousePosition;
            }
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

            else if ((e.keyCode == KeyCode.Return) && selectedFile != "")
            {
                Generate();
            }
        }

        private void OnMouseKeyDown(MouseDownEvent e)
        {
            if ((e.button == 0 || e.button == 2) &&
                CheckIfMouseOverSreen(_mainImg.worldBound, true, new Vector2(0, 20)))
            {
                if (e.button == 0)
                    isRotatingPrev = true;

                if (e.button == 2)
                    isTranslatingPrev = true;

                lastMousePos = Vector2.zero;
            }
        }

        private void OnMouseKeyUp(MouseUpEvent e)
        {
            if (e.button == 0 && isRotatingPrev)
                isRotatingPrev = false;

            if (e.button == 2 && isTranslatingPrev)
                isTranslatingPrev = false;
        }

        private void OnMouseWheelDown(WheelEvent e)
        {
            if (e.delta == Vector3.zero) return;


            if (CheckIfMouseOverSreen(_mainImg.worldBound, true, new Vector2(0, 20)))
            {
                Zoom(e.delta.y);
            }
        }

        private static bool CheckIfMouseOverSreen(Rect r, bool relativePos = true, Vector2? offset = null)
        {
            Vector2 mousePos = relativePos
                ? Event.current.mousePosition
                : GUIUtility.GUIToScreenPoint(Event.current.mousePosition);

            var xMin = r.xMin + (offset.HasValue ? offset.Value.x : 0);
            var xMax = r.xMax + (offset.HasValue ? offset.Value.x : 0);
            var yMin = r.yMin + (offset.HasValue ? offset.Value.y : 0);
            var yMax = r.yMax + (offset.HasValue ? offset.Value.y : 0);

            var condition = mousePos.x >= xMin && mousePos.x <= xMax &&
                            mousePos.y >= yMin && mousePos.y <= yMax;

            return condition;
        }

        #endregion IO

        #region Items Info

        private List<Item> GetItemList()
        {
            if (onlyLocalSearch)
                return storedData.Items;

            return ServerManager.data.Items;
        }

        private async Task<string> GetItem(string itemId, string itemTypeId)
        {
            var filename = $"{itemId}_{itemTypeId}";
            selectedFile = $"{filename}.{PathFactory.MESH_TYPE}";

            bool storedResult = HasStoredItem(itemId, itemTypeId);
            bool tempResult = HasTempFile(selectedFile);

            string path;

            if (storedResult)
            {
                path = $"{PathFactory.relativeToolPath}/{PathFactory.STORED_ITEMS_PATH}/{selectedFile}";
            }
            //If Stored file does not exist
            else
            {
                //If Temp file does not exist
                if (!tempResult)
                {
                    LogMessage("Fetching item name" + filename);

                    //Fetch File from server and add to Temp list
                    await ServerManager.GetRawFile(PathFactory.BuildItemPath(selectedItemId),
                        PathFactory.TEMP_ITEMS_PATH, filename, PathFactory.MESH_TYPE);
                    AssetDatabase.Refresh();
                    AddTempFile(selectedFile);
                }

                path = $"{PathFactory.relativeToolPath}/{PathFactory.TEMP_ITEMS_PATH}/{selectedFile}";
            }

            return path;
        }

        private async Task<Texture2D> GetItemThumbnail(string itemId, string path)
        {
            //If online search - fetch from server
            if (!onlyLocalSearch)
            {
                return await ServerManager.GetImage(path, null, PathFactory.THUMBNAIL_FILE);
            }

            //Else - fetch from local thumbnails folder


            var localPath =
                $"{PathFactory.absoluteToolPath}\\{PathFactory.STORED_THUMB_PATH}\\{itemId}.{PathFactory.THUMBNAIL_TYPE}";
            localPath = localPath.AbsoluteFormat();

            if (!File.Exists(localPath)) return null;

            byte[] imageBytes = File.ReadAllBytes(localPath);

            Texture2D image = new Texture2D(2, 2);
            image.LoadImage(imageBytes);

            return image;
        }

        #endregion Items Info

        #region Stored Items

        private bool HasStoredItem(string itemId, string itemTypeId = null)
        {
            if (itemTypeId == null)
                return storedData.Items.Where(x => x.Id == itemId).Any();

            return storedData.Items.Where(x => x.Id == itemId && x.TypeIds.Contains(itemTypeId)).Any();
        }

        private Item GetStoredItem(string itemId)
        {
            return storedData.Items.Where(x => x.Id == itemId).FirstOrDefault();
        }

        private void AddStoredItem(string file)
        {
            (string id, string typeId) = PathFactory.GetItemIds(file);

            if (!HasStoredItem(id))
                storedData.Items.Add(new Item() { Id = id });

            var item = GetStoredItem(id);

            if (item.TypeIds == null)
                item.TypeIds = new List<string>();

            //Add TypeId
            item.TypeIds.Add(typeId);

        }

        private void SetupStoredItems()
        {
            storedData = new ServerData();
            storedData.Items = new List<Item>();

            string tmpPath = $"{PathFactory.absoluteToolPath}\\{PathFactory.STORED_ITEMS_PATH}";

            var files = Utils.GetFiles(tmpPath, true);

            foreach (var file in files)
                AddStoredItem(file);
        }

        private void RemoveStoredItem(string file)
        {
            var filePath = $"{PathFactory.absoluteToolPath}\\{PathFactory.STORED_ITEMS_PATH}\\{file}";
            filePath = filePath.AbsoluteFormat();

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                File.Delete(filePath + ".meta");
            }

            var item = storedData.Items.FirstOrDefault(x => file.StartsWith(x.Id));

            if (item != null)
                storedData.Items.Remove(item);

        }

        private void RemoveAllStoredItems()
        {
            var numStoredItems = storedData.Items.Count;
            for (int i = 0; i < numStoredItems; i++)
            {
                var numTypeIds = storedData.Items[0].TypeIds.Count;
                for (int j = 0; j < numTypeIds; j++)
                    RemoveStoredItem(PathFactory.BuildItemFile(storedData.Items[0].Id, storedData.Items[0].TypeIds[0]));
            }
        }

        #endregion Stored Items

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

        private bool HasTempFile(string file)
        {
            bool hasFile = tempFiles.Contains(file);
            //int index = hasFile ? tempItems.IndexOf(id) : -1;

            return hasFile;
        }

        private void AddTempFile(string file)
        {
            // If exceeds the buffer limit - delete the 1st element
            if (tempFiles.Count >= tempArraySize)
                RemoveTempFile(tempFiles[0]);

            tempFiles.Add(file);
        }

        private void RemoveTempFile(string file)
        {
            var filePath = $"{PathFactory.absoluteToolPath}\\{PathFactory.TEMP_ITEMS_PATH}\\{file}";
            filePath = filePath.AbsoluteFormat();

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                File.Delete(filePath + ".meta");
            }

            tempFiles.Remove(file);
        }

        private void RemoveAllTempFiles()
        {
            var numTempFiles = tempFiles.Count;
            for (int i = 0; i < numTempFiles; i++)
                RemoveTempFile(tempFiles[0]);
        }

        private void StoreTempFile(string file)
        {
            var dir = $"{Application.dataPath}/{PathFactory.STORED_ITEMS_PATH}";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            AssetDatabase.MoveAsset($"{PathFactory.relativeToolPath}/{PathFactory.TEMP_ITEMS_PATH}/{file}",
                $"{PathFactory.relativeToolPath}/{PathFactory.STORED_ITEMS_PATH}/{file}");

            AddStoredItem(file);
            RemoveTempFile(file);
        }

        #endregion Temporary Items Buffer


        #region Items Sort

        private void SortItems_Random()
        {
            List<Item> items = GetItemList();
            System.Random rng = new System.Random(Utils.GetRandomSeed());
            items = items.OrderBy(a => rng.Next()).ToList();
            SetupItems(items);
        }


        private void SortItems_Alpha()
        {
            List<Item> items = GetItemList();
            items = items.OrderBy(a => a.Id).ToList();
            SetupItems(items);
        }

        private void SortItems_Similar()
        {
            //If no selected Item - Cancel sort
            if (selectedItemId == null) return;

            List<Item> items = GetItemList();

            var selectedItem = items.FirstOrDefault(x => x.Id == selectedItemId);

            //If no selected Item in List - Cancel sort
            if (selectedItem == null) return;

            //Combine all tags in one string to fit the Search Engine params (ex. tag1 tag2 tag3)
            string combinedTags = "";
            foreach (var tag in selectedItem.Tags)
            {
                combinedTags += $"{tag} ";
            }

            //If no tags - Cancel sort
            if (combinedTags.Length <= 0) return;

            //Remove last space char
            combinedTags = combinedTags.Remove(combinedTags.Length - 1);

            items = items.OrderByDescending(x => SearchEngine.WordMatch_Tag(x, combinedTags)).ToList();
            SetupItems(items);
        }


        #endregion Items Sort


        #region Items Search

        private void Search()
        {
            List<Item> items = GetItemList();
            ResetPage();

            var searchVal = _searchBar.value;

            if (String.IsNullOrWhiteSpace(searchVal))
                SetupItems();
            else
            {
                var searchedList = SearchEngine.FuzzySearch(items, searchVal);
                SetupItems(searchedList);
            }
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

        private void SetupElementHoverAnim(VisualElement button)
        {
            button.RegisterCallback<MouseOverEvent>(x => OnButtonHover(button));
            button.RegisterCallback<MouseOutEvent>(x => OnButtonHoverExit(button));
        }
        private void OnButtonHover(VisualElement element)
        {
            element.style.backgroundColor = activeButtonColor;
        }

        private void OnButtonHoverExit(VisualElement element)
        {
            element.style.backgroundColor = defaultButtonColor;
        }

        private async void BeginItemAnimation(Button button, VisualElement shadow)
        {
            button.style.opacity = 0;
            shadow.style.opacity = 0;

            await Task.Delay(50);
            button.style.opacity = 1;
            shadow.style.opacity = 1;
        }


        private void PreviewLoadAnimFrame()
        {
            if (!isLoadingPrev)
            {
                _previewLoaderText.visible = false;
                return;
            }

            loadingPrevFrame++;
            _previewLoaderText.visible = true;

            if ((_previewLoaderText.text == "..." || _previewLoaderText.text == "") && loadingPrevFrame > loadingPrevFrameInterval)
            {
                _previewLoaderText.text = ".";
                loadingPrevFrame = 0;
            }
            else if (_previewLoaderText.text == "." && loadingPrevFrame > loadingPrevFrameInterval)
            {
                _previewLoaderText.text = "..";
                loadingPrevFrame = 0;
            }
            else if (_previewLoaderText.text == ".." && loadingPrevFrame > loadingPrevFrameInterval)
            {
                _previewLoaderText.text = "...";
                loadingPrevFrame = 0;
            }
        }


        #endregion Animation

        #region Object Generation

        private async void Generate()
        {
            StoreTempFile(selectedFile);
            var gObj = AssetDatabase.LoadAssetAtPath(
                $"{PathFactory.relativeToolPath}/{PathFactory.STORED_ITEMS_PATH}/{selectedFile}",
                typeof(UnityEngine.Object)) as GameObject;

            if (gObj == null) return;
            var newObj = GameObject.Instantiate(gObj);


            newObj.name = _propName.value;

            if (_propCol.value)
            {
                var meshCollider = newObj.AddComponent<MeshCollider>();
                meshCollider.convex = _propRb.value;
            }

            if (_propRb.value)
                newObj.AddComponent<Rigidbody>();

            Selection.activeGameObject = newObj;

            PlaceObjectInView(newObj);


            //Save Thumbnail of Image if not already saved
            var localPath =
                $"{PathFactory.absoluteToolPath}\\{PathFactory.STORED_THUMB_PATH}\\{selectedItemId}.{PathFactory.THUMBNAIL_TYPE}";
            if (!File.Exists(localPath))
                await ServerManager.GetImage(PathFactory.BuildItemPath(selectedItemId), localPath,
                    PathFactory.THUMBNAIL_FILE);

            Close();
        }

        private void PlaceObjectInView(GameObject gObject)
        {
            var gMesh = gObject.GetComponent<MeshRenderer>();
            //test.position=  sceneWindow.camera.ScreenToWorldPoint(mPos)+ sceneWindow.camera.transform.forward*2;
            var hits = Physics.RaycastAll(MainController.sceneWindow.camera.transform.position,
                MainController.sceneWindow.camera.transform.forward);
            Vector3? position = null, upVector = null;
            foreach (var hit in hits)
            {
                if (hit.transform.GetInstanceID() == gObject.transform.GetInstanceID()) continue;
                //position = hit.point;
                position = hit.point + new Vector3(0, gMesh.bounds.extents.y, 0) - gMesh.bounds.center;
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

        #region Data Pages
        private void SwitchToMainPage()
        {
            _mainPage.style.display = DisplayStyle.Flex;
            _errorPage.style.display = DisplayStyle.None;

        }

        private void SwitchToErrorPage(string message)
        {
            _grid.Clear();
            _errorMessage.text = message;
            _mainPage.style.display = DisplayStyle.None;
            _errorPage.style.display = DisplayStyle.Flex;
        }

        #endregion Data Pages

        #region Colour Picker

        private VisualElement CreateColorPicker(Material material)
        {
            var field = new ColorField();
            //field.hdr = true;
            field.style.width = 60;
            field.value = material.color;
            field.RegisterValueChangedCallback((x) => ColorChanged(material, x.newValue));

            var container = new VisualElement();
            container.Add(field);
            container.style.width = 20;
            container.style.height = 14;
            //container.style.marginRight = 5;
            container.style.overflow = Overflow.Hidden;

            var borderRadius = 10;
            //StyleScale s = new Scale(new Vector3(1, 1.2f, 1));
            //container.style.scale = s;
            container.style.borderBottomLeftRadius = borderRadius;
            container.style.borderBottomRightRadius = borderRadius;
            container.style.borderTopLeftRadius = borderRadius;
            container.style.borderTopRightRadius = borderRadius;
            return container;
        }

        private void ColorChanged(Material material, Color color)
        {
            //If no material with the specified name in the preview Meshes is registered - return
            if (!previewMaterials.Where(x => x.name == material.name).Any()) return;

            Material selectedMat = previewMaterials.Where(x => x.name == material.name).First();
            selectedMat.color = color;
        }

        #endregion Colour Picker

        #region Pagination

        private void SetupPagination()
        {
            currentPageIndex = 0;

            string prefValue = preferences.PageSize == 0 ? "20" : preferences.PageSize.ToString();

            List<string> pageSizes = new List<string>();
            pageSizes.Add("20");
            pageSizes.Add("50");
            pageSizes.Add("100");
            pageSizes.Add("200");
            if (!pageSizes.Contains(prefValue))
                pageSizes.Add(prefValue);

            _pageSizeDropdown.choices = pageSizes;
            _pageSizeDropdown.value = prefValue;
        }

        private void ChangePage(bool next)
        {
            currentPageIndex += next ? 1 : -1;
            SetupItems();
        }

        private void ResetPage()
        {
            currentPageIndex = 0;
            RefreshPageButtonsState();
        }

        private void ChangePageSize(string newSize)
        {
            int newSizeValue = int.Parse(newSize);
            preferences.PageSize = newSizeValue;

            currentPageIndex = 0;

            RefreshPageButtonsState();
            SetupItems();
            RefreshPageIndexMessage();
            UpdatePreferences();
        }

        private void RefreshPageButtonsState()
        {
            var totalItems = currentItems.Count;
            _prevPageBttn.SetEnabled(currentPageIndex - 1 >= 0);
            _nextPageBttn.SetEnabled((currentPageIndex + 1) * preferences.PageSize < totalItems);
        }

        private void RefreshPageIndexMessage()
        {
            if (currentItems == null) return;

            int totalResults = currentItems.Count;
            int initIndex = currentPageIndex * preferences.PageSize;
            int lastIndex = Mathf.Min(initIndex + preferences.PageSize, totalResults);
            _pageIndexMssg.text = $"{initIndex}-{lastIndex} of {totalResults} Results";
        }

        #endregion Pagination

        #region Item & Type Selected

        private void ItemSelected(string itemId, Texture2D icon = null, bool manualTypeSelect = false)
        {
            if (selectedItemId == itemId) return;

            _generateBttn.SetEnabled(false);
            _similarSortBttn.SetEnabled(true);


            List<string> choices;
            List<Item> items = GetItemList();



            preferences.LastItem = itemId;
            selectedItemId = itemId;

            choices = items.Where(x => x.Id == itemId).FirstOrDefault().TypeIds;


            _mainImg.style.backgroundImage = icon;
            _mainTitle.text = itemId.Capitalize().SeparateCase();

            _propRb.value = preferences.PropRb;
            _propCol.value = preferences.PropCol;



            for (int i = 0; i < choices.Count; i++)
            {
                if (HasStoredItem(itemId, choices[i]))
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
            isLoadingPrev = true;

            //Reset Mesh Preview
            previewObj = null;
            previewMeshes.Clear();


            _itemTypeSelected.text = value;
            _propName.value = $"{value.Capitalize()} {selectedItemId.Capitalize()}";

            string objPath = await GetItem(selectedItemId, value);

            previewObj = AssetDatabase.LoadAssetAtPath(objPath, typeof(UnityEngine.Object)) as GameObject;

            if (previewObj == null)
            {
                LogMessage($"Item {value} couldn't be loaded", 2);
                return;
            }


            var previewMeshFilters = previewObj.GetComponentsInChildren<MeshFilter>();

            if (previewMeshFilters.Length == 0) return;


            foreach (var meshFilter in previewMeshFilters)
            {
                var renderer = meshFilter.gameObject.GetComponent<MeshRenderer>();
                if (renderer == null) continue;

                previewMeshes.Add((meshFilter.sharedMesh, renderer.sharedMaterials));

            }


            previewMaterials = new List<Material>();

            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.zero;
            Vector3 center = Vector3.zero;
            previewBounds.center = Vector3.zero;

            foreach (var previewMesh in previewMeshes)
            {
                foreach (var material in previewMesh.Item2)
                {
                    // Skipping duplicate materials
                    if (!previewMaterials.Contains(material))
                    {
                        ////Make each material not glossy

                        //material.SetColor("_EmissionColor", material.color);

                        //URP 2020
                        material.SetFloat("_Glossiness", 0f);

                        //URP 2021
                        material.SetFloat("_Smoothness", 0f);
                        material.SetFloat("_Metallic", 0f);

                        //Add Separate List with unique materials throughout the object's meshes
                        previewMaterials.Add(material);
                    }
                }
                center += previewMesh.Item1.bounds.center;

                min = Vector3.Min(previewMesh.Item1.bounds.min, min);
                max = Vector3.Max(previewMesh.Item1.bounds.max, max);
            }

            //Get average center point
            previewBounds.center = center / previewMeshes.Count;
            previewBounds.min = min;
            previewBounds.max = max;

            //Add respective color pickers
            _colorPallete.Clear();
            foreach (var material in previewMaterials)
            {
                var field = CreateColorPicker(material);
                _colorPallete.Add(field);
            }

            previewGroundPos = new Vector3(previewBounds.center.x, previewBounds.min.y - 0.01f, previewBounds.center.z);

            var objectSizes = previewBounds.max - previewBounds.min;
            var objectSize = Mathf.Max(objectSizes.x, objectSizes.y, objectSizes.z);



            if (previewRenderUtility == null || previewRenderUtility.camera == null) return;

            // Visible height 1 meter in front
            var cameraView = 2.0f * Mathf.Tan(0.5f * Mathf.Deg2Rad * previewRenderUtility.camera.fieldOfView);
            // Combined wanted distance from the object (1 is the distance factor)
            var distance = 1 * objectSize / cameraView;
            // Estimated offset from the center to the outside of the object
            distance += 0.5f * objectSize;


            previewRenderUtility.camera.transform.RotateAround(previewBounds.center, Vector3.up, Time.deltaTime);
            await Task.Delay(3);

            _generateBttn.SetEnabled(true);

            //If camera has been destroyed while waiting - return
            if (previewRenderUtility == null || previewRenderUtility.camera == null) return;

            var targetPosition = previewBounds.center - distance * previewRenderUtility.camera.transform.forward;
            previewRenderUtility.camera.transform.position = targetPosition;


            //Slight delay to show loading status
            await Task.Delay(100);
            isLoadingPrev = false;
        }


        private void SelectDefaultItem()
        {
            if (preferences.LastItem != null && preferences.LastItemType != null)
            {
                List<Item> items = GetItemList();

                var lastItem = items.Where(x => x.Id == preferences.LastItem).FirstOrDefault();
                if (lastItem == null)
                {
                    LogMessage("Last selected Item Id is not in this library", 1);
                    return;
                }

                if (!lastItem.TypeIds.Where(x => x == preferences.LastItemType).Any())
                {
                    LogMessage("Last selected Item Type Id is not in this library", 1);
                    return;
                }

                //Manual selection set to 'true' to avoid auto selecting a typeId 
                ItemSelected(preferences.LastItem, null, true);
                ItemTypeSelected(preferences.LastItemType);
            }
        }

        #endregion Item & Type Selected

        #region Manage Servers

        private void SetupServers()
        {
            serverList = new Dictionary<string, Server>();

            serverList.Add(LOCALPOOL_MSG, null);

            //If Server preferences are not empty
            if (preferences.ServersSrc != null && preferences.ServersId != null)
            {
                //Load Servers from preferences
                for (int i = 0; i < preferences.ServersId.Count; i++)
                {
                    var server = ServerManager.CreateServer(preferences.Servers_Type[i], preferences.ServersSrc[i],
                        preferences.ServersBranch[i]);

                    serverList.Add(preferences.ServersId[i], server);
                }
            }
            else
            {
                ////Add Default server
                var server =
                    ServerManager.CreateServer(ServerType.GIT, Server_Git.defaultRepo, Server_Git.defaultBranch);

                //Add preferences entry
                preferences.ServersId = new List<string>() { MAINPOOL_MSG };
                preferences.Servers_Type = new List<ServerType>() { ServerType.GIT };
                preferences.ServersSrc = new List<string> { Server_Git.defaultRepo };
                preferences.ServersBranch = new List<string> { Server_Git.defaultBranch };

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

            //Have to manually Trigger Server Change (bug)
            ServerChanged(preferences.LastServer);
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
            foreach (var item in _serversListContainer.Children())
            {
                var idField = (TextField)item.Q("Id");
                var typeField = Utils.CreateDropdownField(item.Q("Type"));
                var srcField = (TextField)item.Q("Src");
                var branchField = (TextField)item.Q("Branch");


                //If the element with the same Id 
                if (preferences.ServersId.Contains(idField.value))
                {
                    var sameId = preferences.ServersId.IndexOf(idField.value);

                    //And with same Src - do not add to the list
                    if (preferences.ServersSrc[sameId] == srcField.value)
                        continue;

                    //In case not change Src for the item Id
                    preferences.ServersSrc[sameId] = srcField.value;
                    continue;
                }

                preferences.ServersId.Add(idField.value);
                preferences.ServersSrc.Add(srcField.value);
                preferences.Servers_Type.Add((ServerType)Enum.Parse(typeof(ServerType), typeField.value));

                //If Server is Type GIT - Add branch
                preferences.ServersBranch.Add(
                    typeField.value == ServerType.GIT.ToString() ? branchField.value : null
                );
            }

            UpdatePreferences();
            SetupServers();
        }

        private void AddServer()
        {
            CreateServerEntry(ServerType.GIT, null, null);
        }

        private void CreateServerEntry(ServerType type, string id = null, string src = null, string branch = null)
        {
            if (_serverTemplate == null)
                _serverTemplate = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath(
                    PathFactory.BuildUiFilePath(PathFactory.BUILDER_SERVER_ITEM_FILE), typeof(VisualTreeAsset));

            var element = _serverTemplate.CloneTree();

            var idField = (TextField)element.Q("Id");
            if (id != null) idField.value = id;
            idField.RegisterCallback<FocusEvent>((x) => { _selectedListServer = element; });

            var branchField = (TextField)element.Q("Branch");
            if (branch != null) branchField.value = branch;

            var typesField = Utils.CreateDropdownField(element.Q("Type"));
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


        private async void ServerChanged(string newServer)
        {
            if (newServer == NEWPOOL_MSG)
            {
                SwitchSidePanel(false);
                return;
            }

            if (newServer != LOCALPOOL_MSG)
            {
                if (!serverList.ContainsKey(newServer)) return;

                Server currentServer = serverList[newServer];
                ServerManager.SetServer(currentServer);
                var result = await ServerManager.FetchServerData();
                if (!result)
                {
                    return;
                    //Debug.Log()
                }
            }


            SwitchToMainPage();
            SwitchSidePanel(true);
            _serverDropSelected.text = newServer;


            onlyLocalSearch = newServer == LOCALPOOL_MSG;
            SetupItems();

            preferences.LastServer = newServer;
            UpdatePreferences();
        }

        #endregion Manage Servers

        #region Footer Menu Items

        private void SetupFooterMenuItems()
        {

            //About
            List<string> footerAboutMenuItems = new List<string>();
            footerAboutMenuItems.Add(ABOUT_TOOL_REPO_MSG);
            footerAboutMenuItems.Add(ABOUT_LIB_REPO_MSG);
            footerAboutMenuItems.Add(ABOUT_REPORT_MSG);
            footerAboutMenuItems.Add(ABOUT_DONATE_MSG);
            _aboutDrop.choices = footerAboutMenuItems;


            //Settings
            List<string> footerSettingsMenuItems = new List<string>();
            footerSettingsMenuItems.Add(SETTINGS_RESETPREF_MSG);
            footerSettingsMenuItems.Add(SETTINGS_CLEARTEMP_MSG);
            footerSettingsMenuItems.Add(SETTINGS_CLEARSTORED_MSG);
            _settingdDrop.choices = footerSettingsMenuItems;


        }
        private void OnAboutMenuChanged(string option)
        {
            if (option == "") return;

            switch (option)
            {
                case ABOUT_TOOL_REPO_MSG:
                    Application.OpenURL("https://github.com/wafflesgama/LazyBuilder");
                    break;

                case ABOUT_LIB_REPO_MSG:
                    Application.OpenURL("https://github.com/wafflesgama/LazyBuilderLibrary");
                    break;

                case ABOUT_REPORT_MSG:
                    Application.OpenURL("https://github.com/wafflesgama/LazyBuilder/issues");
                    break;

                case ABOUT_DONATE_MSG:
                    Application.OpenURL("https://www.buymeacoffee.com/guilhermeGama");
                    break;
            }

            _aboutDrop.value = "";


        }
        private void OnSettingsMenuChanged(string option)
        {
            switch (option)
            {
                case SETTINGS_RESETPREF_MSG:
                    if (EditorUtility.DisplayDialog(SETTING_RESETPREF_TITLE, SETTING_RESETPREF_DESC, CONFIRM_MSG, CANCEL_MSG))
                        ResetPreferences();
                    break;

                case SETTINGS_CLEARTEMP_MSG:
                    if (EditorUtility.DisplayDialog(SETTING_DELETETEMP_TITLE, SETTING_DELETETEMP_DESC, CONFIRM_MSG, CANCEL_MSG))
                    {
                        RemoveAllTempFiles();
                        AssetDatabase.Refresh();
                    }
                    break;

                case SETTINGS_CLEARSTORED_MSG:
                    if (EditorUtility.DisplayDialog(SETTING_DELETESTORED_TITLE, SETTING_DELETESTORED_DESC, CONFIRM_MSG, CANCEL_MSG))
                    {
                        RemoveAllStoredItems();
                        AssetDatabase.Refresh();
                    }
                    break;
            }

            _settingdDrop.value = "";
        }


        #endregion Footer Menu Items

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

        private void ResetPreferences()
        {
            preferences.SetDefaultValues();
            UpdatePreferences();
        }

        #endregion Preferences

        #region Utils

        /// <summary>
        /// Logs Message to the footer panel, msg fades after some seconds
        /// </summary>
        /// <param name="message">The displayed message</param>
        /// <param name="messageLevel">0-Info,1-Warning, 2-Error</param>
        private async void LogMessage(string message, int messageLevel = 0)
        {
            _debugMssg.style.opacity = 1;
            _debugMssg.text = message;
            Color color = messageLevel switch
            {
                1 => Color.yellow,
                2 => Color.red,
                _ => Color.white
            };
            _debugMssg.style.color = color;


            for (int i = 0; i < 1000; i++)
            {
                await Task.Delay(5);
                if (_debugMssg.text != message) return;

                var subval = i * 0.01f;
                if (subval > 1) break;

                _debugMssg.style.opacity = 1 - subval;
            }

            _debugMssg.text = "";
        }

        #endregion Utils
    }
}