using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.SceneManagement;
using System.Linq;
using FMODUnity;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class Player : NetworkBehaviour, IStoryEventHandler
{
    public const float TRANSFORM_TIMEOUT = 0.5F;
    public const int STARTING_TRAIT_REROLLS = 3;

    public readonly static Dictionary<Player, ControlledEntity> players = new();

    [SerializeField] ControlledEntity defaultEntity;
    [SerializeField] PlayerUI uiPrefab;
    [SerializeField] PlayerCamera cameraPrefab;

    
    [SyncVar(hook = nameof(OnControlledEntityChanged))] ControlledEntity controlledEntity = null;
    [SerializeField] TrinketData[] tempDefaultTrinkets;
    [SerializeField] IdolItemData[] tempDefaultIdols;
    [SerializeField] WeaponData[] tempDefaultWeapons;

    [SerializeField] EventReference levelupSound;

    PlayerInput playerInput;
    PlayerDataRuntime playerData;

    [SyncVar] PlayerUI playerUI;
    
    [SyncVar(hook = nameof(OnLevelSyncVarChanged))] public int level = 1;
    [SyncVar(hook = nameof(OnExperienceSyncVarChanged))] public ulong experienceThisLevel = 0;

    [SyncVar(hook = nameof(OnPlayerCameraChanged))] PlayerCamera playerCamera = null;
    [SyncVar(hook = nameof(SetPlayerId))] string playerId;

    [SyncVar(hook = nameof(WraithFormChanged))] PlayerWraith wraithForm;
    [SyncVar(hook = nameof(IdolFormChanged))] IdolForm idolForm;

    [SyncVar] public int traitRerolls = 3;

    public Action<int> OnClientLevelUp;

    IdolItem idolItem;
    Trinket trinket;

    // Mildly different than username as this is what gets given to other clients.
    // They should usually be the same
    [SyncVar] string displayName = ""; 

    [SyncVar] public bool spectating = false;

    public bool isDead = false;
 
    [SyncVar] bool inCutscene = false;
    
    [Command]
    public void CmdSetInCutscene(bool inCutscene) 
    {
        this.inCutscene = inCutscene;
    } 
 
    public readonly SyncDictionary<string, float> stats = new();
 

    float lastTransform = 0.0F;


    // Persistent stats
    float wraithHealthRatio = 1.0F;
    float idolHealthRatio = 1.0F;
    float idolDeathTimeout = 0.0F;


    public TraitList traitList = new();
    [SyncVar] public int traitsOwed = 0; // Number of traits owed from level ups - total number of traits that can be taken now (excluding pending)
    public readonly SyncList<TraitInfo> pendingTraitOptions = new(); // Holds the most recently seen trait options so that opening and closing the menu doesn't reset
    public Action<Player> OnPendingTraitOptionsChanged;
    

    /// <summary> The player can have one weapon that is only available until the run ends </summary>
    [SyncVar] string currentSequenceWeaponID = "";
    

    // Mirrors Entity/ControlledEntity OnEntityKilled event
    public Action<HitInfo> OnEntityKilled;

    public static Action<Quest> OnLocalPlayerQuestReceived;

    public delegate void PlayerBossAction(Boss boss);
    public event PlayerBossAction OnBossDefeated;

    public static Action<ChatMessageType, string> OnChatMessage;


    public static Action OnAllPlayersDied;


    /// <summary>Called on Server when this player is entirely ready</summary>
    public Action OnReady;

    /// <summary>Called on Client when the locally controlled player is ready</summary>
    public static Action OnLocalPlayerReady; 

    public static bool IsLocalPlayerReady {get; private set;}

    public Action<ControlledEntity> OnEntityChangedOnClient;
    public Action<IdolForm> OnNewIdolFormOnClient;
    public Action<PlayerWraith> OnNewWraithFormOnClient;

    public Action<Trinket> OnTrinketEquippedOnClient;
    public Action<IdolItem> OnIdolEquippedOnClient;

    public Action<TraitList> OnTraitsChangedOnClient;

    public Action<string> ClientOnAreaCompleted;

    public Action<string> OnStoryEvent;

    public Action<Item> OnAcquireItem;

    public Action OnNextRevive;

    string localPlayerId = ""; // only has a value on the client who owns this player. used to determine whose data we write to our save file

    bool markedReadyByClient = false;

    public static Player GetLocalPlayer()
    {
        if (NetworkClient.localPlayer == null)
        {
            return null;
        }

        return NetworkClient.localPlayer.GetComponent<Player>();
    }

    
    public static List<Player> GetPlayers() {return players.Keys.ToList();}
    public static List<ControlledEntity> GetPlayerEntities() 
    {
        List<ControlledEntity> ret = players.Values.ToList();

        // Scrub list incase any players don't currently have controlled entities
        for (int i = ret.Count - 1; i >= 0; i--)
        {
            if (ret[i] == null)
            {
                ret.RemoveAt(i);
            }
        }

        return ret;
    }

    public static Player GetPlayer(string playerId)
    {
        foreach (Player player in players.Keys)
        {
            if (player.GetPlayerId() == playerId)
            {
                return player;
            }
        }

        return null;
    }

    public static void ResetInventory()
    {
        foreach (Player player in GetPlayers())
        {
            player.GetInventory().Reset();
        }
    }


    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    void Start()
    {
        DontDestroyOnLoad(gameObject);

        SceneManager.activeSceneChanged += OnSceneChanged;

        if (NetworkServer.active)
        {
            AreaSequencer.OnRunEnded += OnRunEnded;
        }

        AetherdaleNetworkManager.singleton.OnBeforeSceneChange += OnBeforeSceneChange;

        pendingTraitOptions.Callback = PendingTraitOptionsChanged;
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        SendLocalChatMessage(ChatMessageType.EnvironmentMessage, "OnStartLocalPlayer");

        LocalPlayerReady();
    }

    public void Update()
    {
        if (idolForm != null)
        {
            idolForm.ProcessPeriodics();
        }

        if (wraithForm != null)
        {
            wraithForm.ProcessPeriodics();
        }

        if (isOwned)
        {
            playerInput.ReadInput(this);

            if (playerUI != null)
                playerUI.ProcessInput();

            if (controlledEntity != null && !controlledEntity.InGUI() && Time.timeScale != 0)
            {
                controlledEntity.ProcessInput();
            }
        }
    }


    #region Setup and Teardown

    void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
        OnChatMessage -= ReceiveChat;
        
        // We can't rely on isOwned here because apparently the NetworkIdentity gets dismantled before us
        // So we check localPlayerId, which is only set on our own client, and equal to the playerId
        if (!isServerOnly && localPlayerId == playerId)
        {
            PlayerDataRuntime.Save(playerData);
        }
    }

    void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        if (NetworkServer.active && AreaSequencer.GetAreaSequencer().IsSequenceRunning())
        {
            AreaSequencer.GetAreaSequencer().PlayerReady(this);
        }
    }

    [Server]
    void OnRunEnded(bool victory)
    {
        ResetLevelAndExperience();
    }


    [Server]
    public void Initialize()
    {
        OnChatMessage += ReceiveChat;

        traitList.OnModified += (Trait[] traits) =>
        {
            TraitInfo[] traitInfos = traits.Select(trait => trait.ToInfo()).ToArray();
            TargetUpdateTraitList(traitInfos);
        };

        TargetInitializePlayerData();

        StartCoroutine(nameof(WaitForPlayerDataAndFinishInitialization));
    }

    IEnumerator WaitForPlayerDataAndFinishInitialization()
    {
        while (!markedReadyByClient)
        {
            Debug.Log("Server waiting for data for new player");
            yield return new WaitForSeconds(0.25F);
        }

        InitializeCamera();
        InitializeUI();

        yield return null;

        ServerPlayerReady();
    }


    [Server]
    void ServerPlayerReady()
    {
        if (AreaSequencer.GetAreaSequencer().IsSequenceRunning())
        {
            AreaSequencer.GetAreaSequencer().PlayerReady(this);
        }

        OnReady?.Invoke();

        InvokeRepeating(nameof(ProcessTraits), 1, 0.25F);

        //Debug.Log("Player " + GetPlayerId() + " is ready!");
        if (AllPlayersReady())
        {
            // Send message for all receivers of OnAllPlayersReady
            foreach (IOnAllPlayersReadyTarget target in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<IOnAllPlayersReadyTarget>())
            {
                try
                {
                    target.OnAllPlayersReady();
                }
                catch (Exception e)
                {
                    Debug.LogError(e.ToString());
                }
            }

        }

        TargetLocalPlayerReady();
    }

    
    [TargetRpc]
    public void TargetLocalPlayerReady()
    {
        LocalPlayerReady();
    }

    [Client]
    void LocalPlayerReady()
    {
        Debug.Log("Local player ready");

        OnLocalPlayerReady?.Invoke();

        IsLocalPlayerReady = true;

        // Send message for all receivers of OnLocalPlayerReady
        foreach (IOnLocalPlayerReadyTarget target in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<IOnLocalPlayerReadyTarget>())
        {
            try
            {
                target.OnLocalPlayerReady(this);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }

        ConditionalObject.ReevaluateAll();

        foreach (Trait trait in GetTraits())
        {
            trait.OnNewArea(this);
        }
    }

    [Server]
    public void InitializeCamera()
    {
        if (playerCamera == null)
        {
            PlayerCamera cam = Instantiate(cameraPrefab);
            NetworkServer.Spawn(cam.gameObject, connectionToClient);

            playerCamera = cam; // only once spawned can we assign it to our syncvar
        }
        else
        {
            Debug.LogError("Tried to init camera while one exists");
        }
    } 

    [Server]
    public void InitializeUI()
    {
        if (playerUI == null)
        {
            PlayerUI ui = Instantiate(uiPrefab);
            NetworkServer.Spawn(ui.gameObject, connectionToClient);

            playerUI = ui; // only once spawned can we assign it to our syncvar
            playerUI.SetOwningPlayer(this);
            playerUI.gameObject.SetActive(true);

            playerUI.RpcSetCamera(playerCamera);

            TargetInitializeUI();
        }
    }

    [TargetRpc]
    public void TargetInitializeUI()
    {
        PlayerLevelBar.SetLevelAndExperience(level, experienceThisLevel);
    }

    [TargetRpc]
    public void TargetInitializePlayerData()
    {
        playerData = PlayerDataRuntime.Load();
        playerData.RegisterCallbacks(this);

        if (playerData.EquippedIdol != null)
        {
            CmdSetIdol(IdolItem.Serialize(playerData.EquippedIdol));
        }

        if (playerData.EquippedTrinket != null)
        {
            CmdSetTrinket(Trinket.Serialize(playerData.EquippedTrinket));
        }

        // TODO development only
        foreach (IdolItemData idol in tempDefaultIdols)
        {
            if (!playerData.HasItem(idol.GetItemID()))
            {
                Debug.Log("giving player the default Idol");
                TargetAddAccountItem(idol.GetItemID(), 1);
            }
        }

        foreach (WeaponData weapData in tempDefaultWeapons)
        {
            if (playerData.GetWeapon(weapData.GetItemID()) == null)
            {
                Debug.Log("Giving player " + weapData.GetName());
                TargetAddAccountItem(weapData.GetItemID(), 1);
            }
        }

        foreach (TrinketData trinket in tempDefaultTrinkets)
        {
            if (!playerData.HasItem(trinket.GetItemID()))
            {
                Debug.Log("giving player the default Trinket");
                TargetAddAccountItem(trinket.GetItemID(), 1);
            }
        }

        if (playerData.EquippedIdol != null)
        {
            // TODO cross-reference with inventory instead of just giving a new Idol

            idolItem = playerData.EquippedIdol;
        }

        QuestData starterQuestData = AetherdaleData.GetAetherdaleData().starterQuest;
        if (!playerData.HasQuest(starterQuestData))
        {
            Quest starterQuest = new(starterQuestData);
            GiveQuest(starterQuest);
        }


        if (playerData.EquippedTrinket != null)
        {
            SetTrinket(playerData.EquippedTrinket);
            //trinket = playerData.EquippedTrinket;
            //trinket.OnTrinketUsed += (Trinket usedTrinket) => {playerUI.RpcItemCooldownStart(usedTrinket.GetItemID(), usedTrinket.GetCooldown());};
        }

        StartCoroutine(nameof(FinalizeInitialization));

    }

    IEnumerator FinalizeInitialization()
    {
        while (!connectionToServer.isReady)
        {
            yield return null;
        }

        yield return StartCoroutine(nameof(GetSteamUsername));

        CmdMarkReadyOnServer();
    }



    /// <summary>
    /// First time setup
    /// </summary>
    [Command]
    void CmdMarkReadyOnServer()
    {
        markedReadyByClient = true;
    }

    [Server]
    public static bool AllPlayersReady()
    {
        foreach (Player player in GetPlayers())
        {
            if (!player.markedReadyByClient)
            {
                return false;
            }
        }

        return true;
    }


    public void CreateWraithForm()
    {
        StartCoroutine(CreateWraithFormCoroutine());
    }

    IEnumerator CreateWraithFormCoroutine()
    {
        Transform start = AetherdaleNetworkManager.singleton.GetStartPosition();

        PlayerWraith newWraith = Instantiate(AetherdaleData.GetAetherdaleData().wraithPrefab, start);
        newWraith.transform.SetParent(null);
        NetworkServer.Spawn(newWraith.gameObject, connectionToClient);

        yield return null;
        // do
        // {
        //     yield return null; // Skip a frame for wraith to init/spawn/etc
        // } while (playerData == null); // then wait for data to init before continuing, too
        
        SetWraithForm(newWraith);
        SetControlledEntity(wraithForm);
    }


    #endregion


    #region Persistent Stats

    void OnBeforeSceneChange()
    {
        if (wraithForm != null)
        {
            wraithHealthRatio = wraithForm.GetHealthRatio();
        }

        if (idolForm != null)
        {
            idolHealthRatio = idolForm.GetHealthRatio();
            idolDeathTimeout = idolForm.GetDeathTimeout();
        }
        else
        {
            idolHealthRatio = 1.0F;
        }
    }


    public void ResurrectInSeconds(float seconds)
    {
        Invoke(nameof(Resurrect), seconds);
    }

    void Resurrect()
    {
        Restore();
    }

    /// <summary>
    /// Restores entity health, clears dead flag
    /// </summary>
    public void Restore(float wraithHealthRatio = 1.0F, float idolHealthRatio = 1.0F)
    {
        if (wraithForm == null)
        {
            // Must have wraith form at minimum
            CreateWraithForm();
        }

        wraithForm.PlayAnimation("Idle", 0.01F);

        if (wraithHealthRatio > 0)
        {
            wraithForm.SetHealthRatio(wraithHealthRatio);
        }

        if (idolForm != null && idolHealthRatio > 0)
        {
            idolForm.SetDeathTimeout(0);
            idolForm.SetHealthRatio(idolHealthRatio);
            idolForm.isDead = false;
            idolForm.resurrecting = false;
        }

        Debug.Log("is dead at time of rpc? " + isDead);
        RpcOnRestored(wraithForm.resurrecting);

        isDead = false;
        wraithForm.isDead = false;
        wraithForm.resurrecting = false;

        StartCoroutine(RestoreCoroutine());
    }

    [ClientRpc]
    void RpcOnRestored(bool effectResurrection)
    {
        Debug.Log("ON RESTORED");
        OnNextRevive?.Invoke();
        OnNextRevive = null;

        Debug.Log(effectResurrection);
        if (effectResurrection)
        {
            AudioManager.Singleton.PlayOneShot(AetherdaleData.GetAetherdaleData().soundData.resurrectedSound, GetControlledEntity().GetWorldPosCenter());
        }
    }

    IEnumerator RestoreCoroutine()
    {   
        yield return null;
        StopSpectating();
    }

    public void ResetTraitRerolls()
    {
        traitRerolls = STARTING_TRAIT_REROLLS;
    }

    #endregion


    #region Player Data
    IEnumerator GetSteamUsername()
    {
        float startTime = Time.time;

        if (GameManager.RunningViaSteam())
        {
            while (!SteamManager.Initialized)
            {
                if (Time.time - startTime <= 10.0F)
                {
                    Debug.LogWarning("Steam did not initialize, username is unknown");
                    break;
                }


                SetUsername(GetPlayerId());

                yield return null;
            }

            SetUsername(SteamFriends.GetPersonaName());
        }
        else
        {
            SetUsername("Player");
        }
    }


    public PlayerDataRuntime GetPlayerData()
    {
        return playerData;
    }

    [Server]
    public void ResetLevelAndExperience()
    {
        experienceThisLevel = 0;
        level = 1;
    }

    [Server]
    public void GiveExperience(int experience)
    {
        AddExperience(experience);
    }
    
    [Server]
    public void AddExperience(int experience)
    {
        float mult = GetStat(Stats.ExperienceMultiplier, 1.0F);

        int experienceGained = (int) (experience * mult);

        experienceThisLevel = experienceThisLevel + (ulong) experienceGained;

        while (experienceThisLevel >= Equation.PLAYER_EXP_PER_LEVEL.Calculate(level))
        {
            LevelUp();
        }
    }

    [Client]
    void OnExperienceSyncVarChanged(ulong oldExperience, ulong newExperience)
    {
        if (isOwned)
        {
            PlayerLevelBar.SetLevelAndExperience(level, experienceThisLevel);
        }
    }

    [Client]
    void OnLevelSyncVarChanged(int oldLevel, int newLevel)
    {
        if (isOwned)
        {
            PlayerLevelBar.SetLevelAndExperience(level, experienceThisLevel);

            if (newLevel > oldLevel)
            {
                AudioManager.Singleton.PlayOneShot(levelupSound);
                OnClientLevelUp?.Invoke(newLevel);
            }
        }
    }

    

    public Color GetIdolResourceColor()
    {
        if (playerData == null || !playerData.UnlockData.idolsUnlocked || idolItem == null)
        {
            return Color.grey;
        }

        if (idolForm != null && idolForm.GetDeathTimeout() > 0)
        {
            return Color.grey;
        }

        return idolItem.GetResourceColor();
    }

    
    [Client]
    public void SetUsername(string username)
    {
        playerData.SetUsername(username);
        CmdUpdateDisplayName(username);
    }

    [Command]
    void CmdUpdateDisplayName(string newDisplayName)
    {
        displayName = newDisplayName;
    }

    public string GetUsername()
    {
        return playerData.Username;
    }

    public string GetDisplayName()
    {
        return displayName;
    }

    [Server]
    public void AssignPlayerId(string id)
    {
        playerId = id;
    }

    public void SetPlayerId(string oldId, string newId)
    {
        // Update client-only Id for our local player
        if (!isServerOnly && isOwned)
        {
            localPlayerId = playerId;
        }
    }

    public string GetPlayerId()
    {
        return playerId;
    }

    #endregion




    #region Traits / Stats

    [Server]
    void SetDefaultStats()
    {
        SetStat(Stats.InactiveFormHealthRegen, 1.0F);
        SetStat(Stats.TrinketCooldownMultiplier, 1.0F);
        SetStat(Stats.ExperienceMultiplier, 1.0F);
        SetStat(Stats.TraitOptions, 3);
        SetStat(Stats.Luck, 1);

        wraithForm.SetDefaultStats();

        if (idolForm != null)
        {
            idolForm.SetDefaultStats();
        }
    }

    
    [Server]
    public void LevelUp()
    {
        int remainder = (int) (experienceThisLevel - Equation.PLAYER_EXP_PER_LEVEL.Calculate(level));
        if (remainder < 0)
        {
            return;
        }

        level = level + 1;
        experienceThisLevel = (ulong) remainder;

        if (AreaSequencer.GetAreaSequencer().IsSequenceRunning()) //TODO will it ever not be?
        {
            traitsOwed++;
            GetUI().TargetTraitSelectionAvailable(true);
        }
    }

    [Server]
    public void CreatePendingTraitOption()
    {
        if (pendingTraitOptions.Count > 0)
        {
            throw new Exception("Cannot create pending trait option - traits already pending");
        }

        if (traitsOwed <= 0)
        {
            throw new Exception("Cannot create pending trait option - no traits owed");
        }

        traitsOwed--;
        pendingTraitOptions.Clear();

        int numTraitOptions = (int) GetStat(Stats.TraitOptions, 3);
        List<Trait> traitOptions = Trait.GetLevelupTraits(this, numTraitOptions);

        foreach (Trait trait in traitOptions)
        {
            pendingTraitOptions.Add(trait.ToInfo());
        }
    }

    // SyncVar Hook
    private void PendingTraitOptionsChanged(SyncList<TraitInfo>.Operation operation, int arg2, TraitInfo info1, TraitInfo info2)
    {
        OnPendingTraitOptionsChanged?.Invoke(this);
    }

    [Server]
    public void RerollPendingTraitOption()
    {
        if (pendingTraitOptions.Count == 0)
        {
            throw new Exception("Cannot reroll traits - no trait selection available");
        }

        traitRerolls--;
        pendingTraitOptions.Clear();

        int numTraitOptions = (int)GetStat(Stats.TraitOptions, 3);
        List<Trait> traitOptions = Trait.GetLevelupTraits(this, numTraitOptions);

        foreach (Trait trait in traitOptions)
        {
            pendingTraitOptions.Add(trait.ToInfo());
        }
    }


    [TargetRpc]
    void TargetUpdateTraitList(TraitInfo[] traitInfos)
    {
        Trait[] traits = traitInfos.Select(traitInfo => traitInfo.ToTrait()).ToArray();

        // Only SetTraits if we aren't a server. Otherwise this just causes an infinite loop that comes right back here.
        if (isClientOnly)
        {
            traitList.SetTraits(traits);
        }
        
        OnTraitsChangedOnClient?.Invoke(traitList);
    }
    
    [Command]
    public void CmdRequestTraitSelection(TraitInfo traitInfo)
    {
        foreach (TraitInfo traitOption in pendingTraitOptions)
        {
            if (traitOption.traitName == traitInfo.traitName)
            {
                AddTrait(traitOption.ToTrait());
                pendingTraitOptions.Clear();
                break;
            }
        }
        
        if (pendingTraitOptions.Count > 0)
        {
            GetUI().TargetTraitSelectionAvailable(true);
        }
    }

    public float GetStat(string name, float notFoundValue = 0.0F)
    {
        if (!stats.ContainsKey(name))
        {
            return notFoundValue;
        }

        return stats[name];
    }

    public void SetStat(string name, float value)
    {
        if (!stats.ContainsKey(name))
        {
            stats.Add(name, value);
        }
        else
        {
            stats[name] = value;
        }
    }

    [Server]
    public void AddTrait(Trait trait)
    {
        traitList.AddTrait(this, trait);

        TargetTraitAcquired(trait.ToInfo());

        ApplyTraits();
    }

    [TargetRpc]
    void TargetTraitAcquired(TraitInfo traitInfo)
    {
        AudioManager.Singleton.PlayOneShot(traitInfo.ToTrait().GetAcquiredSound());
    }


    public TraitList GetTraits()
    {
        return traitList;
    }

    [Server]
    public void ResetTraits()
    {
        pendingTraitOptions.Clear();
        GetUI().TargetTraitSelectionAvailable(false);

        if (traitList != null)
        {
            traitList.SetTraits(new Trait[0]);
        }
    }

    [Server]
    public void ApplyTraits()
    {
        stats.Clear();

        SetDefaultStats();
        
        // Apply stat changes
        foreach (StatChange statChange in traitList.GetStatChanges())
        {
            if (statChange.stat == Stats.InactiveFormHealthRegen
             || statChange.stat == Stats.TrinketCooldownMultiplier 
             || statChange.stat == Stats.ExperienceMultiplier 
             || statChange.stat == Stats.TraitOptions
             || statChange.stat == Stats.Luck)
            {
                float previousValue = GetStat(statChange.stat);
                SetStat(statChange.stat, previousValue + GetStatChangeFlatAmount(GetStat(statChange.stat), statChange));
            }
        }

        // Now apply any applicable entity traits
        if (controlledEntity != null)
        {
            controlledEntity.ApplyTraits();
        }
    }

    [Server]
    public void ProcessTraits()
    {
        foreach (Trait trait in GetTraits().ToProcOrderList())
        {
            trait.OnProcessTraits(this);
        }
    }
    
    float GetStatChangeFlatAmount(float originalStatValue, StatChange statChange)
    {
        if (statChange.calcMode == StatChangeType.Flat)
        {
            return statChange.amount;
        }
        else if (statChange.calcMode == StatChangeType.Multiplier)
        {
            return statChange.amount * originalStatValue;
        }

        throw new Exception("Stat change was not of a supported type: " + statChange.ToString());
    }
    #endregion


    public void SetInSequence(bool inSequence)
    {
        if (!inSequence)
        {
            ResetTraits();
        }
    }




    #region Transformations

    public IdolItem GetIdolItem()
    {
        return idolItem;
    }

    [Server]
    public void SetIdolForm(IdolForm idolForm)
    {
        this.idolForm = idolForm;
        idolForm.SetHealthRatio(idolHealthRatio);
        idolForm.SetDeathTimeout(idolDeathTimeout);
        idolForm.SetOwningPlayer(this);
    }

    [Server]
    public void SetWraithForm(PlayerWraith wraithForm)
    {
        this.wraithForm = wraithForm;
        if (wraithForm == null)
        {
            return;
        }
        
        this.wraithForm.OnDeathAnimationComplete += Die;

        this.wraithForm.SetHealthRatio(wraithHealthRatio);

        this.wraithForm.SetOwningPlayer(this);

        if (idolForm != null)
        {
            idolForm.SetHealthRatio(idolHealthRatio);
            idolForm.SetDeathTimeout(idolDeathTimeout);
        }

        // if (spectating)
        // {
        //     StopSpectating();
        // }
    }
    [Server]
    public void TransformIntoIdol()
    {
        if (Time.time - lastTransform < TRANSFORM_TIMEOUT)
        {
            return;
        }

        if (idolItem == null)
        {
            // No Idol held right now
            return;
        }

        if (idolForm == null)
        {
            SetIdolForm(IdolForm.CreateIdolForm(this, idolItem.GetAssociatedForm(), controlledEntity.transform.position, controlledEntity.transform.rotation));
            
            idolForm.TargetSetPositionAndRotation(controlledEntity.transform.position + new Vector3(0, 0.5F, 0), controlledEntity.transform.rotation);
            idolForm.RpcSetActive(true);
            
            idolForm.PlayTransformationExplosion(wraithForm.GetWorldPosCenter());
        }
        else if (idolForm.GetCurrentHealth() <= 0 || idolForm.GetDeathTimeout() > 0)
        {
            // Idol exists, and it's on cooldown
            return;
        }
        else
        {
            idolForm.TargetSetPositionAndRotation(controlledEntity.transform.position + new Vector3(0, 0.5F, 0), controlledEntity.transform.rotation);
            idolForm.RpcSetActive(true);
            idolForm.PlayTransformationExplosion(wraithForm.GetWorldPosCenter());
        }
        
        SetControlledEntity(idolForm);
        
        wraithForm.TransferEffects(idolForm);

        wraithForm.velocitySources = new();

        wraithForm.TargetExitAimMode();

        wraithForm.Transforming(idolForm);
        //wraithForm.ClearOwningPlayer();
        wraithForm.RpcSetActive(false);

        foreach (Trait trait in GetTraits().ToProcOrderList())
        {
            trait.OnTransform(idolForm, wraithForm);
        }

        lastTransform = Time.time;
    }

    [Server]
    public void TransformIntoWraith()
    {
        if (Time.time - lastTransform < TRANSFORM_TIMEOUT)
        {
            return;
        }

        wraithForm.TargetSetPositionAndRotation(controlledEntity.transform.position + new Vector3(0, 0.5F, 0), controlledEntity.transform.rotation);
        wraithForm.RpcSetActive(true);
        wraithForm.PlayTransformationExplosion(idolForm.GetWorldPosCenter());

        SetControlledEntity(wraithForm);

        if (idolForm != null)
        {
            idolForm.velocitySources = new();

            idolForm.Transforming(wraithForm);
            //idolForm.ClearOwningPlayer();
            idolForm.RpcSetActive(false);
        }

        idolForm.TransferEffects(wraithForm);

        foreach (Trait trait in GetTraits().ToProcOrderList())
        {
            trait.OnTransform(wraithForm, idolForm);
        }

        lastTransform = Time.time;
    }

    [Server]
    public void SetControlledEntity(ControlledEntity entity)
    {
        controlledEntity = entity;

        players[this] = controlledEntity;

        if (entity != null)
        {
            controlledEntity.SetOwningPlayer(this);
            controlledEntity.ApplyTraits();
        }
    }
    
    /// <summary>
    /// Update to controlled entity SyncVar
    /// </summary>
    void OnControlledEntityChanged(ControlledEntity oldEntity, ControlledEntity newEntity)
    {
        if (oldEntity != null)
        {
            if (isOwned)
            {
                oldEntity.OnDamaged -= AetherdalePostProcessing.DamagePulse;
            }
        }

        if (isOwned && newEntity != null)
        {
            // GUI/camera may not be initialized yet
            if (playerCamera != null)
            {
                StartCoroutine(SetCameraContextNextUpdate(newEntity.GetCameraContext()));
                //playerCamera.SetContext(newEntity.GetCameraContext());

                if (oldEntity != null)
                {
                    newEntity.GetCameraContext().SetRotation(oldEntity.GetCameraContext().GetRotation());
                }

            }

            controlledEntity.SetEntityReady();
            controlledEntity.OnDamaged += AetherdalePostProcessing.DamagePulse;
        }

        OnEntityChangedOnClient?.Invoke(newEntity);
    }

    //
    IEnumerator SetCameraContextNextUpdate(CameraContext context)
    {
        yield return new WaitForFixedUpdate();

        playerCamera.SetContext(context);
    }


    [Client]
    public void SetIdol(string idolId)
    {
        Debug.Log("Set Idol " + idolId);
        idolItem = new((IdolItemData) ItemManager.LookupItemData(idolId));

        playerData.SetEquippedIdol(idolItem);

        OnIdolEquippedOnClient?.Invoke(idolItem);

        CmdSetIdol(IdolItem.Serialize(idolItem));
    }

    [Command]
    public void CmdSetIdol(string serializedIdol)
    {
        idolItem = IdolItem.Deserialize(serializedIdol);

        Debug.Log("Set idol to " + idolItem);
        if (idolForm != null && idolForm.Data.GetItemID() != idolItem.GetItemID())
        {
            if (controlledEntity == idolForm)
            {
                TransformIntoWraith();
            }

            StartCoroutine(TeardownOldIdol(idolForm));
        }
    }

    IEnumerator TeardownOldIdol(IdolForm oldIdolForm)
    {
        oldIdolForm.RpcSetActive(false);
        yield return new WaitForSeconds(0.25F);
        NetworkServer.UnSpawn(oldIdolForm.gameObject);
        Destroy(oldIdolForm.gameObject);
    }

    public float GetIdolHealthRatio()
    {
        return idolHealthRatio;
    }


    public ControlledEntity GetControlledEntity()
    {
        return controlledEntity;
    }

    public PlayerWraith GetWraithForm()
    {
        return wraithForm;
    }

    public IdolForm GetIdolForm()
    {
        return idolForm;
    }


    void OnPlayerCameraChanged(PlayerCamera oldCam, PlayerCamera newCam)
    {
        if (controlledEntity != null && playerCamera != null)
        {
            // Server got our entity to us before first frame, set GUI data now
            playerCamera.SetContext(controlledEntity.GetCameraContext());
        }
    }

    void WraithFormChanged(PlayerWraith oldWraith, PlayerWraith newWraith)
    {
        OnNewWraithFormOnClient?.Invoke(newWraith);
    }

    void IdolFormChanged(IdolForm oldIdol, IdolForm newIdol)
    {
        OnNewIdolFormOnClient?.Invoke(newIdol);
    }

    #endregion




    #region Weapons

    [TargetRpc]
    public void TargetSetWraithWeapon(string weaponID, bool changePlayerData)
    {
        SetWraithWeapon((WeaponData) ItemManager.LookupItemData(weaponID), changePlayerData);
    }

    [Client]
    public void SetWraithWeapon(WeaponData weaponData, bool changePlayerData)
    {
        wraithForm.EquipWeapon(weaponData);

        if (changePlayerData)
        {
            playerData.SetEquippedWeapon(weaponData);
        }
    }

    [Server]
    public void SetSequenceWeapon(WeaponData weapon)
    {
        if (weapon == null)
        {
            currentSequenceWeaponID = "";

            if (wraithForm != null)
            {
                TargetResetWraithWeapon();
            }
        }
        else
        {
            currentSequenceWeaponID = weapon.GetItemID();

            if (wraithForm != null)
            {
                TargetSetWraithWeapon(currentSequenceWeaponID, false);
            }
        }

    }

    public WeaponData GetSequenceWeapon()
    {
        if (currentSequenceWeaponID == "")
        {
            return null;
        }

        return (WeaponData) ItemManager.LookupItemData(currentSequenceWeaponID);
    }

    [TargetRpc]
    public void TargetResetWraithWeapon()
    {
        if (wraithForm != null)
        {
            ResetWraithWeapon();
        }
    }

    [Client]
    public void ResetWraithWeapon()
    {
        if (wraithForm != null)
        {
            wraithForm.EquipWeapon(playerData.EquippedWeapon);
        }
    }

    #endregion




    #region Trinkets
    [Client]
    public void SetTrinket(Trinket newTrinket)
    {
        trinket = newTrinket;
        trinket.SetOwningPlayer(this);

        playerData.SetEquippedTrinket(trinket);

        OnTrinketEquippedOnClient?.Invoke(trinket);

        CmdSetTrinket(Trinket.Serialize(trinket));
    }

    [Command]
    public void CmdSetTrinket(string serializedTrinket)
    {
        trinket = Trinket.Deserialize(serializedTrinket);
        trinket.SetOwningPlayer(this);
    }

    #endregion



    [Client]
    public void ClientStartSequence(AreaSequencer.SequenceMode sequenceMode)
    {
        CmdStartSequence(sequenceMode);
    }

    [Command]
    void CmdStartSequence(AreaSequencer.SequenceMode sequenceMode)
    {
        AreaSequencer.GetAreaSequencer().StartAreaSequence(sequenceMode);
    }


    public void SetUI(PlayerUI newUI)
    {
        playerUI = newUI;
    }

    public PlayerUI GetUI()
    {
        return playerUI;
    }

    public PlayerCamera GetCamera()
    {
        return playerCamera;
    }

    public Inventory GetInventory()
    {
        return gameObject.GetComponent<Inventory>();
    }

    public Trinket GetTrinket()
    {
        return trinket;
    }
    
    [Server]
    public void KilledAnEntity(HitInfo hitResult)
    {
        OnEntityKilled?.Invoke(hitResult);

        if (hitResult.entityHit is Boss entityAsBoss)
        {
            OnBossDefeated?.Invoke(entityAsBoss);
        }
    }

    [Server]
    public void Die()
    {
        //Restore();
        isDead = true;
        PlayerDied(this);
    }

    [Server]
    public static void PlayerDied(Player deadPlayer)
    {
        // Check if all players are dead, invoke OnAllPlayersDied if so
        foreach (Player player in GetPlayers())
        {
            Debug.Log($"Checking {player}...");
            if (!player.isDead || (player.GetControlledEntity() != null && player.GetControlledEntity().resurrecting))
            {
                Debug.Log("A player remains");
                deadPlayer.StartSpectating(player);
                return;
            }
        }

        // No living players
        OnAllPlayersDied?.Invoke();
    }

    #region Spectating
    Entity spectatedEntity;

    [Server]
    public void StartSpectating(Player playerToSpectate)
    {
        Debug.Log(this + " START SPECTATING " + playerToSpectate);
        if (this == playerToSpectate)
        {
            return;
        }

        spectating = true;

        TargetStartSpectate(playerToSpectate);

        // SPECTATE TODO:
        // TODO way to rotate camera
    }

    [TargetRpc]
    void TargetStartSpectate(Player playerToSpectate)
    {
        playerUI.SetTrackedPlayer(playerToSpectate);

        spectatedEntity = playerToSpectate.GetControlledEntity();
        playerCamera.SetContext(spectatedEntity.GetCameraContext());
        spectatedEntity.OnTransformed += SpectatedEntityTransformed;
    }



    [Server]
    public void StopSpectating()
    {
        if (!spectating)
        {
            return;
        }
        
        spectating = false;

        TargetStopSpectate();
    }

    [TargetRpc]         
    public void TargetStopSpectate()
    {
        playerUI.SetTrackedPlayer(this);

        playerCamera.SetContext(GetControlledEntity().GetCameraContext());
        spectatedEntity.OnTransformed -= SpectatedEntityTransformed;
        spectatedEntity = null;
    }

    
    void SpectatedEntityTransformed(Entity oldEntity, Entity newEntity)
    {
        if (spectating)
        {
            spectatedEntity = newEntity;

            oldEntity.OnTransformed -= SpectatedEntityTransformed;

            playerCamera.SetContext(newEntity.GetCameraContext());

            newEntity.OnTransformed += SpectatedEntityTransformed;
        }
    }
    #endregion


    #region Quests
    public void GiveQuest(Quest quest)
    {
        if (playerData.HasQuest(quest))
        {
            Debug.LogWarning("Tried to receive quest " + quest + " we already have");
            return;
        }

        playerData.AddQuest(quest);

        Debug.Log("Started quest " + quest.GetName());

        OnLocalPlayerQuestReceived?.Invoke(quest);
    }


    [Server]
    public void BeatAnArea(Area area)
    {
        TargetBeatAnArea(area.GetAreaID());
    }

    [TargetRpc]
    void TargetBeatAnArea(string areaID)
    {
        ClientOnAreaCompleted?.Invoke(areaID);
    }

    [TargetRpc]
    public void TargetStoryEvent(string eventID)
    {
        StoryEvent(eventID);
    }

    [Client]
    public void StoryEvent(string eventID)
    {
        Debug.Log("Player sees story event " + eventID + " ( " + OnStoryEvent + ")");
        OnStoryEvent?.Invoke(eventID);
    }

    #endregion

    [Client]
    public void GiveDialogueResponse(DialogueAgent dialogueTarget, int chosenIndex)
    {
        if (dialogueTarget != null)
        {
            dialogueTarget.GiveDialogueResponse(this, chosenIndex);
        }
    }

    [Command]
    public void CmdCloseDialogue(DialogueAgent dialogueTarget)
    {
        if (dialogueTarget != null)
        {
            dialogueTarget.CloseDialogue(this);
        }
    }

    [Client]
    public void Chat(string message)
    {
        if (message[0] == '/')
        {
            DebugCommands.RunCommand(message.Replace("/", ""));
        }
        else
        {
            CmdSendChat(message);
        }
    }

    [Command]
    public void CmdSendChat(string message)
    {

        if (message.Length == 0)
        {
            return;
        }

        SendPlayerChatMessage(this, message);
    }

    [Server]
    public static void SendPlayerChatMessage(Player player, string message)
    {
        OnChatMessage?.Invoke(ChatMessageType.PlayerMessage, player.GetUsername() + " says: " + message);
    }

    [Server]
    public static void SendEnvironmentChatMessage(string message)
    {
        OnChatMessage?.Invoke(ChatMessageType.EnvironmentMessage, message);
    }

    public static void SendNpcMessage(NonPlayerCharacter npc, string message)
    {
        OnChatMessage?.Invoke(ChatMessageType.NpcMessage, npc.GetName() + " says: " + message);
    }

    [Server]
    void ReceiveChat(ChatMessageType chatMessageType, string message)
    {
        TargetReceiveChat(chatMessageType, message);
    }

    [TargetRpc]
    void TargetReceiveChat(ChatMessageType chatMessageType, string message)
    {
        SendLocalChatMessage(chatMessageType, message);
    }

    void SendLocalChatMessage(ChatMessageType chatMessageType, string message)
    {
        if (isLocalPlayer && playerUI != null)
        {
            playerUI.GetChatMenu().Receive(chatMessageType, message);
        }
    }


    public static void SendConnectedMessage(Player player)
    {
        OnChatMessage?.Invoke(ChatMessageType.SystemMessage, "[" + player.GetPlayerId() + " has connected]");
    }

    public static void SendDisconnectedMessage(Player player)
    {
        OnChatMessage?.Invoke(ChatMessageType.SystemMessage, "[" + player.GetUsername() + " has disconnected]");
    }


    [Server]
    public void PickupItem(Item item)
    {
        if (item.GetName() == "Aether")
        {
            TargetAddAether(item.GetQuantity());
        }
        else if (item.GetName() == "Gold")
        {
            GetInventory().AddGold(item.GetQuantity());
        }
        else if (item.itemData.IsQuestItem())
        {
            TargetAddAccountItem(item.GetItemID(), 1);
        }

        OnAcquireItem?.Invoke(item);
    }


    [TargetRpc]
    public void TargetAddAether(int quantity)
    {
        playerData.AddAether(quantity);
    }


    [TargetRpc]
    public void TargetAddAccountItem(string itemId, int quantity)
    {
        playerData.AddAccountItem(itemId, quantity);
    }

    /// <summary>
    /// Send dialogue text to this player. Will update the dialogue menu with this text
    /// responseIndices and responseText MUST be the same length, or this will do nothing.
    /// </summary>
    /// <param name="npc">Who is talking to the player</param>
    /// <param name="what">What they're saying</param>
    /// <param name="responseIndices">Indices for each response, to send back to the NPC when that response is chosen</param>
    /// <param name="responseText">What the player is saying for each response</param>
    [Client]
    public void ShowDialogueText(DialogueAgent npc, List<string> spokenText, List<int> responseIndices, List<string> responseText)
    {
        if (responseIndices.Count != responseText.Count)
        {
            Debug.LogError("ERROR: dialogue response index and text counts from server did not match!");
            return;
        }

        Dictionary<int, string> responseData = new();
        for (int i = 0; i < responseIndices.Count; i++)
        {
            responseData.Add(responseIndices[i], responseText[i]);
        }

        if (isOwned)
        {
            playerUI.ShowDialogueTopic(npc, spokenText, responseData);
        }
    }

    /// <summary>
    /// Tell this player that dialogue has ended, and it should close the dialogue menu
    /// </summary>
    [Client]
    public void EndDialogue()
    {
        playerUI.CloseDialogueMenu();
    }

}
