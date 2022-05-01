using Newtonsoft.Json;
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

namespace LazyBuilder
{

    public class MainController : EditorWindow
    {


        private ServerData lazyData;

        private string appPath;
        private string basePath;

        private string lastSessionItem;
        private string lastSessionItemType;
        private bool lastSessionItemFlag;

        private string selectedPool;
        private int selectedPoolIndex;
        private string selectedItem;
        private string selectedFile;

        private PreviewRenderUtility previewRenderUtility;
        private Transform previewTransform;

        private VisualElement _root;

        private TextField _searchBar;

        private DropdownField _poolsDropdown;
        private VisualElement _poolsContainer;
        private TextElement _poolSelected;


        private VisualElement _itemTypeIcon;
        private DropdownField _itemTypeDropdown;
        private TextElement _itemTypeSelected;

        private TextElement _tabTitle;
        private VisualElement _tabImg;

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

        private Mesh groundMesh;
        private Material groundMat;
        //private Material fallbackMat;

        private GameObject previewObj;
        private Mesh previewMesh;
        private Vector3 previewGroundPos;



        private List<string> tempItems;
        private const int tempArraySize = 5;


        private Vector3 initPos;
        bool setDist;
        bool isSearchFocused;

        Material[] previewMats;
        List<Color> previewColors;

        static SceneView sceneWindow;

        const string TEMP_ITEMS_PATH = "LazyBuilder/Data/Temp/Items";
        const string STORED_ITEMS_PATH = "LazyBuilder/Data/Items";
        const string UI_PATH = "LazyBuilder/Data/UI";
        const string MESHES_PATH = "LazyBuilder/Data/Meshes";
        const string MATERIALS_PATH = "LazyBuilder/Data/Materials";

        const string NEWPOOL_MSG = "Add new pool...";

        private VisualTreeAsset _itemTemplate;

        [MenuItem("Tools/Lazy Builder/Build #b")]
        private static void LazyBuildWindow()
        {
            sceneWindow = GetWindow<SceneView>();
            var window = GetWindow<MainController>();
            window.titleContent = new GUIContent("Lazy Build");
            window.Show();


            //var mRelPos = Event.current.mousePosition;

            //var mPos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);

            //Debug.Log($"Mouse position: {mPos.x},{mPos.y}");

            ////Debug.Log($"Scene view position: {sceneWindow.position.xMin},{sceneWindow.position.yMin} - {sceneWindow.position.xMax},{sceneWindow.position.yMax}");

            //var condition = mPos.x >= sceneWindow.position.xMin && mPos.x <= sceneWindow.position.xMax &&
            //                 mPos.y >= sceneWindow.position.yMin && mPos.y <= sceneWindow.position.yMax;

            //Debug.LogWarning($"Is mouse over scene? {condition}");

        }

        private void PlaceObjectInView(GameObject gObject)
        {
            var gMesh = gObject.GetComponent<MeshRenderer>();
            //test.position=  sceneWindow.camera.ScreenToWorldPoint(mPos)+ sceneWindow.camera.transform.forward*2;
            var hits = Physics.RaycastAll(sceneWindow.camera.transform.position, sceneWindow.camera.transform.forward);
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
                position = sceneWindow.camera.transform.forward * 1.5f;

            if (!upVector.HasValue)
                upVector = Vector3.up;

            gObject.transform.position = position.Value;
            //gObject.transform.position = Vector3.zero;
            //gObject.transform.position = sceneWindow.camera.transform.position;
            //gObject.transform.localPosition -= gMesh.localBounds.center;
            gObject.transform.up = upVector.Value;
        }

        [MenuItem("Tools/Lazy Builder/Lazy Data Manager")]
        private static void LazyManageWindow()
        {

        }


        private async void OnEnable()
        {
            InitVariables();


            SetupPreviewUtils();

            GetTmpItems();
            SetupBaseUI();
            SetupBindings();
            SetupCallbacks();
            SetupInputCallbacks();

            SetupIcons();
            SetupCamera();

            await FetchLazyData();
            SetupItems(lazyData.Pools[selectedPoolIndex].Items);


            //This infinite async loop ensures that the preview's camera is always rendered
            RepaintCycle();

            Debug.Log("Searcbar focus");
            this.Focus();
            _searchBar.Focus();
        }

