using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LazyBuilder
{

    public static class Utils
    {
        public static int GetRandomSeed()
        {
            return DateTime.UtcNow.Hour - DateTime.UtcNow.Millisecond + DateTime.UtcNow.Second;
        }
        public static string Capitalize(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;
            // convert to char array of the string
            char[] letters = source.ToCharArray();
            // upper case the first char
            letters[0] = char.ToUpper(letters[0]);
            // return the array made of the new char array
            return new string(letters);
        }

        public static string SeparateCase(this string source)
        {
            string[] splittedWords = Regex.Split(source, @"(?<=[a-z])(?=[A-Z])");

            //string[] splittedWords = Regex.Split(source, @"(?<!^)(?=[A-Z])");
            string final = "";

            foreach (var splittedWord in splittedWords)
            {
                if (splittedWord.Length == 0) continue;
                final += splittedWord + " ";
            }
            //Remove last space
            final.Remove(final.Length - 1, 1);

            return final;
        }

        public static string AbsoluteFormat(this string source)
        {
            return source.Replace('/', '\\');
        }

        public static string RelativeFormat(this string source)
        {
            return source.Replace('\\', '/');
        }

        public static char Upper(this char source)
        {
            return char.ToUpper(source);
        }

        public static async Task CreateThumbnailFromModelAsync(string absItemPath, string fileName, Vector3 posOffset, Vector3 rotOffset, string fileType = "fbx")
        {
            IconFactory.MarkTextureNonReadable = false;
            IconFactory.OrthographicMode = true;
            IconFactory.BackgroundColor = new Color(0.294f, 0.294f, 0.294f);

            var tempFileName = "thumbModel";
            var tempFileAbsPath = $"{PathFactory.absoluteToolPath}\\{PathFactory.TEMP_THUMB_PATH.AbsoluteFormat()}\\{tempFileName}.{fileType}";
            var tempFileRelPath = $"{PathFactory.relativeToolPath}/{PathFactory.TEMP_THUMB_PATH}/{tempFileName}.{fileType}";

            if (File.Exists(tempFileAbsPath))
                File.Delete(tempFileAbsPath);

            File.Copy($"{absItemPath}\\{fileName}.{fileType}", tempFileAbsPath);

            var tex = await IconFactory.GenerateModelPreviewFromPath(tempFileRelPath, posOffset, rotOffset, 256, 256);
            var bytes = tex.EncodeToPNG();

            var filePath = $"{absItemPath}\\thumbnail.png";


            if (File.Exists(filePath))
                File.Delete(filePath);

             File.WriteAllBytes(filePath, bytes);

            File.Delete(tempFileAbsPath);
        }

        public static string[] GetFiles(string path, bool createDir = false)
        {
            if (!Directory.Exists(path))
            {
                if (createDir)
                    Directory.CreateDirectory(path);
                else
                    return new string[0];
            }

            DirectoryInfo tmpInfo = new DirectoryInfo(path);
            FileInfo[] files = tmpInfo.GetFiles();

            //Get all file (exluding meta files) names sorted by the modification date
            return files
                .Where(x => !x.Extension.Contains("meta"))
                .OrderBy(f => f.LastWriteTime)
                .Select(f => f.Name)
                .ToArray();
        }

        public static PopupField<string> CreateDropdownField(VisualElement parentElement)
        {
            PopupField<string> field = new PopupField<string>();
            field.style.flexGrow = parentElement.style.flexGrow;
            field.style.flexWrap = parentElement.style.flexWrap;
            //field.style.visibility= parentElement.style.visibility;
            //field.style.opacity= parentElement.style.opacity;
            field.style.width = new StyleLength(Length.Percent(100));
            field.style.height = new StyleLength(Length.Percent(100));

            parentElement.Add(field);
            return field;

        }

        public static Material ConvertToDefault(this Material material)
        {
            Shader defaultShader;
            if (UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset == null)
                defaultShader = AssetDatabase.GetBuiltinExtraResource<Shader>("Standard.shader");
            else
                defaultShader = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset.defaultShader;

            material.shader = defaultShader;
            return material;

        }

    }



}
