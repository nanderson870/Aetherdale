using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

using System;
using UnityEngine.VFX;


// Used as a game manager and a bridge between decoupled parts of the code


public class WorldManager : NetworkBehaviour
{
    [SerializeField] Faction defaultFaction;

    public readonly List<Entity> entities = new();

    static WorldManager Singleton;

    Transform itemPool;

    [SyncVar(hook = nameof(OnTimescaleChanged))] float timeScale = 1.0F;

    public static Action OnGamePaused;
    public static Action OnGameResumed;


    public static WorldManager GetWorldManager()
    {
        GameObject worldManagerGameObject = GameObject.Find("WorldManager");

        if (worldManagerGameObject != null)
            return worldManagerGameObject.GetComponent<WorldManager>();

        return null;
    }

    void Awake()
    {
        Debug.Log("WORLD MANAGER AWAKE");
        if (NetworkServer.active)
        {
            // Delete if duplicate
            if (Singleton != null)
            {
                Debug.Log("DESTROYING NEW WORLD MANAGER");
                Destroy(gameObject);
                return;
            }
            Singleton = this;
        }

    }

    void Start()
    {
        if (NetworkServer.active)
        {
            SceneManager.activeSceneChanged += OnSceneChanged;

            InvokeRepeating(nameof(PeriodicCleanup), 10, CLEANUP_INTERVAL);
        }
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        Debug.Log("WORLD MANAGER DESTROYED");
        SceneManager.activeSceneChanged -= OnSceneChanged;
        CancelInvoke();
    }

    const float CLEANUP_INTERVAL = 3.0F;
    void PeriodicCleanup()
    {
        if (isServer)
        {
            foreach (Pickup pickup in FindObjectsByType<Pickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (pickup.gameObject.activeSelf && pickup.GetTimeout() != 0 && ((Time.time - pickup.GetSpawnTime()) > pickup.GetTimeout()))
                {
                    pickup.Teardown();
                }
            }
        }
    }

    [Server]
    public void SetTimescale(float timescale)
    {
        Time.timeScale = timescale;
        timeScale = timescale;
    }

    void OnTimescaleChanged(float oldTimescale, float newTimescale)
    {
        Time.timeScale = newTimescale;

        if (Mathf.Approximately(newTimescale, 0))
        {
            OnGamePaused?.Invoke();
        }
        else if (Mathf.Approximately(newTimescale, 1))
        {
            OnGameResumed?.Invoke();
        }
    }

    public void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        if (isServer)
        {
            entities.Clear();
        }
    }


    [Server]
    public Entity GetNearestEntity(Vector3 worldPosition, float maxRange, Entity excludeEntity = null, Entity excludeAlliesOf = null)
    {
        float closestDist = Mathf.Infinity;
        Entity closest = null;

        foreach (Entity entity in entities)
        {

            if (entity != null && !entity.IsDead() && entity != excludeEntity && (excludeAlliesOf == null || !entity.IsAlly(excludeAlliesOf)))
            {
                float distance = Vector3.Distance(worldPosition, entity.transform.position);
                if (distance <= closestDist)
                {
                    closest = entity;
                    closestDist = distance;
                }
            }
        }

        return closest;
    }

    [ServerCallback]
    public static void RegisterEntity(Entity entity)
    {
        GetWorldManager().InstancedRegisterEntity(entity);
    }

    [ServerCallback]
    public static void UnregisterEntity(Entity entity)
    {
        GetWorldManager().InstancedUnregisterEntity(entity);
    }

    [Server]
    void InstancedRegisterEntity(Entity entity)
    {
        if (!entities.Contains(entity))
        {
            entities.Add(entity);
        }
    }

    [Server]
    void InstancedUnregisterEntity(Entity entity)
    {
        if (entities.Contains(entity))
        {
            entities.Remove(entity);
        }
    }


    [Server]
    public static Entity GetNearestEnemyOfFaction(Faction faction, Vector3 position, float maxRange = Mathf.Infinity, List<Entity> excluded = null)
    {
        if (faction == null)
        {
            return null;
        }

        List<Entity> inRangeEnemies = new();
        foreach (Entity entity in GetWorldManager().entities)
        {
            if (entity != null
                && entity.gameObject.activeSelf
                && entity is not NonPlayerCharacter
                && faction.IsEnemiesWithFaction(entity.GetFaction())
                && (Vector3.Distance(position, entity.transform.position) <= maxRange)
                && !entity.IsDead()
                && (excluded == null || !excluded.Contains(entity)))
            {
                inRangeEnemies.Add(entity);
            }
        }

        if (inRangeEnemies.Count == 0)
        {
            return null;
        }

        Entity nearestEnemy = inRangeEnemies[0];
        float nearestEnemyDistance = Vector3.Distance(position, inRangeEnemies[0].transform.position);
        foreach (Entity entity in inRangeEnemies)
        {
            float distanceToEnemy = Vector3.Distance(position, entity.transform.position);
            if (distanceToEnemy < nearestEnemyDistance)
            {
                nearestEnemy = entity;
                nearestEnemyDistance = distanceToEnemy;
            }
        }

        return nearestEnemy;
    }

    [Server]
    public ControlledEntity GetNearestControlledEntity(Vector3 from, float maxRange = 0.0F)
    {
        if (Player.GetPlayerEntities().Count == 0)
        {
            return null;
        }

        ControlledEntity nearest = null;
        float nearestDistance = Mathf.Infinity;
        foreach (ControlledEntity entity in Player.GetPlayerEntities())
        {
            float distanceToEntity = Vector3.Distance(from, entity.transform.position);
            if (distanceToEntity < maxRange && distanceToEntity < nearestDistance && entity.gameObject.activeSelf)
            {
                nearest = entity;
                nearestDistance = distanceToEntity;
            }
        }

        return nearest;

    }

    [Server]
    public Faction GetDefaultFaction()
    {
        return defaultFaction;
    }

    // TODO Refactor into NonPlayerCharacter
    [Server]
    public NonPlayerCharacter GetNonPlayerCharacterInstance(NonPlayerCharacterName name)
    {
        NonPlayerCharacter[] npcs = FindObjectsByType<NonPlayerCharacter>(FindObjectsSortMode.None);

        foreach (NonPlayerCharacter npc in npcs)
        {
            if (npc.GetNpcName() == name)
            {
                return npc;
            }
        }

        return null;
    }

}