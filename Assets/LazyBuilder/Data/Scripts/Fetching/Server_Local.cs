using System;
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

        public async Task<ServerResponse<string>> GetRawFile(string src, string savePath, string fileName, string fileType)
        {
            ServerResponse<string> response = new ServerResponse<string>();
            try
            {

                var savedFile = $"{savePath}\\{fileName}.{fileType}";

                string savedFilePath = $"{PathFactory.absoluteToolPath}\\{savePath}\\{fileName}.{fileType}";
                var srcFilePath = $"{serverPath}\\{src}\\{fileName}.{fileType}";

                savedFilePath = savedFilePath.AbsoluteFormat();
                srcFilePath = srcFilePath.AbsoluteFormat();

                if (File.Exists(savedFilePath))
                    File.Delete(savedFilePath);

                File.Copy(srcFilePath, savedFilePath);

                //Since the lack of File.CopyAsync - Added hammered workaround 
                for (int i = 0; i < 9000; i++)
                {
                    if (File.Exists(savedFilePath)) break;
                    await Task.Delay(10);
                }

                response.Data = savedFilePath;
                response.Success = true;
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
                src = src.AbsoluteFormat();
#if UNITY_2021_1_OR_NEWER
                response.Data = await File.ReadAllTextAsync($"{serverPath}\\{src}\\{fileName}.{fileType}");
#else
                response.Data =  File.ReadAllText($"{serverPath}\\{src}\\{fileName}.{fileType}");
#endif
                response.Success = true;
            }
            catch (Exception e)
            {
                response.Success = false;
                response.Error = e;
            }
            return response;
        }
        public async Task<ServerResponse<Texture2D>> GetImage(string src, string saveFilePath, string imgName)
        {
            ServerResponse<Texture2D> response = new ServerResponse<Texture2D>();
            try
            {
                src = src.AbsoluteFormat();

                var path = $"{serverPath}\\{src}\\{imgName}.{PathFactory.THUMBNAIL_TYPE}";

                if (!File.Exists(path)) return null;

                byte[] imageBytes = File.ReadAllBytes(path);

                response.Data = new Texture2D(2, 2);
                response.Data.LoadImage(imageBytes);


                if (saveFilePath != null)
                {
                    if (!File.Exists(saveFilePath))
                        File.Create(saveFilePath).Close();

#if UNITY_2021_1_OR_NEWER
                    await File.WriteAllBytesAsync(saveFilePath, imageBytes);
#else
                    File.WriteAllBytes(saveFilePath, imageBytes);
#endif
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
