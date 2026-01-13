using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

public class SpawnZone : EntityTrackingZone
{
    [Tooltip("Default faction to spawn entities into. Leave empty to let area managers decide.")] 
    [SerializeField] Faction faction;

    float spawnDensity = 12; // entities / 100m
    float spawnInterval = 7.5F;

    
    int enemyLevel = 1;

    AreaManager areaManager;


    public static bool spawnsEnabled = true;


    public void SetAreaManager(AreaManager areaManager)
    {
        this.areaManager = areaManager;
    }

    public bool SetFaction(Faction faction)
    {
        if (this.faction != null)
        {
            return false;
        }

        this.faction = faction;
        return true;
    }


    /// <summary>
    /// Set density - entities/100m
    /// </summary>
    /// <returns></returns>
    public void SetSpawnDensity(float density)
    {
        this.spawnDensity = density;
    }
    
    public void SetLevel(int level)
    {
        this.enemyLevel = level;
        
        //this.eliteChance = 0;//100;
    }

    public void StartSpawning()
    {
        InvokeRepeating(nameof(ProcessSpawns), 0.0F, spawnInterval);
    }

    public void StopSpawning()
    {
        CancelInvoke();
    }

    void ProcessSpawns()
    {
        if (areaManager != null && areaManager.AtMaxEnemies())
        {
            // Don't exceed maximum for area
            return;
        }

        if (entitiesInZone.Count < GetMaxEntities())
        {
            DoSpawn();
        }
    }

    void DoSpawn()
    {
        if (!StatefulCombatEntity.GetStatefulCombatEntityGlobalAIEnabled() || !spawnsEnabled)
        {
            return;
        }

        if (areaManager.spawnLists.Count == 0 || !areaManager.spawnLists.Any(spawnList => spawnList != null))
        {
            Debug.LogWarning("No valid spawn lists given to spawn zone!");
        }
        

        
        Entity newEntityPrefab = SpawnList.GetEntityFromSpawnLists(areaManager.spawnLists);
        areaManager.SpawnEnemy(newEntityPrefab);
        //spawnedEntity.stateMachine.ChangeState(new SimpleCombatEntity.EndlessPursuitState(spawnedEntity, spawnedEntity.GetNearestEnemy()));
    }

    public Vector3 GetSpawnPosition()
    {
        Collider collider = GetComponent<Collider>();
        Vector3 colliderCenter = collider.bounds.center;
        
        Vector3 horizontalPosition = new();
        if (collider is SphereCollider sphereCollider)
        {
            Vector2 unitCirclePoint = UnityEngine.Random.insideUnitCircle * sphereCollider.radius;
            horizontalPosition = colliderCenter + new Vector3(unitCirclePoint.x, 0, unitCirclePoint.y);
        }
        else if (collider is BoxCollider boxCollider)
        {
            Vector3 extents = boxCollider.bounds.extents;
            float xOffset = UnityEngine.Random.Range(-extents.x, extents.x);
            float zOffset = UnityEngine.Random.Range(-extents.z, extents.z);
            horizontalPosition = colliderCenter + new Vector3(xOffset, 0, zOffset);
        }
        else 
        {
            throw new System.Exception("Could not create a position for spawn zone - probably unsupported collider");
        }


        return horizontalPosition;
    }

    public int GetMaxEntities()
    {
        return (int) (GetVolume() * spawnDensity);
    }

    public float GetVolume()
    {
        float volume = 0.0F;
        if (TryGetComponent(out BoxCollider boxCollider))
        {
            volume = boxCollider.size.x * boxCollider.size.y * boxCollider.size.z;
        }
        else if (TryGetComponent(out SphereCollider sphereCollider))
        {
            volume = (4/3) * Mathf.PI * Mathf.Pow(sphereCollider.radius, 3);
        }


        return volume;
    }
    
    /// --- EDITOR ONLY ----------------------------------------------------------------
    #region EDITOR ONLY
    void OnDrawGizmos()
    {
        //Collider collider = GetComponentInChildren<Collider>();
        //Gizmos.color = Color.cyan;
        //if (collider is SphereCollider sphereCollider)
        //{
        //    Gizmos.DrawWireSphere(sphereCollider.transform.position, sphereCollider.radius);
        //}
        //else if (collider is BoxCollider boxCollider)
        //{
        //    Gizmos.DrawWireCube(boxCollider.bounds.center, boxCollider.size);
        //}
    }
    #endregion

}