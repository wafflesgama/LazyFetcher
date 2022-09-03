using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace LazyBuilder
{

    public static class Utils
    {

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

        public static string AbsoluteFormat(this string source)
        {
            return source.Replace('/', '\\');
        }

        public static string RelativeFormat(this string source)
        {
            return source.Replace('\\', '/');
        }

        public static async Task CreateThumbnailFromModelAsync(string absItemPath, string fileName, string fileType = "fbx")
        {
            IconFactory.MarkTextureNonReadable = false;
            IconFactory.OrthographicMode = true;
            IconFactory.BackgroundColor = new Color(0, 0, 0, 0);

            var tempFileName = "thumbModel";
            var tempFileAbsPath = $"{PathFactory.absoluteToolPath}\\{PathFactory.TEMP_THUMB_PATH.AbsoluteFormat()}\\{tempFileName}.{fileType}";
            var tempFileRelPath = $"{PathFactory.relativeToolPath}/{PathFactory.TEMP_THUMB_PATH}/{tempFileName}.{fileType}";

            if (File.Exists(tempFileAbsPath))
                File.Delete(tempFileAbsPath);

            File.Copy($"{absItemPath}\\{fileName}.{fileType}", tempFileAbsPath);

            var tex = await IconFactory.GenerateModelPreviewFromPath(tempFileRelPath, 1024, 1024);
            var bytes = tex.EncodeToPNG();

            var filePath = $"{absItemPath}\\thumbnail.png";


            if (File.Exists(filePath))
                File.Delete(filePath);

            await File.WriteAllBytesAsync(filePath, bytes);

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
    }



}
