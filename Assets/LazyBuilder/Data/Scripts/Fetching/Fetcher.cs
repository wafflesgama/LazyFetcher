
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
    public interface Fetcher
    {
        public string GetSrcPath();
        public Task<string> GetRawFile(string src, string savePath, string fileName, string fileType);

        public Task<string> GetRawString(string src, string fileName, string fileType);

        public Task<Texture2D> GetImage(string src, string imgName, string imgType= PathFactory.THUMBNAIL_TYPE);

    }
}