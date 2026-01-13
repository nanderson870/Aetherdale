using System.Collections.Generic;
using UnityEngine;

public class Lava : MonoBehaviour
{
    [SerializeField] float damagePerSecond = 10.0F;
    [SerializeField] float damageInterval = 0.5F;
    [SerializeField] Element damageType = Element.Fire;

    List<Entity> collidingEntities = new();

    void Start()
    {
        InvokeRepeating(nameof(Process), 0, damageInterval);
    }

    void OnTriggerEnter(Collider coll)
    {
        if (coll.gameObject.TryGetComponent(out Entity entity))
        {
            if (!collidingEntities.Contains(entity))
            {
                collidingEntities.Add(entity);

            }
        }
    }

    void OnTriggerExit(Collider coll)
    {
        if (coll.gameObject.TryGetComponent(out Entity entity))
        {
            if (collidingEntities.Contains(entity))
            {
                collidingEntities.Remove(entity);
            }
        }
    }

    void Process()
    {
        for (int i = collidingEntities.Count - 1; i >= 0; i--)
        {
            if (collidingEntities[i] == null || !collidingEntities[i].isActiveAndEnabled)
            {
                collidingEntities.RemoveAt(i);
            }
            
            collidingEntities[i].Damage((int) (damagePerSecond * damageInterval), damageType, HitType.Environment, null, 0);
        }
    }
}
