using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;


public class Hitbox : MonoBehaviour
{
    public bool hitsOwner = false;
    [SerializeField] bool hitsAllies = false;

    Entity owningEntity;
    Collider hitboxCollider;
    
    List<Damageable> hitDamageables = new();
    
    int currentDamage;
    Element currentElement = Element.Physical;
    HitType hitType = HitType.None;
    int currentImpact;
    Action<HitboxHitData> currentOnHitAction = null;
    bool forceCritical = false;

    bool inHit = false;

    Vector3 velocity = new();
    Vector3 lastPosition = new();

    /// <summary>
    /// Used to send collision events to a different object
    /// </summary>
    public Action<Collider> CollisionReroute;


    private void Start()
    {
        hitboxCollider = this.GetComponent<Collider>();
        GetComponent<Collider>().isTrigger = true;

        // if (!inHit)
        // {
        //     gameObject.SetActive(false);
        // }
    }

    

    void FixedUpdate()
    {
        if (!inHit)
        {
            return;
        }
        else
        {
            foreach (Collider collider in GetOverlappedColliders())
            {
                TryHit(collider);
            }
        }

        velocity = transform.position - lastPosition;
        lastPosition = transform.position;
    }

    /// <summary>
    /// Hit within this hitbox.
    /// </summary>
    /// <param name="damage">Damage to deal on hit</param>
    /// <param name="damageType">Type of the damage</param>
    /// <param name="damageDealer">Optional entity that is the source of the damage</param>
    /// <param name="onHitAction">Optional action to invoke with the target as an argument, on a successful hit</param>
    /// <returns>The results of any damage dealt by this hit</returns>
    [ServerCallback]
    public List<HitInfo> HitOnce(int damage, Element damageType, Entity damageDealer, Action<HitboxHitData> onHitAction = null, int impact = 0, HitType hitType = HitType.Attack)
    {
        inHit = false;
        List<HitInfo> results = new();

        List<Entity> hitEntities = new();

        // Sort collision data into hit entities
        Dictionary<GameObject, List<Damageable>> entityHitInfo = new();
        foreach (Collider collider in GetOverlappedColliders())
        {
            Damageable potentialDamageable = collider.gameObject.GetComponent<Damageable>();
            if (potentialDamageable != null //&& potentialDamageable.GetDamageableEntity() != null
                && (potentialDamageable.GetDamageableEntity() != damageDealer || hitsOwner))
            {
                GameObject hitObject = potentialDamageable.gameObject;
                Entity hitEntity = potentialDamageable.GetDamageableEntity();
                if (hitEntity != null)
                {
                    hitObject = hitEntity.gameObject;
                }
                
                if (!entityHitInfo.ContainsKey(hitObject))
                {
                    entityHitInfo.Add(hitObject, new List<Damageable>());
                }

                entityHitInfo[hitObject].Add(potentialDamageable);
            }
        }

        // Determine highest prio colliders 
        List<Damageable> highestPrioDamageables = new();
        foreach (KeyValuePair<GameObject, List<Damageable>> entityHit in entityHitInfo)
        {
            float nearestDistance = Vector3.Distance(hitboxCollider.bounds.center, entityHit.Value[0].gameObject.GetComponent<Collider>().bounds.center);
            Damageable highestPrio = entityHit.Value[0];
            foreach (Damageable damageable in entityHit.Value)
            {
                float distance = Vector3.Distance(hitboxCollider.bounds.center, damageable.gameObject.GetComponent<Collider>().bounds.center);
                if (damageable.GetDamageablePriority() > highestPrio.GetDamageablePriority() && (!damageable.DamageableColliderDistanceMatters() || distance < nearestDistance))
                {
                    highestPrio = damageable;
                }
            }

            highestPrioDamageables.Add(highestPrio);
        }

        // Apply hits
        foreach (Damageable damageable in highestPrioDamageables)
        {
            Entity hitEntity = damageable.GetDamageableEntity();

            if (hitEntity != null 
                && ((damageDealer != null && damageDealer.IsAlly(hitEntity) && !hitsAllies)
                    || hitEntities.Contains(hitEntity)))
            {
                continue;
            }

            HitInfo result = damageable.Damage((int) damage, damageType, hitType, damageDealer, impact:impact);
            results.Add(result);

            bool killedTheTarget = result.killedTarget;

            if (hitEntity != null)
            {
                hitEntities.Add(hitEntity);
            }
                    
            if (killedTheTarget && damageDealer != null)
            {
                damageDealer.KilledAnEntity(result);
            }
            
            Vector3 collisionPoint = hitboxCollider.ClosestPointOnBounds(GetComponent<Collider>().bounds.center);

            HitboxHitData data = new();
            data.hitEntity = hitEntity;
            data.position = collisionPoint;
            data.velocity = Vector3.zero;

            onHitAction?.Invoke(data);
        }

        return results;
    }