        private void InitVariables()
        {
            initPos = Vector3.zero;
            defaultButtonColor = new Color(0.345098f, 0.345098f, 0.345098f, 1);
            activeButtonColor = new Color(0.27f, 0.38f, 0.49f);

            appPath = Path.GetDirectoryName(Application.dataPath);

            var currentPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this)));
            basePath = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(currentPath)));
        }

        private void SetupPreviewUtils()
        {
            GameObject groundObj = AssetDatabase.LoadAssetAtPath($"{basePath}/{MESHES_PATH}/ground.fbx", typeof(UnityEngine.Object)) as GameObject;

            if (groundObj == null)
            {
                Debug.LogError("Ground Mesh couldn't be fetched");
                return;
            }
            groundMesh = groundObj.GetComponent<MeshFilter>().sharedMesh;
            groundMat = AssetDatabase.LoadAssetAtPath<Material>($"{basePath}/{MATERIALS_PATH}/groundMat.mat");
        }

        private void OnDisable()
        {
            previewRenderUtility?.Cleanup();
        }
        private void OnDestroy()
        {
            previewRenderUtility?.Cleanup();
        }

        private void SetupInputCallbacks()
        {
            _root.RegisterCallback<KeyDownEvent>(OnKeyboardKeyDown, TrickleDown.TrickleDown);
            _root.RegisterCallback<MouseDownEvent>(OnMouseKeyDown, TrickleDown.TrickleDown);
            _root.RegisterCallback<WheelEvent>(OnMouseWheelDown, TrickleDown.TrickleDown);
        }

        private void OnKeyboardKeyDown(KeyDownEvent e)
        {
            Debug.Log($"Key down {e.keyCode}");

            if (e.keyCode == KeyCode.Escape)
                this.Close();

            else if (e.keyCode == KeyCode.Return && isSearchFocused)
            {
                Search();
                _searchBar.Focus();
            }

            else if (e.keyCode == KeyCode.Return && selectedItem != "")
            {
                Generate();
            }
        }

        private void OnMouseKeyDown(MouseDownEvent e)
        {
            Debug.Log($"On mouse down {e.button}");
        }

        private void OnMouseWheelDown(WheelEvent e)
        {
            if (e.delta == Vector3.zero) return;

            Debug.Log($"On mouse wheel scroll {e.delta}");

            if (CheckIfMouseOverSreen(_tabImg.worldBound, true, new Vector2(0, 20)))
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

        private void SetupBindings()
        {
            _tabTitle = (TextElement)_root.Q("TitleText");
            _tabImg = _root.Q("ItemThumbnail");
            _searchBar = (TextField)_root.Q("SearchBar");
            _grid = _root.Q("ItemsColumn1");

            _generateBttn = (Button)_root.Q("GenerateBttn");
            _generateBttnIcon = _root.Q("GenerateBttnIcon");

            _zoomInBttn = (Button)_root.Q("ZoomIn");
            _zoomOutBttn = (Button)_root.Q("ZoomOut");

            _poolsContainer = _root.Q("PoolsContainer");
            _poolsDropdown = (DropdownField)_root.Q("PoolsList");
            _poolSelected = (TextElement)_root.Q("PoolSelected");

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
        }

        private void SetupCallbacks()
        {
            //Event.KeyboardEvent.Add()
            //Event.current.mousePosition

            _generateBttn.clicked += Generate;
            //_searchBar.RegisterValueChangedCallback(SearchChanged);
            _itemTypeDropdown.RegisterValueChangedCallback(ItemTypeChanged);
            _poolsDropdown.RegisterValueChangedCallback(PoolChanged);

            _searchBttn.clicked += Search;
            _searchBar.RegisterCallback<FocusInEvent>(OnSearchFocusIn);
            _searchBar.RegisterCallback<FocusOutEvent>(OnSearchFocusOut);
            _zoomInBttn.clicked += ZoomIn;
            _zoomOutBttn.clicked += ZoomOut;
        }

        private async Task FetchLazyData()
        {
            var rawData = await Fetcher.GitWebRawString("", "/", "main", "json");
            //Debug.Log(rawData);
            lazyData = JsonConvert.DeserializeObject<ServerData>(rawData);

            selectedPoolIndex = 0;
            selectedPool = lazyData.Pools[selectedPoolIndex].Id;

        }

        private void SetupBaseUI()
        {
            _root = rootVisualElement;
            // root.styleSheets.Add(Resources.Load<StyleSheet>("qtStyles"));

            // Loads and clones our VisualTree (eg. our UXML structure) inside the root.
            var quickToolVisualTree = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath($"{basePath}/{UI_PATH}/MainLayout.uxml", typeof(VisualTreeAsset));
            //var quickToolVisualTree = Resources.Load<VisualTreeAsset>("MainLayout");
            quickToolVisualTree.CloneTree(_root);
        }

        private void SetupCamera()
        {
            //Debug.Log("Camera Setup");
            if (previewRenderUtility != null)
            {
                previewRenderUtility.Cleanup();
                previewRenderUtility = null;
            }

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


        private void SetupItems(List<Item> items)
        {
            _grid.Clear();

            //for (int j = 0; j < 10; j++)
            //{

            for (int i = 0; i < items.Count; i++)
            {
                //var template = Resources.Load<VisualTreeAsset>("LazyItem");
                if (_itemTemplate == null)
                    _itemTemplate = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath($"{basePath}/{UI_PATH}/LazyItem.uxml", typeof(VisualTreeAsset));

                var element = _itemTemplate.CloneTree();
                SetupButton(element, items[i].Id, i, $"{selectedPool}/{items[i].Id}");
                _grid.Add(element);
                //await Task.Delay(1);
            }



            //}
        }

        private async void SetupButton(VisualElement element, string name, int index, string path)
        {
            element.name = name;
            var label = element.Query<Label>().First();
            label.text = name;

            var button = element.Q<Button>();
            var shadow = element.Q<VisualElement>("Shadow");

            BeginItemAnimation(button, shadow);

            // var buttonIcon = button.Q(className: "quicktool-button-icon");

            var texture = await Fetcher.WebImage(path, "thumbnail", "png");


            button.style.backgroundImage = texture;

            // Sets a basic tooltip to the button itself.
            button.tooltip = name;

            //Add Callback
            button.clicked += () => ItemSelected(name, index, texture);

            if (lastSessionItem != null && lastSessionItem == name && !lastSessionItemFlag)
            {
                lastSessionItemFlag = true;
                ItemSelected(name, index, texture, true);
                _itemTypeDropdown.value = lastSessionItemType;
            }

        }

        private async void BeginItemAnimation(Button button, VisualElement shadow)
        {
            button.style.opacity = 0;
            shadow.style.opacity = 0;

            await Task.Delay(50);
            button.style.opacity = 1;
            shadow.style.opacity = 1;
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
        private void Generate()
        {
            MoveTempItem(selectedFile);
            var gObj = AssetDatabase.LoadAssetAtPath($"{basePath}/{STORED_ITEMS_PATH}/{selectedFile}", typeof(UnityEngine.Object)) as GameObject;

            if (gObj == null) return;
            var newObj = GameObject.Instantiate(gObj);
            PlaceObjectInView(newObj);
            //PlaceObjectInView(test);
            Close();
        }
        private void ItemSelected(string itemId, int index, Texture2D icon, bool firstSelection = false)
        {
            if (selectedItem == itemId) return;

            //Debug.Log($"ClickedItem {itemId}");
            selectedItem = itemId;
            lastSessionItem = itemId;
            _tabImg.style.backgroundImage = icon;
            var titleCapitalized = itemId.Substring(0, 1).ToUpper() + itemId.Substring(1);
            _tabTitle.text = titleCapitalized;


            //_tabDropdown.choices.Clear();

            _itemTypeDropdown.choices = lazyData.Pools[selectedPoolIndex].Items[index].TypeIds;

            if (_itemTypeDropdown.choices.Count > 0 && !firstSelection)
                _itemTypeDropdown.value = _itemTypeDropdown.choices[0];

            //var oldPos = previewRenderUtility.camera.transform.position;

            //if (initSet)
            //initPos = previewRenderUtility.camera.transform.position;

            setDist = true;
            //Debug.Log($" oldPos {oldPos}");
            //Debug.Log($" newPos {initPos}");


            //previewRenderUtility.camera.transform.position = initPos;
            //previewRenderUtility.camera.transform.position = new Vector3(0, 10.5f, -18);
            //previewRenderUtility.camera.transform.position= new Vector3(0, 10.5f, -18);
            //previewMesh.bounds.
            //edit added Instantiation using your variable for clarity
            //UnityEngine.Object go;
            //if (_asset != null)
            //{
            //    go = Instantiate(_asset, Vector3.zero, Quaternion.identity);
            //    go.name = "ye";
            //}
            //var ft = new Fetcher();
            //Debug.Log("Donwload prompted");
            //ft.GetWebFile();
        }

        private void CreateObject(string primitiveTypeName)
        {
            var pt = (PrimitiveType)Enum.Parse
                         (typeof(PrimitiveType), primitiveTypeName, true);
            var go = ObjectFactory.CreatePrimitive(pt);
            go.transform.position = Vector3.zero;
        }


        private void OnGUI()
        {
            if (previewRenderUtility == null) return;

            Rect rect = _tabImg.worldBound;
            //Debug.Log($"Tab Img Rect {r.ToString() }");
            //Rect r = new Rect(0, 0, 200, 200);

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


            //var sign = Resources.Load<GameObject>("sign");
            //if (sign != null)
            //    GameObject.Instantiate(sign);


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


        private void SearchChanged(ChangeEvent<string> value)
        {
            //Debug.Log($"changed {value.newValue}");
        }

        private void Search()
        {
            var initList = lazyData.Pools[selectedPoolIndex].Items;
            var searchVal = _searchBar.value;

            if (String.IsNullOrWhiteSpace(searchVal))
                SetupItems(initList);
            else
            {
                var searchedList = FuzzySearch(initList, searchVal);
                SetupItems(searchedList);
            }
        }

        private List<Item> FuzzySearch(List<Item> source, string key)
        {
            //source.Sort((a, b) => FuzzyItemComparison(a, b, key));
            return source.Where(x => FuzzyItemMatch(x, key) > 1).OrderByDescending(x => FuzzyItemMatch(x, key)).ToList();
        }

        //private int FuzzyItemComparison(Item a, Item b, string key)
        //{
        //    var matchA = FuzzyItemMatch(a, key);
        //    var matchB = FuzzyItemMatch(b, key);

        //    if (matchA > matchB)
        //        return 1;
        //    else if (matchA < matchB)
        //        return -1;

        //    return 0;
        //}
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


        private void PoolChanged(ChangeEvent<string> value)
        {
            var newPool = value.newValue;

            if (newPool == NEWPOOL_MSG)
            {
                return;
            }

            _poolSelected.text = newPool;

        }
        private async void ItemTypeChanged(ChangeEvent<string> value)
        {
            if (string.IsNullOrEmpty(value.newValue)) return;


            previewObj = null;
            previewMesh = null;
            previewMats = null;

            _itemTypeSelected.text = value.newValue;
            lastSessionItemType = value.newValue;

            var filename = $"{selectedItem}_{value.newValue}";
            var fileFormat = "fbx";

            selectedFile = $"{filename}.{fileFormat}";

            (bool, int) result = HasTempItem(selectedFile);
            if (!result.Item1)
            {
                Debug.Log("Fetching item name" + filename);
                AddTempItem($"{filename}.{fileFormat}");
                await Fetcher.GitRawFile($"{selectedPool}/{selectedItem}", TEMP_ITEMS_PATH, filename, fileFormat);
            }

            previewObj = AssetDatabase.LoadAssetAtPath($"{basePath}/{TEMP_ITEMS_PATH}/{selectedFile}", typeof(UnityEngine.Object)) as GameObject;

            if (previewObj == null)
            {
                Debug.LogError("Item couldn't be loaded");
                return;
            }


            var meshRend = previewObj.gameObject.GetComponent<MeshRenderer>();
            previewMats = meshRend.sharedMaterials;
            previewColors = new List<Color>();

            //make materials not glossy/shiny
            foreach (var material in previewMats)
            {
                material.SetColor("_EmissionColor", material.color);
                material.SetFloat("_Glossiness", 0f);
                previewColors.Add(material.color);

            }

            //Add color pickers
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
            var targetPosition = bounds.center - distance * previewRenderUtility.camera.transform.forward;
            previewRenderUtility.camera.transform.position = targetPosition;

            //Debug.Log($"Max bounds {maxSize}");
        }

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

        private void OnSearchFocusIn(FocusInEvent focus)
        {
            isSearchFocused = true;
            _poolsContainer.style.backgroundColor = activeButtonColor;
            _searchBttn.style.backgroundColor = activeButtonColor;
        }

        private void OnSearchFocusOut(FocusOutEvent focus)
        {
            isSearchFocused = false;
            _poolsContainer.style.backgroundColor = defaultButtonColor;
            _searchBttn.style.backgroundColor = defaultButtonColor;
        }
        #region Temp Buffer

        private void GetTmpItems()
        {
            string tmpPath = $"{appPath}/{basePath}/{TEMP_ITEMS_PATH}";
            if (!Directory.Exists(tmpPath))
                Directory.CreateDirectory(tmpPath);

            DirectoryInfo tmpInfo = new DirectoryInfo(tmpPath);
            FileInfo[] files = tmpInfo.GetFiles();
            //string[] allFiles = Directory.GetFiles(tmpPath);

            ////Filters only for files with tmp1.xxx structure, Clamps with the temp size and orders by theirs indexes
            //string[] tmpFiles = allFiles
            //    .Where(x => x.StartsWith("tmp") && int.TryParse(x.Substring(3, x.IndexOf('.')), out int index) && index > 0 && index < tempArraySize)
            //    .OrderBy(x => int.Parse(x.Substring(3, x.IndexOf('.'))))
            //    .ToArray();
            //Removes all the files that not comply the above filters
            //allFiles.Except(tmpFiles).ToList().ForEach(x => File.Delete(x));

            string[] tmpFiles = files
                .Where(x => !x.Extension.Contains("meta"))
                .OrderBy(f => f.LastWriteTime)
                .Select(f => f.Name)
                .ToArray();

            if (tempItems == null)
                tempItems = new List<string>();

            tempItems.Clear();
            int minArrSize = Mathf.Min(tempArraySize, tmpFiles.Length);
            for (int i = 0; i < minArrSize; i++)
                tempItems.Add(tmpFiles[i]);

            if (minArrSize > 0)
            {
                var newestItem = tempItems.Last();
                var nameSplitted = newestItem.Substring(0, newestItem.IndexOf('.')).Split('_');
                lastSessionItem = nameSplitted[0];
                lastSessionItemType = nameSplitted[1];
            }
        }

        private (bool, int) HasTempItem(string itemName)
        {
            bool hasFile = tempItems.Contains(itemName);
            int index = hasFile ? tempItems.IndexOf(itemName) : -1;

            return (hasFile, index);
        }

        private void AddTempItem(string itemName)
        {
            if (tempItems.Count >= tempArraySize)
            {
                var filePath = $"{appPath}/{basePath}/{TEMP_ITEMS_PATH}/{tempItems[0]}";
                File.Delete(filePath);
                File.Delete(filePath + ".meta");
                tempItems.RemoveAt(0);
            }

            tempItems.Add(itemName);
        }

        private void MoveTempItem(string itemName)
        {
            var dir = $"{Application.dataPath}/{STORED_ITEMS_PATH}";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            AssetDatabase.MoveAsset($"{basePath}/{TEMP_ITEMS_PATH}/{itemName}", $"{basePath}/{STORED_ITEMS_PATH}/{itemName}");
        }

        #endregion

        //#region Data

        //public LazyData lazyData;
        //public class LazyData
        //{
        //    [JsonProperty("pools")]
        //    public List<Pool> Pools { get; set; }
        //}

        //public class Pool
        //{
        //    [JsonProperty("PoolName")]
        //    public string PoolName { get; set; }

        //    [JsonProperty("PoolItems")]
        //    public List<string> PoolItems { get; set; }
        //}

        //#endregion
    }
}