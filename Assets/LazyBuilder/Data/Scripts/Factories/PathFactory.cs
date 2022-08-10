using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LazyBuilder
{
    public class PathFactory
    {
        public static string absoluteProjectPath { get; set; }
        public static string relativeToolPath { get; set; }


        public const string DATA_FOLDER = "general";

        public const string THUMBNAIL_FILE = "thumbnail";
        public const string THUMBNAIL_TYPE = "png";

        public const string TEMP_ITEMS_PATH = "Data/Temp/Items";
        public const string TEMP_THUMB_PATH = "Data/Temp/Thumbnails";
        public const string STORED_ITEMS_PATH = "Data/Items";
        public const string UI_PATH = "Data/UI";
        public const string MESHES_PATH = "Data/Meshes";
        public const string MATERIALS_PATH = "Data/Materials";

        public const string LAYOUT_TYPE = "uxml";
        public const string STYLE_TYPE = "uss";

        public const string MANAGER_LAYOUT_FILE = "ManagerLayout";
        public const string MANAGER_ITEM_LAYOUT_FILE = "ManagerItemLayout";
        public const string BUILDER_LAYOUT_FILE = "BuilderLayout";
        public const string BUILDER_ITEM_LAYOUT_FILE = "BuilderItemLayout";

        public const string MESH_TYPE = "fbx";
        public const string MATERIAL_TYPE = "mat";

        public const string GROUND_FILE = "ground";

        public static void Init(ScriptableObject scriptSource)
        {
            absoluteProjectPath = Path.GetDirectoryName(Application.dataPath);
            var currentPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(scriptSource)));
            relativeToolPath = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(currentPath))).RelativeFormat();
        }
        public static string BuildUiFilePath(string fileName, bool layoutFile = true, bool absolute = false)
        {
            var rootPath = absolute ? absoluteProjectPath : relativeToolPath;
            var fileType = layoutFile ? LAYOUT_TYPE : STYLE_TYPE;
            var path = $"{rootPath}/{UI_PATH}/{fileName}.{fileType}";

            if (absolute)
                path = path.AbsoluteFormat();

            return path;
        }

        public static string BuildMeshFilePath(string fileName, bool absolute = false)
        {
            var rootPath = absolute ? absoluteProjectPath : relativeToolPath;
            var path = $"{rootPath}/{MESHES_PATH}/{fileName}.{MESH_TYPE}";

            if (absolute)
                path = path.AbsoluteFormat();

            return path;
        }

        public static string BuildMaterialFilePath(string fileName, bool absolute = false)
        {
            var rootPath = absolute ? absoluteProjectPath : relativeToolPath;
            var path = $"{rootPath}/{MATERIALS_PATH}/{fileName}.{MATERIAL_TYPE}";

            if (absolute)
                path = path.AbsoluteFormat();

            return path;
        }
    }
}