    Collider[] GetOverlappedColliders()
    {
        // Collect collision data
        Physics.SyncTransforms();
        if (hitboxCollider is CapsuleCollider collAsCapsule)
        {
            Vector3 collTop = new(collAsCapsule.bounds.center.x, collAsCapsule.bounds.center.y + (collAsCapsule.height / 2), collAsCapsule.bounds.center.z);
            Vector3 collBottom = new(collAsCapsule.bounds.center.x, collAsCapsule.bounds.center.y - (collAsCapsule.height / 2), collAsCapsule.bounds.center.z);
            return Physics.OverlapCapsule(collTop, collBottom, collAsCapsule.radius);
        }
        else if (hitboxCollider is SphereCollider collAsSphere)
        {
            return Physics.OverlapSphere(collAsSphere.bounds.center, collAsSphere.radius);
        }
        else
        {
            return Physics.OverlapBox(hitboxCollider.bounds.center, hitboxCollider.bounds.extents, Quaternion.identity);
        }
    }

    
    public void StartHit(int damage, Element element, HitType hitType, Entity damageDealer, int impact = 0, Action<HitboxHitData> onHitAction = null)
    {
        hitDamageables.Clear();
        gameObject.SetActive(true);

        this.owningEntity = damageDealer;

        currentDamage = damage;
        currentElement = element;
        this.hitType = hitType;
        currentImpact = impact;
        this.currentOnHitAction = onHitAction;

        forceCritical = false;

        inHit = true;
    }

    public void ResetHits()
    {
        hitDamageables.Clear();
    }

    public void SetForceCritical(bool forceCritical = true)
    {
        this.forceCritical = forceCritical;
    }

    public void EndHit()
    {
        inHit = false;
    }

    // [ServerCallback]
    // public void OnTriggerEnter(Collider collider) => TryHit(collider);

    // [ServerCallback]
    // public void OnTriggerStay(Collider collider) => TryHit(collider);

    [Server]
    void TryHit(Collider collider)
    {
        //Debug.Log(collider);
        if (!inHit)
        {
            return;
        }

        if (CollisionReroute != null)
        {
            CollisionReroute.Invoke(collider);
            return;
        }

        Vector3 collisionPoint = collider.ClosestPointOnBounds(GetComponent<Collider>().bounds.center);

        Damageable potentialDamageable = collider.gameObject.GetComponent<Damageable>();
        if (potentialDamageable != null)
        {
            Entity hitEntity = potentialDamageable.GetDamageableEntity();
            if ((hitEntity != owningEntity) || hitsOwner)
            {
                if (owningEntity != null)
                {
                    if (hitEntity != null && owningEntity.IsAlly(hitEntity))
                    {                   
                        // Do not hit allies
                        return;
                    }
                }

                if (hitEntity != null && hitDamageables.Contains(hitEntity))
                {
                    return;
                }

                if (!hitDamageables.Contains(potentialDamageable))
                {
                    Hit(potentialDamageable, collisionPoint);
                }
            }
        }
    }

    [Server]
    void Hit(Damageable damageable, Vector3 position)
    {
        HitboxHitData data = new();
        data.hitEntity = damageable.GetDamageableEntity();
        data.position = position;
        data.velocity = velocity;

        HitInfo result = damageable.Damage((int) currentDamage, currentElement, hitType, owningEntity, impact:currentImpact, forceCritical:forceCritical, hitboxHitData:data);

        // if (owningEntity != null)
        // {
        //    owningEntity.HitAnEntity(result);
        // }

        bool killedTheTarget = result.killedTarget;

        if (damageable.GetDamageableEntity() != null)
        {
            hitDamageables.Add(damageable.GetDamageableEntity());
        }
        else
        {
            hitDamageables.Add(damageable);
        }
         
        if (killedTheTarget && owningEntity != null)
        {
            owningEntity.KilledAnEntity(result);
        }

        currentOnHitAction?.Invoke(data);
    }

}

public enum HitType
{
    None = 0,
    Attack = 1,
    Ability = 2,
    Effect = 3,
    Environment = 4,
}


public class HitboxHitData
{
    public Entity hitEntity;
    public Vector3 position;
    public Vector3 velocity;
}