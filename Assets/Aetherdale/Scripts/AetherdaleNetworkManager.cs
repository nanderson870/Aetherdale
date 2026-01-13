using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Collections;
using System.Linq;
using System.IO;
using UnityEngine.SceneManagement;

/*
	Documentation: https://mirror-networking.gitbook.io/docs/components/network-manager
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkManager.html
*/

public class AetherdaleNetworkManager : NetworkManager
{

    string clientPlayerId = string.Empty;

    // Overrides the base singleton so we don't
    // have to cast to this type everywhere.
    public static new AetherdaleNetworkManager singleton => (AetherdaleNetworkManager)NetworkManager.singleton;

    public static Action<AetherdaleNetworkManager> NetworkManagerCreated;

    bool isInTransition;

    public Action OnBeforeSceneChange;

    //string currentSceneName;


    /// <summary>
    /// Runs on both Server and Client
    /// Networking is NOT initialized when this fires
    /// </summary>
    public override void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Debug.Log("destroy non-singleton netman");
            Destroy(gameObject);
            return;
        }

        base.Awake();

        // Load spawnable prefabs from resources
        LoadSpawnablePrefabsFromResources<LootItem>("Loot Items");

        AddSpawnablePrefabs(AetherdaleData.GetAetherdaleData().entities.ToList());

        LoadSpawnablePrefabsFromResources<AreaOfEffect>("Explosions");
        LoadSpawnablePrefabsFromResources<AreaOfEffectTelegrapher>("Explosions/Telegraphers");
        LoadSpawnablePrefabsFromResources<Laser>("Lasers");
        LoadSpawnablePrefabsFromResources<Projectile>("Projectiles");
        LoadSpawnablePrefabsFromResources<WeaponBehaviour>("Items/Weapons/Weapon GameObjects");

        DontDestroyOnLoad(gameObject);
        
    }


    /// <summary>
    /// Runs on both Server and Client
    /// Networking is NOT initialized when this fires
    /// </summary>
    public override void Start()
    {
        base.Start();

        NetworkManagerCreated?.Invoke(this);
    }

    public void LoadSpawnablePrefabsFromResources<T> (string path)
    {
        List<T> loadedObjs = Resources.LoadAll(path, typeof(T)).Cast<T>().ToList();

        AddSpawnablePrefabs(loadedObjs);
    }

    public void AddSpawnablePrefabs<T>(List<T> prefabs)
    {
        foreach (T obj in prefabs)
        {
            Component comp = obj as Component;
            if (comp == null)
            {
                //Debug.LogWarning("Loaded prefab is not a component, skipping");
                continue;
            }

            GameObject gameObj = comp.gameObject;


            if (!singleton.spawnPrefabs.Contains(gameObj))// && gameObj != AetherdaleData.GetAetherdaleData().goldCoinsItem.GetLootEquivalentGameObject()) //coins spawn differently
            {
                singleton.spawnPrefabs.Add(gameObj);
            }
        }
    }

    #region Start & Stop

    /// <summary>
    /// Set the frame rate for a headless server.
    /// <para>Override if you wish to disable the behavior or set your own tick rate.</para>
    /// </summary>
    public override void ConfigureHeadlessFrameRate()
    {
        base.ConfigureHeadlessFrameRate();
    }

    /// <summary>
    /// called when quitting the application by closing the window / pressing stop in the editor
    /// </summary>
    public override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
    }

    #endregion

    #region Scene Management

    [SerializeField]

    /// <summary>
    /// This causes the server to switch scenes and sets the networkSceneName.
    /// <para>Clients that connect to this server will automatically switch to this scene. This is called automatically if onlineScene or offlineScene are set, but it can be called from user code to switch scenes again while the game is in progress. This automatically sets clients to be not-ready. The clients must call NetworkClient.Ready() again to participate in the new scene.</para>
    /// </summary>
    /// <param name="newSceneName"></param>
    public override void ServerChangeScene(string newSceneName)
    {
        Debug.Log("Server change scene " + newSceneName);
        StartCoroutine(ServerChangeSceneCoroutine(newSceneName));
    }

    IEnumerator ServerChangeSceneCoroutine(string newSceneName)
    {
        OnBeforeSceneChange?.Invoke();

        float timeRemaining = 0.5F; // Artificial delay for smooth transitions
        while (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            yield return null;
        }

        base.ServerChangeScene(newSceneName); // creates async operation
        if (loadingSceneAsync != null)
        {
            while (loadingSceneAsync.progress < 0.9F) yield return null;
        }

        timeRemaining = 0.5F; // Artificial delay for smooth transitions
        while (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            yield return null;
        }
    }


    /// <summary>
    /// Called on the server when a scene is completed loaded, when the scene load was initiated by the server with ServerChangeScene().
    /// </summary>
    /// <param name="sceneName">The name of the new scene.</param>
    public override void OnServerSceneChanged(string sceneName)
    {
        // Clear active player list, will re-populate as players join
        //Player.players.Clear();
    }

    /// <summary>
    /// Called on clients when a scene has completed loaded, when the scene load was initiated by the server.
    /// <para>Scene changes can cause player objects to be destroyed. The default implementation of OnClientSceneChanged in the NetworkManager is to add a player object for the connection if no player object exists.</para>
    /// </summary>
    public override void OnClientSceneChanged()
    {
        if (!isInTransition)
        {
            base.OnClientSceneChanged();
        }
    }

    #endregion

    #region Server System Callbacks

    /// <summary>
    /// Called on the server when a client is ready.
    /// <para>The default implementation of this function calls NetworkServer.SetClientReady() to continue the network setup process.</para>
    /// </summary>
    /// <param name="conn">Connection from client.</param>
    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        Debug.Log("OnServerReady");
        StartCoroutine(AddPlayerDelayed(conn));
    }

    // This delay is mostly for the host player that loads too fast for the
    // server to have subscenes async loaded from OnServerSceneChanged ahead of it.
    IEnumerator AddPlayerDelayed(NetworkConnectionToClient conn)
    {
        GameObject playerGO = null;
        // Search for existing Player object for connection
        foreach (NetworkIdentity thing in conn.owned)
        {
            if (thing.GetComponent<Player>() is Player netIdPlayer)
            {
                playerGO = netIdPlayer.gameObject;
            }
        }

        bool firstTimeSetup = false;
        if (playerGO == null)
        {
            firstTimeSetup = true;

            // No existing player object, perform first time setup
            playerGO = Instantiate(playerPrefab, GetStartPosition());
            playerGO.transform.SetParent(null);
            NetworkServer.AddPlayerForConnection(conn, playerGO); // Add player object, this will spawn it

            // Wait for end of frame before adding the player to ensure Scene Message goes first
            yield return null;

            playerGO.GetComponent<Player>().Initialize();
        }
        
        
        // Player should already be good to go, hook entity up to them now
        Player player = playerGO.GetComponent<Player>();
        player.CreateWraithForm();

        yield return null;

        player.SetControlledEntity(player.GetWraithForm());
        player.Restore();

        if (player.spectating)
        {
            player.StopSpectating();
        }


        if (firstTimeSetup)
        {
            // just make their id 'Player N'
            string playerId = "Player " + Player.players.Count;
            player.AssignPlayerId(playerId);
        }
        // else
        // {
        //     // existing player, just send ready message to it
        //     player.GetComponent<Player>().TargetLocalPlayerReady();
        // }
        

        NetworkServer.SetClientReady(conn);

        // TODO send this from somewhere else, once our data is properly initialized
        //Player.SendConnectedMessage(player); // Send chat message saying a player connected

    }


    /// <summary>
    /// Called on the server when a client adds a new player with ClientScene.AddPlayer.
    /// <para>The default implementation for this function creates a new player object from the playerPrefab.</para>
    /// </summary>
    /// <param name="conn">Connection from client.</param>
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);
    }

    /// <summary>
    /// Called on the server when a client disconnects.
    /// <para>This is called on the Server when a Client disconnects from the Server. Use an override to decide what should happen when a disconnection is detected.</para>
    /// </summary>
    /// <param name="conn">Connection from client.</param>
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        foreach (NetworkIdentity networkId in conn.owned)
        {
            Player idAsPlayer = networkId.gameObject.GetComponent<Player>();
            Debug.Log($"Server Disconnect {idAsPlayer}");
            if (idAsPlayer != null)
            {
                Player.players.Remove(idAsPlayer);
                //Player.SendDisconnectedMessage(idAsPlayer);
            }

            ControlledEntity idAsEntity = networkId.gameObject.GetComponent<ControlledEntity>();
            if (idAsEntity != null)
            {
                //idAsEntity.RpcCleanUp();
            }
        }

        base.OnServerDisconnect(conn);
    }

    /// <summary>
    /// Called on server when transport raises an error.
    /// <para>NetworkConnection may be null.</para>
    /// </summary>
    /// <param name="conn">Connection of the client...may be null</param>
    /// <param name="transportError">TransportError enum</param>
    /// <param name="message">String message of the error.</param>
    public override void OnServerError(NetworkConnectionToClient conn, TransportError transportError, string message) 
    {
        Debug.LogError("Server error - " + message);
    }

    #endregion

    #region Client System Callbacks

    /// <summary>
    /// Called on the client when connected to a server.
    /// <para>The default implementation of this function sets the client as ready and adds a player. Override the function to dictate what happens when the client connects.</para>
    /// </summary>
    public override void OnClientConnect()
    {
        base.OnClientConnect();
    }

    /// <summary>
    /// Called on clients when disconnected from a server.
    /// <para>This is called on the client when it disconnects from the server. Override this function to decide what happens when the client disconnects.</para>
    /// </summary>
    public override void OnClientDisconnect()
    {
    }

    /// <summary>
    /// Called on clients when a servers tells the client it is no longer ready.
    /// <para>This is commonly used when switching scenes.</para>
    /// </summary>
    public override void OnClientNotReady() { }

    /// <summary>
    /// Called on client when transport raises an error.</summary>
    /// </summary>
    /// <param name="transportError">TransportError enum.</param>
    /// <param name="message">String message of the error.</param>
    public override void OnClientError(TransportError transportError, string message)
    { 
        Debug.LogError("Client error - " + message);
    }


    #endregion

    #region Start & Stop Callbacks

    // Since there are multiple versions of StartServer, StartClient and StartHost, to reliably customize
    // their functionality, users would need override all the versions. Instead these callbacks are invoked
    // from all versions, so users only need to implement this one case.

    /// <summary>
    /// This is invoked when a host is started.
    /// <para>StartHost has multiple signatures, but they all cause this hook to be called.</para>
    /// </summary>
    public override void OnStartHost() { }

    /// <summary>
    /// This is invoked when a server is started - including when a host is started.
    /// <para>StartServer has multiple signatures, but they all cause this hook to be called.</para>
    /// </summary>
    public override void OnStartServer() 
    {
    }

    /// <summary>
    /// This is invoked when the client is started.
    /// </summary>
    public override void OnStartClient() 
    {
    }

    /// <summary>
    /// This is called when a host is stopped.
    /// </summary>
    public override void OnStopHost() { }

    /// <summary>
    /// This is called when a server is stopped - including when a host is stopped.
    /// </summary>
    public override void OnStopServer() { }

    /// <summary>
    /// This is called when a client is stopped.
    /// </summary>
    public override void OnStopClient() { }

    #endregion

}
