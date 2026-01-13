using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/*

Triggers only when all specified entities have been slain

*/

public class EntityKillTrigger : MonoBehaviour
{
    public UnityEvent trigger;

    [SerializeField] List<Entity> entities;

    void Start()
    {
        foreach (Entity entity in entities)
        {
            entity.OnDeath += (Entity entity, Entity killer) => Check(entity);
        }
    }

    void Check(Entity entity)
    {
        entities.Remove(entity);
        if (entities.Count == 0)
        {
            trigger.Invoke();
        }
    }
}
