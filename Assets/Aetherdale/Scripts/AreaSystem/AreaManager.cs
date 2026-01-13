using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Aetherdale;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

public class AreaManager : NetworkBehaviour
{
    public static AreaManager CurrentAreaManager {get; private set;}

    public static int regionsCompleted = 0;
    public static int areasCompleted = 0;

    [SerializeField] Area area;

    [Header("Spawning")]
    readonly float spawnInterval = 2.0F;
    public int maxEnemies = 25;
    float eliteChance = 1.0F;
    readonly float spawnStartDelay = 6.0F;
    readonly LinearEquation maxSpawnsPerIntervalPerLevel = new(0.5F, 4);
    [SerializeField] float lowerBound = -200;
    [SerializeField] float upperBound = 400;

    readonly PolynomialEquation MAX_ENEMIES_PER_PLAYER_PER_MINUTE_IN_AREA = new(0.03F, 2.5F, 2F, 10F);
    readonly LinearEquation DENSITY_MULT_PER_LEVEL = new LinearEquation(0.1F, 1.0F);


    [Header("Bosses")]
    [SerializeField] Transform[] bossSpawnPositions;

    [Header("NPC Encounters")]
    [SerializeField] Transform[] npcEncounterLocations;
    [SerializeField] NonPlayerCharacter[] encounterNpcs;

    [Header("Chests")]
    [SerializeField] int minNumberOfChests;
    [SerializeField] int maxNumberOfChests;

    [Header("Derbo Tables")]
    [SerializeField] int minDerboTables = 3;
    [SerializeField] int maxDerboTables = 6;


    [Header("Ancient Tablets")]
    [SerializeField] int minChallengeTablets = 0;
    [SerializeField] int maxChallengeTablets = 2;
    [SerializeField] int percentChallengeTabletChance = 25;

    [Header("Cursed Shrines")]
    [SerializeField] int minCursedShrines = 0;
    [SerializeField] int maxCursedShrines = 1;

    int enemyLevel = 1;

    List<AreaPortal> areaPortals = new();

    SpawnMesh spawnMesh = null;
    NavMeshTriangulation navMeshTriangulation;

    float startTime;

    public static Action<AreaManager> OnAreaReady;

    void Awake()
    {
        CurrentAreaManager = this;
    }
    
    public void Start()
    {
        if (isServer)
        {
            navMeshTriangulation = NavMesh.CalculateTriangulation();

            startTime = Time.time;

            StartCoroutine(nameof(SetupArea));

            InvokeRepeating(nameof(CheckBounds), 5, 1.0F);

            spawnMesh = FindAnyObjectByType<SpawnMesh>();

            AddSpawnList(area.region.spawnList);
        }

    }

    IEnumerator SetupArea()
    {
        // Debug.Log("START SETTING UP SERVER");
        yield return null;

        while (!Player.AllPlayersReady())
        {
            // Debug.Log("Area Manager waiting for all players to be ready...");
            yield return null;
        }

        areaPortals = FindObjectsByType<AreaPortal>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();
        // Debug.Log("Setup area - " + areaPortals.Count() + " portals found");


        // Use invoke for these because we don't want exceptions breaking the control flow here
        if (!area.IsSafeArea())
        {
            Invoke(nameof(SetupChests), 0);

            Invoke(nameof(SetupDerboTables), 0);

            Invoke(nameof(SetupChallengeTablets), 0);

            Invoke(nameof(SetupCursedShrines), 0);

            Invoke(nameof(SetupSpawnZones), spawnStartDelay);

            InvokeRepeating(nameof(ProcessSpawns), 0, spawnInterval);
        }

        // Debug.Log("Area Manager finished setup");

        OnAreaReady?.Invoke(this);
    }
    
