using Steamworks;
using Steamworks.Data;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Network.Framework
{
    public static class NW_SteamExtensions
    {
        public static event UnityAction<float> OnItemDownloading;

        public enum OverlayType : byte
        {
            Friends,
            Community,
            Players,
            Settings,
            OfficialGameGroup,
            Stats,
            Achievements
        }

        public enum LobbyVisibility : byte
        {
            Public,
            Private,
            FriendsOnly,
            Invisible,
        }

        [System.Serializable]
        public struct LobbyConfig
        {
            public string name;
            public bool joinable;
            public LobbyVisibility visibility;
            public byte maxMembers;
        }

        public static bool IsSteamRunning => SteamClient.IsValid;

        public const string GAME_ID = "game-example"; // Unique identifier for matchmaking so we don't match up with other Spacewar games

        /// <summary>
        /// Set visibility for lobby
        /// </summary>
        /// <param name="lobby"></param>
        /// <param name="visibility"></param>
        public static void SetVisibility(this Lobby lobby, LobbyVisibility visibility)
        {
            switch (visibility)
            {
                case LobbyVisibility.Public: lobby.SetPublic(); break;
                case LobbyVisibility.Private: lobby.SetPrivate(); break;
                case LobbyVisibility.FriendsOnly: lobby.SetFriendsOnly(); break;
                case LobbyVisibility.Invisible: lobby.SetInvisible(); break;
            }
        }

        /// <summary>
        /// Get shareable link for lobby (<b>NOTE:</b> Link should be open by web-browser. Link is a url-scheme/deep link, which automatically connects the game when requested)
        /// </summary>
        /// <param name="lobby"></param>
        /// <returns></returns>
        public static string GetShareableLink(this Lobby lobby) => $"steam://joinlobby/{SteamClient.AppId}/{lobby.Id}/{SteamClient.SteamId}";

        private const int CODE_APPEND_POS = 4;

        /// <summary>
        /// Get shareable code for lobby
        /// </summary>
        /// <param name="lobby"></param>
        /// <returns>XXXX-XXXX-XXXX-XXXX</returns>
        public static string GetShareableCode(this Lobby lobby)
        {
            var input = $"{lobby.Id:X16}";
            var builder = new System.Text.StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                if (i % CODE_APPEND_POS == 0 && i > 0)
                    builder.Append('-');

                builder.Append(input[i]);
            }

            return builder.ToString().Trim();
        }

        /// <summary>
        /// Get <seealso cref="SteamId"/> from shareable code
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static SteamId GetLobbyIdFromCode(this string code)
        {
            var hexCode = code.Replace("-", string.Empty).Trim();
            return System.Convert.ToUInt64(hexCode, fromBase: 16);
        }

        /// <summary>
        /// Get your friends list
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Friend> GetFriends()
        {
            if (!IsSteamRunning)
                return default;

            return SteamFriends.GetFriends();
        }

        /// <summary>
        /// Gets avatar texture form <paramref name="steamId"/>
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static async Task<Texture2D> GetAvatarTextureAsync(this SteamId steamId)
        {
            var image = await SteamFriends.GetLargeAvatarAsync(steamId);
            return await GetSteamImageAsTextureAsync(image ?? default);
        }

        /// <summary>
        /// Converts <paramref name="image"/> into <seealso cref="Texture2D"/>
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static async Task<Texture2D> GetSteamImageAsTextureAsync(this Image image)
        {
            return await Task.Run(() =>
            {
                var texture = new Texture2D((int)image.Width, (int)image.Width, TextureFormat.RGBA32, mipChain: false, linear: true);

                texture.LoadRawTextureData(image.Data);
                texture.Apply();

                return texture;
            });
        }

        /// <summary>
        /// Gets lobbies in the same immediate region
        /// </summary>
        /// <returns></returns>
        public static async Task<Lobby[]> GetLobbiesAsync()
        {
            if (!IsSteamRunning)
                return await Task.FromResult(new Lobby[0]);

            return await SteamMatchmaking.LobbyList
                .WithKeyValue("game", GAME_ID)
                .FilterDistanceClose()
                .WithMaxResults(max: 20)
                .RequestAsync();
        }

        /// <summary>
        /// Gets servers in the same immediate region
        /// </summary>
        /// <returns></returns>
        public static async Task<List<ServerInfo>> GetListOfServersAsync()
        {
            var servers = new List<ServerInfo>();

            using (var list = new Steamworks.ServerList.Internet())
            {
                //list.AddFilter("map", "de_dust");

                await list.RunQueryAsync();
                await Task.Yield();

                servers.AddRange(list.Responsive);
            }

            return servers;
        }

        /// <summary>
        /// Will begin to download a item from Steam workshop
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>
        public static async Task<bool> DownloadItemAsync(this PublishedFileId fileId) => await SteamUGC.DownloadAsync(fileId, progress: HandleItemDownloadingProgress, ct: workshopItemToken);

        /// <summary>
        /// Writes a file to the cloud (<b>NOTE:</b> Max size is 100 MiB) 
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="data"></param>
        public static void WriteFileToCloudAsync(string filename, byte[] data) => SteamRemoteStorage.FileWrite(filename, data);

        /// <summary>
        /// Reads a file from the cloud
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static byte[] ReadFileFromCloudAsync(string filename) => SteamRemoteStorage.FileRead(filename);

        private static void HandleItemDownloadingProgress(float progress) => OnItemDownloading?.Invoke(progress);

        private static CancellationToken workshopItemToken;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
#if UNITY_EDITOR
    Debug.unityLogger.logEnabled = true;
#else
    Debug.unityLogger.logEnabled = Debug.isDebugBuild || false;
#endif
            if (NetworkManager.Singleton == null || !IsSteamRunning)
                return;

            SteamFriends.ClearRichPresence();

            Application.quitting += Shutdown;

            var source = new CancellationTokenSource();
            workshopItemToken = source.Token;
        }

        private static void Shutdown()
        {
            Application.quitting -= Shutdown;
        }
    }
}