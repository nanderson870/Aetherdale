using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.UI;

// Thanks to Zyger's video on this

public class SteamLobby : MonoBehaviour
{
    public static SteamLobby singleton;


    #region CALLBACKS
    protected Callback<LobbyCreated_t> LobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> LobbyJoinRequest;
    protected Callback<LobbyEnter_t> LobbyEntered;
    #endregion

    public CSteamID currentLobbyID;
    private const string HOST_ADDRESS_KEY = "HostAddress";

    void Start()
    {
        if (singleton != null)
        {
            Destroy(gameObject);
            return;
        }
        singleton = this;

        if (!GameManager.RunningViaSteam() || !SteamManager.Initialized)
        {
            Debug.LogError("Steam Manager is not initialized when starting lobby");
            return;
        }

        LobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        LobbyJoinRequest = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);
        LobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEnter);

        DontDestroyOnLoad(gameObject);
    }

    public void HostLobby()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, AetherdaleNetworkManager.singleton.maxConnections);
    }

    public void LeaveLobby()
    {
        SteamMatchmaking.LeaveLobby(currentLobbyID);

        AetherdaleNetworkManager.singleton.StopHost();
    }

	void OnLobbyCreated(LobbyCreated_t pCallback) 
    {
        if (pCallback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("Callback was not okay");
            return;
        }

        currentLobbyID = new(pCallback.m_ulSteamIDLobby);

        AetherdaleNetworkManager.singleton.StartHost();

        SteamMatchmaking.SetLobbyData(new CSteamID(pCallback.m_ulSteamIDLobby), HOST_ADDRESS_KEY, SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(new CSteamID(pCallback.m_ulSteamIDLobby), "name", SteamFriends.GetPersonaName());
	}


    /// <summary>
    /// Join requested by invitee/client
    /// </summary>
    /// <param name="pCallback"></param>
    void OnJoinRequest(GameLobbyJoinRequested_t pCallback)
    {
        Debug.LogError("Join request....");
        SteamMatchmaking.JoinLobby(pCallback.m_steamIDLobby);
    }

	void OnLobbyEnter(LobbyEnter_t pCallback) 
    {
        Debug.LogError("Joining lobby....");
        if (!NetworkServer.active)
        {
            currentLobbyID = new(pCallback.m_ulSteamIDLobby);

            if (GameManager.networkManager == null)
            {
                GameManager.CreateNetworkManager();
            }

            AetherdaleNetworkManager.singleton.networkAddress = SteamMatchmaking.GetLobbyData(currentLobbyID, HOST_ADDRESS_KEY);

            AetherdaleNetworkManager.singleton.StartClient();

            GameManager.joiningGame = false;
            Time.timeScale = 1;
        }
	}

}
