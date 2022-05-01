
using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine.Networking;
using System.Collections.Generic;


namespace LazyBuilder
{
    public class Fetcher
    {
        private static Dictionary<string, Texture2D> storedImages = new Dictionary<string, Texture2D>();

        public static string gitRepo = "wafflesgama/LazyBuilderData";
        public static string gitBranch = "main";
        const string gitBlob = "blob";
        const string gitRawPrefix = "https://raw.githubusercontent.com";
        const string gitStandardPrefix = "https://github.com";

        //public const string gitUrl = "https://github.com/wafflesgama/LazyBuilderData/blob/main";
        //public string testUrl = "https://github.com/ud-waffle/Test/blob/main/SubFolderX/SubFolderXX/signX%20X.fbx?raw=true";

        public static async Task<string> GitRawFile(string subUrl, string savePath, string fileName, string fileType)
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

        public static async Task<string> GitWebRawString(string subUrl, string localPath, string fileName, string fileType)
        {
            using (var client = new WebClient())
            {
                TaskCompletionSource<(bool, string)> processFinished = new TaskCompletionSource<(bool, string)>();
                client.DownloadDataCompleted += (sender, e) =>
                {
                    Debug.Log("String fetched");
                    processFinished.SetResult((!e.Cancelled && e.Error == null, System.Text.Encoding.Default.GetString(e.Result)));
                };

                string savedFilePath = $"{localPath}/{fileName}.{fileType}";
                string urlFullPath = $"{gitStandardPrefix}/{gitRepo}/{gitBlob}/{gitBranch}/{subUrl}/{fileName}.{fileType}?raw=true";

                client.DownloadDataAsync(new System.Uri(urlFullPath));
                var processResult = await processFinished.Task;
                if (processResult.Item1)
                    return processResult.Item2;
                return string.Empty;
            }
        }


        public static async Task<Texture2D> WebImage(string subPath, string imgName, string imgType)
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

    }
}