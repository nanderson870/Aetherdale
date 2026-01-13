using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(Collider))]
public class EntityTrackingZone : MonoBehaviour
{
    protected List<Entity> entitiesInZone = new();


    public Action<Entity, Entity> OnEntityDiedInZone;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out Entity entity) && entity.gameObject.activeSelf)
        {
            EntityEnteredZone(entity);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.TryGetComponent(out Entity entity))
        {
            EntityExitedZone(entity);
        }
    }

    void EntityEnteredZone(Entity newEntity)
    {
        if (!entitiesInZone.Contains(newEntity))
        {
            entitiesInZone.Add(newEntity);
            newEntity.OnTransformed += OnEntityTransformed;

            newEntity.OnDeath += EntityDiedInZone;
        }
    }

    void EntityExitedZone(Entity leavingEntity)
    {
        entitiesInZone.Remove(leavingEntity);
    }

    void OnEntityTransformed(Entity oldEntity, Entity newEntity)
    {
        if (entitiesInZone.Contains(oldEntity))
        {
            entitiesInZone.Remove(oldEntity);
        }
    }

    public List<T> GetEntitiesInZone<T>()
    {
        List<T> ret = new();

        foreach (Entity entity in entitiesInZone)
        {
            if (entity == null)
            {
                continue;
            }
            
            if (entity is T t && entity.gameObject.activeSelf)
            {
                ret.Add(t);
            }
        }

        return ret;
    }

    public void EntityDiedInZone(Entity entity, Entity killer)
    {
        OnEntityDiedInZone?.Invoke(entity, killer);
    }
}