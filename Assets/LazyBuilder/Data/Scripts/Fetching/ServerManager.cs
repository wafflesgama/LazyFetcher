using Newtonsoft.Json;
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

        public static Server server { get; private set; }
        public static ServerData data { get; private set; }

        public static void SetServer(Server _server)
        {
            server = _server;
        }
        public static async Task<bool> FetchServerData()
        {
            try
            {
                var rawData = await server.GetRawString("", PathFactory.MAIN_FILE, PathFactory.MAIN_TYPE);

                if (rawData == null) return false;

                //Debug.Log(rawData);
                data = JsonConvert.DeserializeObject<ServerData>(rawData);
                return true;
            }
            catch (Exception)
            {
                return false;
            }

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
    }
}
