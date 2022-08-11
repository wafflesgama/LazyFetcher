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

namespace LazyBuilder
{

    public class BuilderWindow : EditorWindow
    {
        private string lastSessionItem;
        private string lastSessionItemType;
        private bool lastSessionItemFlag;

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

        private VisualTreeAsset _itemTemplate;


        const string NEWPOOL_MSG = "Add new pool...";


        #region Unity Functions
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

            await MainController.FetchServerData();


            SetupItems(MainController.lazyData.Items);


            //This infinite async loop ensures that the preview's camera is always rendered
            RepaintCycle();

            Debug.Log("Searcbar focus");
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

        private void SetupItems(List<Item> items)
        {
            _grid.Clear();

            //for (int j = 0; j < 10; j++)
            //{

            for (int i = 0; i < items.Count; i++)
            {
                //var template = Resources.Load<VisualTreeAsset>("LazyItem");
                if (_itemTemplate == null)
                    _itemTemplate = (VisualTreeAsset)AssetDatabase.LoadAssetAtPath(PathFactory.BuildUiFilePath(PathFactory.BUILDER_ITEM_LAYOUT_FILE), typeof(VisualTreeAsset));

                var element = _itemTemplate.CloneTree();
                SetupItem(element, items[i].Id, i, PathFactory.BuildItemPath(items[i].Id));
                _grid.Add(element);
                //await Task.Delay(1);
            }



            //}
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

            var texture = await MainController.GetImage(path, PathFactory.THUMBNAIL_FILE, PathFactory.THUMBNAIL_TYPE);


            var imgHolder = button.Q("Img");
            imgHolder.style.backgroundImage = texture;

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
            if (previewRenderUtility == null) return;

            Rect rect = _tabImg.worldBound;

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
        #endregion IO

        #region Temporary Items Buffer

        private void GetTmpItems()
        {
            string tmpPath = $"{PathFactory.absoluteProjectPath}/{PathFactory.relativeToolPath}/{PathFactory.TEMP_ITEMS_PATH}";
            if (!Directory.Exists(tmpPath))
                Directory.CreateDirectory(tmpPath);

            DirectoryInfo tmpInfo = new DirectoryInfo(tmpPath);
            FileInfo[] files = tmpInfo.GetFiles();

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
                var filePath = $"{PathFactory.absoluteProjectPath}/{PathFactory.relativeToolPath}/{PathFactory.TEMP_ITEMS_PATH}/{tempItems[0]}";
                File.Delete(filePath);
                File.Delete(filePath + ".meta");
                tempItems.RemoveAt(0);
            }

            tempItems.Add(itemName);
        }

        private void MoveTempItem(string itemName)
        {
            var dir = $"{Application.dataPath}/{PathFactory.STORED_ITEMS_PATH}";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            AssetDatabase.MoveAsset($"{PathFactory.relativeToolPath}/{PathFactory.TEMP_ITEMS_PATH}/{itemName}", $"{PathFactory.relativeToolPath}/{PathFactory.STORED_ITEMS_PATH}/{itemName}");
        }

        #endregion Temporary Items Buffer

        #region Items Search

        private void Search()
        {
            var initList = MainController.lazyData.Items;
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
            _poolsContainer.style.backgroundColor = activeButtonColor;
            _searchBttn.style.backgroundColor = activeButtonColor;
        }

        private void OnSearchFocusOut(FocusOutEvent focus)
        {
            isSearchFocused = false;
            _poolsContainer.style.backgroundColor = defaultButtonColor;
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
            MoveTempItem(selectedFile);
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
        private void ItemSelected(string itemId, int index, Texture2D icon, bool firstSelection = false)
        {
            if (selectedItem == itemId) return;

            //Debug.Log($"ClickedItem {itemId}");
            selectedItem = itemId;
            lastSessionItem = itemId;
            _tabImg.style.backgroundImage = icon;
            _tabTitle.text = itemId.Capitalize();


            //_tabDropdown.choices.Clear();

            _itemTypeDropdown.choices = MainController.lazyData.Items[index].TypeIds;

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
                await MainController.GetRawFile(PathFactory.BuildItemPath(selectedItem), PathFactory.TEMP_ITEMS_PATH, filename, fileFormat);
            }

            AssetDatabase.Refresh();
            previewObj = AssetDatabase.LoadAssetAtPath($"{PathFactory.relativeToolPath}/{PathFactory.TEMP_ITEMS_PATH}/{selectedFile}", typeof(UnityEngine.Object)) as GameObject;

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
        #endregion Item & Type Change

        #region Item Pools 
        private void PoolChanged(ChangeEvent<string> value)
        {
            var newPool = value.newValue;

            if (newPool == NEWPOOL_MSG)
            {
                return;
            }

            _poolSelected.text = newPool;

        }

        #endregion Item Pools
    }
}
