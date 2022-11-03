using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LazyBuilder
{
    public class PathFactory
    {
        public static string absoluteToolPath { get; set; }
        public static string relativeToolPath { get; set; }


        public const string DATA_FOLDER = "content";

        public const string MAIN_FILE = "data";
        public const string MAIN_TYPE = "json";

        public const string THUMBNAIL_FILE = "thumbnail";
        public const string THUMBNAIL_TYPE = "png";

        public const string PREFS_PATH = "Data/Preferences";
        public const string BUILDER_PREFS_FILE = "Builder";
        public const string MANAGER_PREFS_FILE = "Manager";

        public const string TEMP_ITEMS_PATH = "Data/Temp/Items";
        public const string TEMP_THUMB_PATH = "Data/Temp/Thumbnails";

        public const string STORED_ITEMS_PATH = "Data/Items";
        public const string STORED_THUMB_PATH = "Data/Thumbnails";

        public const string UI_PATH = "Data/UI";
        public const string MESHES_PATH = "Data/Meshes";

        public const string MATERIALS_PATH = "Data/Materials";
        public const string MATERIALS_GROUND_FILE = "Lazy_Preview_Mat";

        public const string LAYOUT_TYPE = "uxml";
        public const string STYLE_TYPE = "uss";

        public const string MANAGER_LAYOUT_FILE = "ManagerLayout";
        public const string MANAGER_ITEM_LAYOUT_FILE = "ManagerItemLayout";
        public const string BUILDER_LAYOUT_FILE = "BuilderLayout";
        public const string BUILDER_ITEM_LAYOUT_FILE = "BuilderItemLayout";
        public const string BUILDER_SERVER_ITEM_FILE = "BuilderServerItem";

        public const string MESHES_GROUND_FILE = "Lazy_Preview_Ground";
        public const string MESH_TYPE = "fbx";
        public const string MATERIAL_TYPE = "mat";


        public const string MARKDOWN_FILE = "AssetList";


        public static void Init(ScriptableObject scriptSource)
        {
            var currentPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(scriptSource)));
            relativeToolPath = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(currentPath))).RelativeFormat();

            absoluteToolPath = $"{Path.GetDirectoryName(Application.dataPath)}\\{relativeToolPath}";
        }

        #region Local Paths

        public static string BuildUiFilePath(string fileName, bool layoutFile = true, bool absolute = false)
        {
            var rootPath = absolute ? absoluteToolPath : relativeToolPath;
            var fileType = layoutFile ? LAYOUT_TYPE : STYLE_TYPE;
            var path = $"{rootPath}/{UI_PATH}/{fileName}.{fileType}";

            if (absolute)
                path = path.AbsoluteFormat();

            return path;
        }

        public static string BuildMeshFilePath(string fileName, bool absolute = false)
        {
            var rootPath = absolute ? absoluteToolPath : relativeToolPath;
            var path = $"{rootPath}/{MESHES_PATH}/{fileName}.{MESH_TYPE}";

            if (absolute)
                path = path.AbsoluteFormat();

            return path;
        }

        public static string BuildMaterialFilePath(string fileName, bool absolute = false)
        {
            var rootPath = absolute ? absoluteToolPath : relativeToolPath;
            var path = $"{rootPath}/{MATERIALS_PATH}/{fileName}.{MATERIAL_TYPE}";

            if (absolute)
                path = path.AbsoluteFormat();


            return path;
        }

        #endregion Local Paths


        #region Sub Paths

        public static string BuildItemPath(string itemName, bool absolute = false)
        {
            var path = $"{DATA_FOLDER}/{itemName}";

            if (absolute)
                path = path.AbsoluteFormat();

            return path;
        }

        public static string BuildItemFile(string itemId, string itemTypeId)
        {
            return $"{itemId}_{itemTypeId}.{MESH_TYPE}";
        }

        /// <summary>
        /// Gets Item Id & Item Type Id from the fileName structure "Id_TypeId.fbx"
        /// </summary>
        /// <param name="file">The standard named Item's file</param>
        /// <returns> Returns (Id,TypeId)</returns>
        public static (string, string) GetItemIds(string file)
        {
            //Format: Id_TypeId.fbx

            var fileSplit = file.Split('_');

            return (fileSplit[0], fileSplit[1].Substring(0, fileSplit[1].IndexOf('.')));
        }
        #endregion Sub Paths
    }
}
