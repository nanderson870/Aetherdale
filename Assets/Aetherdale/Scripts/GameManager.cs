using System.Collections;
using Steamworks;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    public bool alwaysUseNonSteam = false;

    public static GameManager singleton;
    public static AetherdaleNetworkManager networkManager;

    public static bool joiningGame = false;

    void Awake()
    {
        if (singleton == null)
        {
            singleton = this;
            
            DontDestroyOnLoad(gameObject);

            //SceneManager.activeSceneChanged += SceneLoaded;
        }
        else
        {
            if (singleton != this)
            {
                Destroy(gameObject);
                return;
            }
        }


        Settings.InitSettings();
    }

    void OnDestroy()
    {
        Debug.Log(this + " destroyed");
    }

    // void SceneLoaded(Scene _, Scene _2)
    // {
    //     Debug.Log("scene loaded");
    //     if (joiningGame)
    //     {
    //         Debug.Log("Scene loaded during join");
    //         StartCoroutine(JoinAsClientCoroutine());
    //     }
    // }


    public static bool RunningViaSteam()
    {
        return /*SteamAPI.IsSteamRunning() && */ !singleton.alwaysUseNonSteam;
    }

    public static void CreateNetworkManager()
    {
        if (networkManager != null)
        {
            throw new System.Exception("Tried to create a network manager when one already existed");
        }
        
        if (RunningViaSteam())
        {
            networkManager = Instantiate(AetherdaleData.GetAetherdaleData().steamNetworkManagerPrefab);
        }
        else
        {
            networkManager = Instantiate(AetherdaleData.GetAetherdaleData().nonsteamNetworkManagerPrefab);
        }

        DontDestroyOnLoad(networkManager.gameObject);
    }

    /// <summary>
    /// Start game (from menu)
    /// </summary>
    public static void StartGame()
    {
        if (networkManager == null)
        {
            CreateNetworkManager();
        }

        if (RunningViaSteam())
        {
            SteamLobby lobby = FindObjectsByType<SteamLobby>(FindObjectsSortMode.None)[0];
            lobby.HostLobby(); // will eventually StartHost() (once lobby created)
        }
        else
        {
            // Without steam we go directly to StartHost()
            networkManager.StartHost();
        }
    }

    /// <summary>
    /// Return to menu
    /// </summary>
    public static void StopGame()
    {
        if (RunningViaSteam())
        {
            SteamLobby lobby = FindObjectsByType<SteamLobby>(FindObjectsSortMode.None)[0];
            lobby.LeaveLobby();
        }
        else
        {
            if (networkManager != null)
            {
                networkManager.StopHost();
            }
        }
    }

    public static void JoinGame()
    {
        GameManager.StopGame();

        singleton.JoinAsClient();

    }

    void JoinAsClient()
    {
        if (networkManager != null)
        {
            //Don't want whatever this is
            Destroy(networkManager.gameObject);
        }
        

        StartCoroutine(JoinAsClientCoroutine());
        //StartCoroutine(AlwaysTrue());
        joiningGame = true;
    }

    IEnumerator JoinAsClientCoroutine()
    {
        while (networkManager != null)
        {
            // Debug.Log("Waiting for previous network manager to tear down...");
            yield return null;
        }

        // Debug.Log("Network manager removed, re-creating to join as client");
        CreateNetworkManager();

        // Debug.Log("MADE IT HERE");
        networkManager.StartClient();

        joiningGame = false;
        Time.timeScale = 1;
    }
}
