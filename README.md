# UnitySteamNetcodeGameObjects
Code for using "Steamworks Network/Facepunch" solution with "Unity Netcode For GameObjects/MLAPI"

# How To Install

[Install first Unity Netcode For GameObjects](https://docs-multiplayer.unity3d.com/netcode/current/installation/install)<br/>
[Then install Facepunch Transport](https://github.com/Unity-Technologies/multiplayer-community-contributions/tree/main/Transports/com.community.netcode.transport.facepunch) Or [Copy this git command and paste in Package Manager](https://github.com/Unity-Technologies/multiplayer-community-contributions.git?path=/Transports/com.community.netcode.transport.facepunch)<br/>
[Then you have wiki page for getting started](https://wiki.facepunch.com/steamworks/)<br/>

# How To Use It:

You need to assign SteamId to FacepunchTransport

```c#
private void HandleTransport(SteamId id) => NetworkManager.Singleton.GetComponent<FacepunchTransport>().targetSteamId = id;
```

By using callback from steam, you can send that information to FacepunchTransport
```c#

private void Start() => SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested; // Add the callback

private void OnDestroy() => SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested; // Remove the callback

private void OnGameLobbyJoinRequested(Lobby lobby, SteamId id) => HandleTransport(id);
```

After you got the callbacks and also changing "targetSteamId" in FacepunchTransport, is pretty much the same stuff with Unity Netcode For GameObjects.<br/>
[Highly recommend watching DapperDino's playlist over Unity Netcode For GameObjects](https://www.youtube.com/playlist?list=PLS6sInD7ThM2_N9a1kN2oM4zZ-U-NtT2E)

# All the links

[Link to Multiplayer Community Contributions](https://github.com/Unity-Technologies/multiplayer-community-contributions/)<br/>
[Link to Facepunch Transport for Netcode for GameObjects](https://github.com/Unity-Technologies/multiplayer-community-contributions/tree/main/Transports/com.community.netcode.transport.facepunch)<br/>
[Link to Facepunch Wiki Page](https://wiki.facepunch.com/steamworks/)<br/>
[Link to Steamworks API](https://partner.steamgames.com/doc/api)<br/>
[Link to DapperDino's playlist over Unity Netcode For GameObjects](https://www.youtube.com/playlist?list=PLS6sInD7ThM2_N9a1kN2oM4zZ-U-NtT2E)
