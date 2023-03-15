using System;
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
        public const string rawSuffix = "?raw=true";

        private string gitRepo;
        private string gitBranch;

        public Server_Git(string repo, string branch)
        {
            gitRepo = repo;
            gitBranch = branch;
            storedImages = new Dictionary<string, Texture2D>();
        }

        public async Task<ServerResponse<string>> GetRawFile(string subUrl, string savePath, string fileName, string fileType)
        {
            ServerResponse<string> response = new ServerResponse<string>();
            try
            {
                using (var client = new WebClient())
                {

                    TaskCompletionSource<bool> processFinished = new TaskCompletionSource<bool>();

                    client.DownloadFileCompleted += (sender, e) =>
                    {
                        AssetDatabase.Refresh();

                        bool success = !e.Cancelled && e.Error == null;
                        response.Success = success;
                        response.Error = e.Error;

                        processFinished.SetResult(success);
                    };

                    string savedFilePath = $"{PathFactory.absoluteToolPath}/{savePath}/{fileName}.{fileType}";
                    string urlFullPath = $"{gitStandardPrefix}/{gitRepo}/{gitBlob}/{gitBranch}/{subUrl}/{fileName}.{fileType}{rawSuffix}";

                    client.DownloadFileAsync(new System.Uri(urlFullPath), savedFilePath);

                    var result = await processFinished.Task;

                    response.Data = result ? savedFilePath : string.Empty;

                }
            }
            catch (Exception e)
            {
                response.Success = false;
                response.Error = e;
            }
            return response;
        }

        public async Task<ServerResponse<string>> GetRawString(string src, string fileName, string fileType)
        {
            ServerResponse<string> response = new ServerResponse<string>();
            try
            {
                using (var client = new WebClient())
                {
                    TaskCompletionSource<bool> processFinished = new TaskCompletionSource<bool>();
                    client.DownloadDataCompleted += (sender, e) =>
                    {
                        bool success = !e.Cancelled && e.Error == null;
                        response.Success = success;
                        response.Data = success ? System.Text.Encoding.Default.GetString(e.Result) : string.Empty;
                        response.Error = e.Error;

                        processFinished.SetResult(success);
                    };

                    string urlFullPath = $"{gitStandardPrefix}/{gitRepo}/{gitBlob}/{gitBranch}/{src}/{fileName}.{fileType}?raw=true";

                    client.DownloadDataAsync(new System.Uri(urlFullPath));

                    var processResult = await processFinished.Task;

                    await processFinished.Task;

                }
            }
            catch (Exception e)
            {
                response.Success = false;
                response.Error = e;
            }
            return response;
        }

        public async Task<ServerResponse<Texture2D>> GetImage(string subPath, string saveFilePath, string imgName)
        {
            ServerResponse<Texture2D> response = new ServerResponse<Texture2D>();
            var url = $"{gitRawPrefix}/{gitRepo}/{gitBranch}/{subPath}/{imgName}.{PathFactory.THUMBNAIL_TYPE}";

            try
            {
                if (storedImages.ContainsKey(url))
                {
                    response.Data = storedImages[url];
                    response.Success = true;
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

                    response.Data = DownloadHandlerTexture.GetContent(uwr);

                    //Re check in case of concurrency + delay
                    if (!storedImages.ContainsKey(url))
                        storedImages.Add(url, response.Data);
                }

                //If specified a Save Path - saves the Texture2D
                if (saveFilePath != null)
                {
                    var bytes = response.Data.EncodeToPNG();

                    if (!File.Exists(saveFilePath))
                        File.Create(saveFilePath).Close();

                    File.WriteAllBytes(saveFilePath, bytes);

                }

                response.Success = true;
            }
            catch (Exception e)
            {
                response.Success = false;
                response.Error = e;
            }
            return response;
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
