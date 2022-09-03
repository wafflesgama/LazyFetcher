using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace LazyBuilder
{
    public class Server_Local : Server
    {

        private string serverPath;

        public Server_Local(string _serverPath)
        {
            serverPath = _serverPath;
        }

        public async Task<string> GetRawFile(string src, string savePath, string fileName, string fileType)
        {
            var savedFile = $"{savePath}\\{fileName}.{fileType}";

            string savedFilePath = $"{PathFactory.absoluteToolPath}\\{savePath}\\{fileName}.{fileType}";
            var srcFilePath = $"{serverPath}\\{src}\\{fileName}.{fileType}";

            savedFilePath = savedFilePath.AbsoluteFormat();
            srcFilePath = srcFilePath.AbsoluteFormat();

            if (File.Exists(savedFilePath))
                File.Delete(savedFilePath);

            File.Copy(srcFilePath, savedFilePath);

            for (int i = 0; i < 9000; i++)
            {
                if (File.Exists(savedFilePath)) break;
                await Task.Delay(10);
            }
            return savedFile;
        }
        public Task<string> GetRawString(string src, string fileName, string fileType)
        {
            src = src.AbsoluteFormat();

            return File.ReadAllTextAsync($"{serverPath}\\{src}\\{fileName}.{fileType}");
        }
        public async Task<Texture2D> GetImage(string src, string imgName, string imgType = PathFactory.THUMBNAIL_TYPE)
        {
            src = src.AbsoluteFormat();

            var path = $"{serverPath}\\{src}\\{imgName}.{imgType}";

            if (!File.Exists(path)) return null;

            byte[] pngBytes = await File.ReadAllBytesAsync(path);

            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(pngBytes);

            return tex;
        }

        public string GetFullPath()
        {
            return serverPath;
        }
        public string GetSrc()
        {
            return serverPath;
        }
        public string GetBranch()
        {
            return null;
        }
    }
}
