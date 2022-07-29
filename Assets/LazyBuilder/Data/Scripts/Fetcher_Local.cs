using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace LazyBuilder
{
    public class Fetcher_Local : Fetcher
    {

        private string serverPath;

        public Fetcher_Local(string _serverPath)
        {
            serverPath = _serverPath;
        }

        public async Task<string> GetRawFile(string src, string savePath, string fileName, string fileType)
        {
            var savedFile = $"{savePath}/{fileName}.{fileType}";
            File.Copy($"{serverPath}/{src}/{fileName}.{fileType}", savedFile);
            return savedFile;
        }
        public Task<string> GetRawString(string src, string fileName, string fileType)
        {
            return File.ReadAllTextAsync($"{serverPath}/{src}/{fileName}.{fileType}");
        }
        public async Task<Texture2D> GetImage(string src, string imgName, string imgType)
        {
            var path = $"{serverPath}/{src}/{imgName}.{imgType}";
            byte[] pngBytes = await File.ReadAllBytesAsync(path);

            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(pngBytes);

            return tex;
        }

        public string GetSrcPath()
        {
            return serverPath;
        }

    }
}
