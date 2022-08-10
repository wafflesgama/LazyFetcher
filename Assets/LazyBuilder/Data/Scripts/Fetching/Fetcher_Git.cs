using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace LazyBuilder
{
    public class Fetcher_Git : Fetcher
    {
        private static Dictionary<string, Texture2D> storedImages = new Dictionary<string, Texture2D>();

        private string gitRepo = "wafflesgama/LazyBuilderData";
        private string gitBranch = "main";

        private const string gitBlob = "blob";
        private const string gitRawPrefix = "https://raw.githubusercontent.com";
        private const string gitStandardPrefix = "https://github.com";

        public Fetcher_Git(string repo = null, string branch = null)
        {
            if (gitRepo != null)
                gitRepo = repo;

            if (branch != null)
                gitBranch = branch;
        }
        public async Task<string> GetRawFile(string subUrl, string savePath, string fileName, string fileType)
        {
            using (var client = new WebClient())
            {
                TaskCompletionSource<bool> processFinished = new TaskCompletionSource<bool>();

                client.DownloadFileCompleted += (sender, e) =>
                {

                    Debug.Log($"Asset fetched- {fileName}.{fileType}");
                    AssetDatabase.Refresh();
                    processFinished.SetResult(!e.Cancelled && e.Error == null);
                };

                string savedFilePath = $"{Application.dataPath}/{savePath}/{fileName}.{fileType}";
                string urlFullPath = $"{gitStandardPrefix}/{gitRepo}/{gitBlob}/{gitBranch}/{subUrl}/{fileName}.{fileType}?raw=true";
                Debug.Log($"Fetching file from: {urlFullPath}");
                Debug.Log($"Saving  file to: {savedFilePath}");

                client.DownloadFileAsync(new System.Uri(urlFullPath), savedFilePath);
                if (await processFinished.Task)
                    return savedFilePath;

                return string.Empty;
            }
        }

        public async Task<string> GetRawString(string src, string fileName, string fileType)
        {
            using (var client = new WebClient())
            {
                TaskCompletionSource<(bool, string)> processFinished = new TaskCompletionSource<(bool, string)>();
                client.DownloadDataCompleted += (sender, e) =>
                {
                    Debug.Log("String fetched");
                    processFinished.SetResult((!e.Cancelled && e.Error == null, System.Text.Encoding.Default.GetString(e.Result)));
                };

                //string savedFilePath = $"{localPath}/{fileName}.{fileType}";
                string urlFullPath = $"{gitStandardPrefix}/{gitRepo}/{gitBlob}/{gitBranch}/{src}/{fileName}.{fileType}?raw=true";

                client.DownloadDataAsync(new System.Uri(urlFullPath));
                var processResult = await processFinished.Task;
                if (processResult.Item1)
                    return processResult.Item2;
                return string.Empty;
            }
        }


        public async Task<Texture2D> GetImage(string subPath, string imgName, string imgType= PathFactory.THUMBNAIL_TYPE)
        {

            var url = $"{gitRawPrefix}/{gitRepo}/{gitBranch}/{subPath}/{imgName}.{imgType}";
            //Debug.Log($"Fetching img from: {url}");

            if (storedImages.ContainsKey(url))
                return storedImages[url];

            UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url);

            //bool finished = false;
            //uwr.SendWebRequest().completed += (e) => finished = true;
            uwr.SendWebRequest();

            while (!uwr.isDone)
                await Task.Delay(15);

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(uwr.error);
                return null;
            }

            Texture2D iconAsset = DownloadHandlerTexture.GetContent(uwr);

            //Re check in case of concurrency + delay
            if (!storedImages.ContainsKey(url))
                storedImages.Add(url, iconAsset);

            return iconAsset;
        }

        public string GetSrcPath()
        {
            return $"{gitStandardPrefix}/{gitRepo}/{gitBlob}/{gitBranch}";
        }
    }
}