    [Server]
    void SetupChests()
    {
        if (area.IsSafeArea())
        {
            return;
        }
        
        int numberTables = UnityEngine.Random.Range(minNumberOfChests, maxNumberOfChests + 1);

        GameObject chestGroup = new GameObject();
        chestGroup.AddComponent<DerboTableGroup>();

        for (int i = 0; i < numberTables; i++)
        {
            Vector3 spawnPosition = GetSpawnPosition();

            Chest chestPrefab = GetChestPrefab();

            Chest chest = Instantiate(chestPrefab, spawnPosition, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0));
            NetworkServer.Spawn(chest.gameObject);
        }
    }

    
    [Server]
    void CheckBounds()
    {
        foreach(ControlledEntity controlledEntity in FindObjectsByType<ControlledEntity>(FindObjectsSortMode.None))
        {
            float entityHeight = controlledEntity.transform.position.y;
            if (entityHeight <= lowerBound || entityHeight >= upperBound)
            {
                controlledEntity.TargetSetPosition(AetherdaleNetworkManager.singleton.GetStartPosition().position);
            }
        }
    }

    [Server]
    void SetupDerboTables()
    {
        int numberTables = UnityEngine.Random.Range(minDerboTables, maxDerboTables + 1);
        Queue<IShopOffering> tableOfferings = new();
        for (int i = 0; i < numberTables; i++)
        {
            IShopOffering offering = ShopOffering.GetRandomLevelledShopOffering(enemyLevel, weaponWeight:10, traitWeight:90, consumableWeight:10);
            tableOfferings.Enqueue(offering);
        }

        GameObject derboTableGroup = new GameObject();
        derboTableGroup.AddComponent<DerboTableGroup>();

        for (int i = 0; i < numberTables; i++)
        {
            Vector3 spawnPosition = GetSpawnPosition();

            DerboTable table = Instantiate(AetherdaleData.GetAetherdaleData().derboTablePrefab, spawnPosition, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0));
            NetworkServer.Spawn(table.gameObject);

            table.GetComponent<StatefullyActiveNetworkBehaviour>().OrderState(StatefullyActiveNetworkBehaviour.ActiveState.OrderedActive);

            table.SetOffering(tableOfferings.Dequeue());
        }
    }
    
    [Server]
    void SetupChallengeTablets()
    {
        ChallengeTablet[] existingTablets = FindObjectsByType<ChallengeTablet>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        int tabletChances = maxChallengeTablets - existingTablets.Length;
        if (tabletChances <= 0)
        {
            // Don't add tablets if max is already added to level
            return;
        }

        int tabletsSpawned = existingTablets.Length;
        for (int i = 0; i < tabletChances; i++)
        {
            if (tabletsSpawned < minChallengeTablets || UnityEngine.Random.Range(0, 100) < percentChallengeTabletChance)
            {
                Vector3 spawnPosition = GetSpawnPosition();

                ChallengeTablet tablet = Instantiate(AetherdaleData.GetAetherdaleData().challengeTabletPrefab, spawnPosition, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0));
                NetworkServer.Spawn(tablet.gameObject);

                tablet.GetComponent<StatefullyActiveNetworkBehaviour>().OrderState(StatefullyActiveNetworkBehaviour.ActiveState.OrderedActive);
            }
        }
    }

    [Server]
    void SetupCursedShrines()
    {
        int numberShrines = UnityEngine.Random.Range(minCursedShrines, maxCursedShrines + 1);

        for (int i = 0; i < numberShrines; i++)
        {
            Vector3 spawnPosition = GetSpawnPosition();

            CursedShrine shrine = Instantiate(AetherdaleData.GetAetherdaleData().cursedShrinePrefab, spawnPosition, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0));
            NetworkServer.Spawn(shrine.gameObject);

            shrine.GetComponent<StatefullyActiveNetworkBehaviour>().OrderState(StatefullyActiveNetworkBehaviour.ActiveState.OrderedActive);
        }
    }

    Vector3 GetSpawnPosition(bool flying = false)
    {
        Vector3 unsampledPosition;
        if (spawnMesh != null)
        {
            unsampledPosition = SpawnMesh.GetSpawnPoint();
        }
        else if (GetSpawnZones().Count() > 0)
        {
            List<Tuple<float, SpawnZone>> weightedZones = new();
            foreach (SpawnZone spawnZone in GetSpawnZones())
            {
                weightedZones.Add(new((float) spawnZone.GetVolume(), spawnZone));
            }

            SpawnZone zone = Misc.RouletteRandom(weightedZones);
            unsampledPosition = zone.GetSpawnPosition();
        }
        else
        {
            throw new Exception("No Spawn Zones Found");
        }

        
        NavMeshQueryFilter filter = new()
        {
            areaMask = NavMesh.AllAreas,
            agentTypeID = flying ? Misc.GetNavMeshAgentID("Flying") : Misc.GetNavMeshAgentID("Humanoid")
        };
        
        if (NavMesh.SamplePosition(unsampledPosition, out NavMeshHit hit, 100.0F, filter))
        {
            return hit.position;
        }
        else
        {
            throw new System.Exception("Could not sample position " + unsampledPosition + " on navmesh");
        }
    }
    
    List<SpawnZone> GetSpawnZones()
    {
        return FindObjectsByType<SpawnZone>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();
    }

    [Server]
    void SetupSpawnZones()
    {
        foreach (SpawnZone spawnZone in GetSpawnZones())
        {
            spawnZone.SetAreaManager(this);
            spawnZone.SetFaction(area.region.defaultFaction);
            spawnZone.SetSpawnDensity(12 + AreaSequencer.GetAreaSequencer().GetAreaLevel());
            spawnZone.SetLevel(AreaSequencer.GetAreaSequencer().GetAreaLevel());

            spawnZone.StartSpawning();
        }
    }

    [Server]
    void ProcessSpawns()
    {
        this.eliteChance = enemyLevel + 10; // Maybe adjust this - right now elite chance is just the level as a percent

        if (spawnLists.Count == 0)
        {
            throw new Exception("NO SPAWN LISTS");
        }

        int numSpawns = UnityEngine.Random.Range(1, (int) maxSpawnsPerIntervalPerLevel.Calculate(enemyLevel) + 1);
        for (int i = 0; i < numSpawns; i++)
        {
            if (!AtMaxEnemies())
            {
                Entity newEntityPrefab = SpawnList.GetEntityFromSpawnLists(spawnLists);
                SpawnEnemy(newEntityPrefab);
            }    
        }    
    }

    public void SpawnEnemy(Entity enemy)
    {
        bool flying = enemy.TryGetComponent(out FlyingStatefulCombatEntity fsce);
        Vector3 spawnPosition = GetSpawnPosition(flying);

        Entity spawnedEntity = Instantiate(enemy, spawnPosition, Quaternion.identity);
        spawnedEntity.transform.SetParent(null);

        NetworkServer.Spawn(spawnedEntity.gameObject);

        if (UnityEngine.Random.Range(0.0F, 100.0F) < eliteChance)
        {
            Element element = (Element) UnityEngine.Random.Range((int) Element.Fire, (int) Element.Dark + 1);

            Elite.CreateElite(spawnedEntity, element);
        }

        // Adjust entity 
        spawnedEntity.SetLevel(enemyLevel);
        spawnedEntity.SetFaction(AetherdaleData.GetAetherdaleData().defaultEnemyFaction);
    }

    
    public List<Tuple<SpawnList, SpawnList.SpawnListLevelMechanism>> spawnLists = new();



    public int GetNumberOfEnemiesSpawned()
    {
        int ret = 0;
        foreach (Entity entity in WorldManager.GetWorldManager().entities)
        {
            if (entity.IsCountedBySpawners())
            {
                ret++;
            }
        }

        return ret;
    }


    public int GetMaxEnemies()
    {
        return (int)(MAX_ENEMIES_PER_PLAYER_PER_MINUTE_IN_AREA.Calculate(GetSecondsInArea() / 60.0F) * Player.GetPlayers().Count() * DENSITY_MULT_PER_LEVEL.Calculate((float) AreaSequencer.GetAreaSequencer().GetEnemyLevel()));
    }

    public bool AtMaxEnemies()
    {
        int numSpawned = GetNumberOfEnemiesSpawned();
        int max = GetMaxEnemies();

        //Debug.Log($"At {numSpawned}/{max} enemies - {GetSecondsInArea() / 60.0F} minutes");
        return numSpawned >= max;
    }

    float GetSecondsInArea()
    {
        return Time.time - startTime;
    }

    public List<AreaPortal> GetNextAreaPortals()
    {
        return areaPortals.ToList();
    }

    public void SetupPortals(List<Area> nextAreas)
    {
        // Validate
        int numberOfAreas = nextAreas.Count();
        List<AreaPortal> potentialNextPortals = GetNextAreaPortals();
        if (numberOfAreas > potentialNextPortals.Count())
        {
            throw new Exception($"This area does not have enough AreaPortals to support the amount it's trying to set up ({numberOfAreas} {potentialNextPortals.Count()}");
        }

        // Determine which portals to set up
        for (int i = 0; i < numberOfAreas; i++)
        {
            Area nextArea = nextAreas[i];
            AreaPortal selectedPortal = potentialNextPortals[UnityEngine.Random.Range(0, potentialNextPortals.Count())];

            StatefullyActiveNetworkBehaviour sanb = selectedPortal.GetComponent<StatefullyActiveNetworkBehaviour>();
            sanb.OrderState(StatefullyActiveNetworkBehaviour.ActiveState.OrderedActive);

            NetworkServer.Spawn(selectedPortal.gameObject);

            selectedPortal.SetArea(nextArea);

            // Prevent duplicates
            potentialNextPortals.Remove(selectedPortal);
        }
    }

    public AreaPortal GetRandomChargingPortal()
    {
        List<AreaPortal> potentialPortals = GetNextAreaPortals().ToList();
        for (int i = potentialPortals.Count() - 1; i >= 0; i--)
        {
            if (!potentialPortals[i].rebuilding)
            {
                potentialPortals.RemoveAt(i);
            }
        }

        if (potentialPortals.Count() == 0)
        {
            return null;
        }

        return potentialPortals[UnityEngine.Random.Range(0, potentialPortals.Count())];
    }


    [Server]
    public void StopSpawning()
    {
        CancelInvoke();
    }

    [Server]
    public Boss SpawnBoss(Vector3 position = new())
    {
        Boss bossPrefab = area.region.GetBoss(enemyLevel);

        if (position == Vector3.zero)
        {
            position = GetSpawnPosition();//bossSpawnPositions[UnityEngine.Random.Range(0, bossSpawnPositions.Length)].position;
        }

        Boss bossInstance = Instantiate(bossPrefab, position, Quaternion.identity);
        bossInstance.transform.SetParent(null);
        NetworkServer.Spawn(bossInstance.gameObject);

        bossInstance.SetLevel(enemyLevel);
        bossInstance.SetPlayerNumberScaling(Player.GetPlayers().Count);
        bossInstance.SetFaction(area.region.defaultFaction);

        return bossInstance;
    }
    

    public void SetEnemyLevel(int level)
    {
        this.enemyLevel = level;
        
        foreach (SpawnZone spawnZone in GetSpawnZones())
        {
            spawnZone.SetLevel(level);
        }
    }

    public int GetEnemyLevel()
    {
        return enemyLevel;
    }

    public void AddSpawnList(SpawnList spawnList, SpawnList.SpawnListLevelMechanism mechanism = null)
    {
        Debug.Log("ADD SPAWN LIST " + spawnList);
        if (!spawnLists.Any(sl => sl.Item1 == spawnList))
        {
            spawnLists.Add(new (spawnList, mechanism));
            return;
        }

        return;
    }

    public Area GetArea()
    {
        return area;
    }

    
    
    #region Chest Spawns
    private struct ChestSpawnChances
    {
        public int rusty;
        public int silver;
        public int gold;
        public int diamond;

        public ChestSpawnChances(int r, int s, int g, int d)
        {
            rusty = r;
            silver = s;
            gold = g;
            diamond = d;
        }
    }

    static ChestSpawnChances chestSpawnChances1 = new(100, 0, 0, 0);
    static ChestSpawnChances chestSpawnChances2 = new(50, 50, 0, 0);

    static ChestSpawnChances GetChestSpawnChances()
    {
        if (regionsCompleted < 1)
        {
            return chestSpawnChances1;
        }
        else
        {
            return chestSpawnChances2;
        }
    }

    static Chest GetChestPrefab()
    {
        ChestSpawnChances spawnChances = GetChestSpawnChances();

        return Misc.RouletteRandom(new List<Tuple<float, Chest>> ()
        {
            new (spawnChances.rusty, AetherdaleData.GetAetherdaleData().rustyChestPrefab),
            new (spawnChances.silver, AetherdaleData.GetAetherdaleData().silverChestPrefab),
            new (spawnChances.gold, AetherdaleData.GetAetherdaleData().goldChestPrefab),
            new (spawnChances.diamond, AetherdaleData.GetAetherdaleData().diamondChestPrefab),
        });
    }
    #endregion
}
