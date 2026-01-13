using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FMODUnity;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AreaSequencer : NetworkBehaviour
{
    public enum SequenceMode
    {
        Normal,
        Gauntlet
    }

    [SerializeField] string hubSceneName="Online Scene";
    [SerializeField] Region forestRegion;
    [SerializeField] Region iceCavesRegion;
    [SerializeField] Region magmaForgeRegion;


    // Checkpoints
    [SerializeField] Area postForestCheckpoint;
    [SerializeField] Area postIceCavesCheckpoint;
    [SerializeField] Area postMagmaForgeCheckpoint;
    

    [SerializeField] TieredSpawnList dangerSpawnlist;

    [SerializeField] BossCutscene bossCutscenePrefab;

    static AreaSequencer Singleton;

    
    public static AreaSequencer GetAreaSequencer()
    {
        return Singleton;
    }

    const int NORMAL_LEVELS_PER_AREA = 2;
    const int GAUNTLET_LEVELS_PER_AREA = 3;

    const float NORMAL_LEVELS_PER_MINUTE = 0.5F;


    const float DANGER_PER_MINUTE = 0.3F;


    Queue<Area> areaQueue = new();


    SequenceMode currentMode;
    Area currentArea;
    Region currentRegion;
    Objective currentObjective;
    public string currentObjectiveDescription = "No objective";
    public Action<string> OnCurrentObjectiveUpdate; // Client

    public static Action<bool /*Victory?*/> OnRunEnded;


    readonly Dictionary<string /*id*/, int /*#*/> currentRewards = new();

    [SyncVar] bool sequenceRunning = false;
    float currentLevel = 1;

    float dangerLevel = 0;

    [SyncVar] float sequenceStartTime = 0;

    bool bossFoughtThisArea = false;

    AreaManager currentAreaManager;

    void Awake()
    {
        if (Singleton == null)
        {
            Singleton = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // Start is called before the first frame update
    public void Start()
    {
        AreaPortal.OnPortalRebuildStart += OnAreaPortalActivated;

        AreaManager.OnAreaReady += AreaManagerReady;

        DontDestroyOnLoad(this);

        SceneManager.activeSceneChanged += OnSceneChanged;
    }


    void OnSceneChanged(Scene old, Scene newScene)
    {
        OnCurrentObjectiveUpdate = null;
    }

    void Update()
    {
        if (isServer)
        {
            if (currentObjective is TimedObjective timedObj)
            {
                timedObj.Evaluate();
            }

            if (IsSequenceRunning())
            {
                // Enemy level
                int previousLevel = GetEnemyLevel();

                currentLevel += NORMAL_LEVELS_PER_MINUTE * Time.deltaTime / 60.0F;

                if (GetEnemyLevel() > previousLevel)
                {
                    EnemyLevelChanged();
                }


                // Ithindar awareness
                int previousDangerLevel = (int) dangerLevel;
                dangerLevel += DANGER_PER_MINUTE * Time.deltaTime / 60.0F;
                if ((int) dangerLevel != previousDangerLevel)
                {
                    Debug.Log("DANGER LEVEL IS NOW " + dangerLevel);
                }
            }

        }
    }

    const int SHOP_PORTAL_CHANCE = 20;
    void AreaManagerReady(AreaManager manager)
    {
        currentAreaManager = manager;
        bossFoughtThisArea = false;
        
        if (IsSequenceRunning())
        {

            if (areaQueue.Count == 0)
            {
                PopulateAreaQueue();
            }

            ConfigureNextAreas(manager);

            // Adjust difficulty in new area
            if (manager != null)
            {
                manager.SetEnemyLevel(GetEnemyLevel());
            }

            // Add danger spawn list for more ramp-up
            manager.AddSpawnList(dangerSpawnlist, GetDangerLevel);

            currentObjective = null;
            if (currentArea.IsBossArea())
            {
                currentObjective = StartKillBossObjective();
            }
            else if (!currentArea.IsSafeArea())
            {
                //currentObjective = StartKillBossObjective();
                currentObjective = StartRepairPortalObjective();
                //currentAreaManager.GetNextAreaPortal().OnRebuildValueChanged += (float _) => {ObjectiveProgressed(currentObjective);};
            }

            if (currentObjective != null)
            {
                currentObjective.OnObjectiveProgress += ObjectiveUpdated;
                ObjectiveUpdated(currentObjective);
            }
            else
            {
                ObjectiveDisplay.ClearObjectiveGUI();
            }


            if (currentObjective != null)
            {
                currentObjective.OnObjectiveCompleted += AreaObjectiveCompleted;
            }
        }    
    }

    void ConfigureNextAreas(AreaManager manager)
    {
        // List of next areas to send to the area manager
        List<Area> nextAreas = new()
        {
            // Set next sequential area at a minimum
            areaQueue.Peek()
        };

        // TODO how do we implement shop areas better?
        // Roll for shop area
        // if (!currentArea.IsSafeArea())
        // {
        //     if (UnityEngine.Random.Range(0, 100) < SHOP_PORTAL_CHANCE)
        //     {
        //         nextAreas.Add(shopArea);
        //     }
        // }

        manager.SetupPortals(nextAreas);
    }


    /// <summary>
    /// Tell sequencer that player is ready in area
    /// </summary>
    /// <param name="player"></param>
    [Server]
    public void PlayerReady(Player player)
    {
        player.SetInSequence(true);

        if (AreaManager.areasCompleted == 0)
        {
            player.traitsOwed += 1; // give starting trait
            player.GetUI().TargetTraitSelectionAvailable(true);
        }
    }

    [Server]
    public void StartAreaSequence(SequenceMode sequenceMode)
    {
        foreach (Player player in Player.GetPlayers())
        {
            player.SetInSequence(true);
        }

        areaQueue.Clear();

        PopulateAreaQueue();

        currentArea = null;
        currentRegion = null;

        currentLevel = 1;
        AreaManager.areasCompleted = 0;
        AreaManager.regionsCompleted = 0;
        currentMode = sequenceMode;
        sequenceRunning = true;
        sequenceStartTime = Time.time;

        dangerLevel = 0;
        
        Player.OnAllPlayersDied += StopAreaSequence;
        
        LoadArea(areaQueue.Dequeue(), stepScaling:false);
    }

    void PrintAreaQueue(string message = "")
    {
        Debug.Log(message + ": AREA QUEUE IS:");
        foreach (Area area in areaQueue)
        {
            Debug.Log("  " + area.areaName);
        }
        Debug.Log("END");
    }

    public int GetSecondsInSequence()
    {
        return (int) (Time.time - sequenceStartTime);
    }

    void PopulateAreaQueue()
    {
        areaQueue.Clear();

        #if AETHERDALE_DEMO_BUILD
            AddRegionToQueue(forestRegion);
            return;
        #endif

        AddRegionToQueue(forestRegion);
        areaQueue.Enqueue(postForestCheckpoint);

        AddRegionToQueue(iceCavesRegion);
        
        AddRegionToQueue(magmaForgeRegion);
    }

    void AddRegionToQueue(Region region)
    {
        List<Area> areaList = region.areas.ToList();
        for (int i = 0; i < 3 && areaList.Count() != 0; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, areaList.Count());
            Area area = areaList[randomIndex];
            areaQueue.Enqueue(area);

            areaList.Remove(area);
        }

        //areaQueue.Enqueue(shopArea);
    }

    [Server]
    public void LoadArea(Area area, bool stepScaling = true)
    {
        currentArea = area;

        if (area.region != null && area.region != currentRegion)
        {
            if (currentRegion == null)
            {
                // Went from no region -> region, presumably run has just started
                
            }
            else
            {
                AreaManager.regionsCompleted++;
            }

            currentRegion = area.region;
        }

        if (areaQueue.TryPeek(out Area next) && area == next)
        {
            // TODO there must be a better way of managing options
            areaQueue.Dequeue();
        }

        TeardownCurrentObjective();

        if (stepScaling)
        {
            currentLevel = GetNextAreaLevel();
            AreaManager.areasCompleted++;
        }

        AetherdaleNetworkManager.singleton.ServerChangeScene(area.GetSceneName());
    }

    [Server]
    public void StopAreaSequence()
    {
        foreach (Player player in Player.GetPlayers())
        {
            player.SetInSequence(false);
            player.SetSequenceWeapon(null);
            player.Restore();
            player.ResetTraitRerolls();
        }

        bool victory = false;
        if (currentObjective != null && !currentObjective.IsObjectiveComplete())
        {
            Debug.Log("FAILURE");
            if (currentMode == SequenceMode.Gauntlet)
            {
                currentRewards.Clear(); // TODO mode-based partial reward handout
            }
        }
        else
        {
            victory = true;
            Debug.Log("SUCCESS");
        }

        StoryEvent.Send("RunEnded");

        Player.ResetInventory();

        TeardownCurrentObjective();

        OnRunEnded?.Invoke(victory);

        currentAreaManager = null;
        sequenceRunning = false;
        AetherdaleNetworkManager.singleton.ServerChangeScene(hubSceneName);

        AudioManager.SetMusicIntensity(0);
    }

    

    [Server]
    void AreaObjectiveCompleted(Objective objective)
    {
        //ObjectiveDisplay.UpdateObjectiveDescription(GetObjectiveLabelText(), "Continue through the portal");
        //if (!currentAreaManager.GetNextAreaPortal().portalActive)
        //{
        //    currentAreaManager.GetNextAreaPortal().SetPortalActive(true);
        //}

        foreach (Player player in Player.GetPlayers())
        {
            player.BeatAnArea(currentArea);
        }

    }

    void EnemyLevelChanged()
    {
        Debug.Log($"Enemies are now level {GetEnemyLevel()}");
        if (currentAreaManager != null)
        {
            currentAreaManager.SetEnemyLevel(GetEnemyLevel());
        }
    }

    string GetObjectiveLabelText()
    {
        return $"Stage {AreaManager.areasCompleted + 1} | Enemy Lvl {(int) currentLevel}";
    }

    void ObjectiveUpdated(Objective objective)
    {
        ObjectiveDisplay.UpdateObjectiveDescription(GetObjectiveLabelText(), objective.GetDescription());
    }


    public int GetAreaLevel()
    {
        return GetEnemyLevel();
    }

    public int GetNextAreaLevel()
    {
        return GetEnemyLevel() + (currentMode == SequenceMode.Gauntlet ? GAUNTLET_LEVELS_PER_AREA : NORMAL_LEVELS_PER_AREA);
    }

    RepairPortalObjective StartRepairPortalObjective()
    {
        RepairPortalObjective obj =  new();
        obj.OnObjectiveUpdated += ObjectiveUpdated;

        obj.StartTracking();
        return obj;
    }

    void OnAreaPortalActivated(AreaPortal areaPortal)
    {
        if (areaQueue.Peek().region != currentArea.region && !bossFoughtThisArea)
        {
            // Final area of region, spawn a boss
            Player.SendEnvironmentChatMessage("Activating the portal has summoned a guardian!");

            KillBossObjective obj = StartKillBossObjective(areaPortal.transform.position + areaPortal.transform.forward * 5.0F);
            areaPortal.AddAdditionalObjective(obj);
            bossFoughtThisArea = true;
        }
    }
    
    
    KillBossObjective StartKillBossObjective(Vector3 spawnPos = new())
    {
        Boss boss = currentAreaManager.SpawnBoss(spawnPos);
        boss.SetLevel(GetEnemyLevel());
        boss.SetAIEnabled(false);

        //BossCutscene cutscene = Instantiate(bossCutscenePrefab);
        //NetworkServer.Spawn(cutscene.gameObject);
        //cutscene.OnExited += () => boss.SetAIEnabled(true);

        KillBossObjective bossObj = new(boss.GetBossName());

        boss.OnBossDeath += bossObj.ProgressObjective;
        boss.SetAIEnabled(true);

        bossObj.StartTracking();

        return bossObj;
    }

    void TeardownCurrentObjective()
    {
        if (currentObjective == null)
        {
            return;
        }

        currentObjective = null;
        ObjectiveDisplay.ClearObjectiveGUI();
    }

    [Server]
    public void AddRewardItem(Item item)
    {
        if (!currentRewards.ContainsKey(item.GetItemID()))
        {
            currentRewards.Add(item.GetItemID(), item.GetQuantity());
        }
        else
        {
            currentRewards[item.GetItemID()] += item.GetQuantity();
        }
    }

    public bool IsSequenceRunning()
    {
        return sequenceRunning;
    }

    public int GetEnemyLevel()
    {
        return (int) currentLevel;
    }
    
    public int GetDangerLevel()
    {
        if (currentArea.region.minDangerLevel > dangerLevel)
        {
            return currentArea.region.minDangerLevel;
        }

        return (int) dangerLevel;
    }

    public ItemList GetCurrentRewards()
    {
        ItemList itemList = new();
        foreach (KeyValuePair<string, int> kvp in currentRewards)
        {
            itemList.Add(new Item(ItemManager.LookupItemData(kvp.Key), kvp.Value));
        }

        return itemList;
    }

    public Objective GetAreaObjective()
    {
        return currentObjective;
    }

    public SequenceMode GetCurrentMode()
    {
        return currentMode;
    }


}
