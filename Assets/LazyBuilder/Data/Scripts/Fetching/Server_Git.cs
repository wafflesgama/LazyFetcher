using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace LazyBuilder
{
    public class Server_Git : Server
    {
        private static Dictionary<string, Texture2D> storedImages = new Dictionary<string, Texture2D>();


        public const string gitBlob = "blob";
        public const string gitRawPrefix = "https://raw.githubusercontent.com";
        public const string gitStandardPrefix = "https://github.com";
        public const string defaultRepo = "wafflesgama/LazyBuilderLibrary";
        public const string defaultBranch = "main";

        private string gitRepo;
        private string gitBranch;

        public Server_Git(string repo, string branch)
        {
            gitRepo = repo;
            gitBranch = branch;
            storedImages = new Dictionary<string, Texture2D>();
        }

        public async Task<string> GetRawFile(string subUrl, string savePath, string fileName, string fileType)
        {
            using (var client = new WebClient())
            {
                TaskCompletionSource<bool> processFinished = new TaskCompletionSource<bool>();

                client.DownloadFileCompleted += (sender, e) =>
                {

                    //Debug.Log($"Asset fetched- {fileName}.{fileType}");
                    AssetDatabase.Refresh();
                    processFinished.SetResult(!e.Cancelled && e.Error == null);
                };

                string savedFilePath = $"{PathFactory.absoluteToolPath}/{savePath}/{fileName}.{fileType}";
                string urlFullPath = $"{gitStandardPrefix}/{gitRepo}/{gitBlob}/{gitBranch}/{subUrl}/{fileName}.{fileType}?raw=true";

                //Debug.Log($"Fetching file from: {urlFullPath}");
                //Debug.Log($"Saving  file to: {savedFilePath}");


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


        public async Task<Texture2D> GetImage(string subPath, string saveFilePath, string imgName)
        {
            var url = $"{gitRawPrefix}/{gitRepo}/{gitBranch}/{subPath}/{imgName}.{PathFactory.THUMBNAIL_TYPE}";
            //Debug.Log($"Fetching img from: {url}");

            Texture2D image;

            if (storedImages.ContainsKey(url))
            {
                image = storedImages[url];
            }
            else
            {


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

                image = DownloadHandlerTexture.GetContent(uwr);

                //Re check in case of concurrency + delay
                if (!storedImages.ContainsKey(url))
                    storedImages.Add(url, image);
            }

            if (saveFilePath != null)
            {
                var bytes = image.EncodeToPNG();

                //AssetDatabase.StartAssetEditing();
                //var tempFilePath=$"{Application.temporaryCachePath}/writeThumbnail.{PathFactory.THUMBNAIL_TYPE}";

                //if(File.Exists(tempFilePath))
                if (!File.Exists(saveFilePath))
                    File.Create(saveFilePath).Close();

                await File.WriteAllBytesAsync(saveFilePath, bytes);
                
                //AssetDatabase.StopAssetEditing();
            }

            return image;
        }

        public string GetFullPath()
        {
            return $"{gitStandardPrefix}/{gitRepo}/{gitBlob}/{gitBranch}";
        }

        public string GetSrc()
        {
            return gitRepo;
        }

        public string GetBranch()
        {
            return gitBranch;
        }
    }
}
