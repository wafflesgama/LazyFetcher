using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace LazyBuilder
{
    public class ServerManager
    {
        public enum ServerType
        {
            GIT,
            LOCAL
        }

        public static ServerData data { get; private set; }


        public delegate void ServerCallback();
        public static ServerCallback OnDisconnect;
        public static ServerCallback OnInvalidData;
        public static ServerCallback On404;

        private static Server server;

        public static void SetServer(Server _server)
        {
            server = _server;
        }
        public static async Task<bool> FetchServerData()
        {
            ServerResponse<string> response = await server.GetRawString("", PathFactory.MAIN_FILE, PathFactory.MAIN_TYPE);
            if (!response.Success)
            {
                if (response.Error is System.Net.WebException && response.Error.Message.Contains("404"))
                {
                    if (On404 != null)
                        On404.Invoke();
                }
                else if (response.Error is System.Net.WebException)
                {
                    if (OnDisconnect != null)
                        OnDisconnect.Invoke();
                }
                else if (response.Error is System.IO.FileNotFoundException)
                {
                    if (OnInvalidData != null)
                        OnInvalidData.Invoke();
                }

                return false;
            }
            try
            {
                data = JsonUtility.FromJson<ServerData>(response.Data);

                if (data.Items == null)
                    throw new Exception();
            }
            catch (Exception)
            {
                if (OnInvalidData != null)
                    OnInvalidData.Invoke();

                return false;
            }
            return true;

        }

        public static async Task<string> GetRawFile(string src, string savePath, string fileName, string fileType)
        {
            ServerResponse<string> response = await server.GetRawFile(src, savePath, fileName, fileType);

            if (!response.Success)
            {
                if (response.Error is System.Net.WebException && response.Error.Message.Contains("404"))
                {
                    if (On404 != null)
                        On404.Invoke();
                }
                else if (response.Error is System.Net.WebException)
                {
                    if (OnDisconnect != null)
                        OnDisconnect.Invoke();
                }
                else if (response.Error is System.IO.FileNotFoundException)
                {
                    if (OnInvalidData != null)
                        OnInvalidData.Invoke();
                }

                return string.Empty;
            }
            return response.Data;
        }

        public static async Task<string> GetRawString(string src, string fileName, string fileType)
        {
            ServerResponse<string> response = await server.GetRawString(src, fileName, fileType);

            if (!response.Success)
            {
                if (response.Error is System.Net.WebException && response.Error.Message.Contains("404"))
                {
                    if (On404 != null)
                        On404.Invoke();
                }
                else if (response.Error is System.Net.WebException)
                {
                    if (OnDisconnect != null)
                        OnDisconnect.Invoke();
                }
                else if (response.Error is System.IO.FileNotFoundException)
                {
                    if (OnInvalidData != null)
                        OnInvalidData.Invoke();
                }

            }
            return response.Data;
        }

        public static async Task<Texture2D> GetImage(string src, string saveFilePath, string imgName)
        {
            ServerResponse<Texture2D> response = await server.GetImage(src, saveFilePath, imgName);

            if (!response.Success)
            {
                if (response.Error is System.Net.WebException && response.Error.Message.Contains("404"))
                {
                    if (On404 != null)
                        On404.Invoke();
                }
                else if (response.Error is System.Net.WebException)
                {
                    if (OnDisconnect != null)
                        OnDisconnect.Invoke();
                }
                else if (response.Error is System.IO.FileNotFoundException)
                {
                    if (OnInvalidData != null)
                        OnInvalidData.Invoke();
                }
                return null;
            }
            return response.Data;
        }


        public static Server CreateServer(ServerType type, string src, string branch)
        {
            switch (type)
            {
                case ServerType.GIT:
                    //if (!src.StartsWith(Fetcher_Git.gitStandardPrefix)) return null;

                    return new Server_Git(src, branch);
                default:
                    return new Server_Local(src);
            }
        }
        public static List<string> GetServerTypes() => Enum.GetNames(typeof(ServerType)).ToList();

        public static string GetServerPath() => server.GetFullPath();
    }
}
