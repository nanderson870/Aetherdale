using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class Hurtbox : MonoBehaviour, Damageable
{
    [SerializeField] float damageMult = 1.0F; // multiply all damage incoming through this hurtbox by this
    [SerializeField] int hurtBoxHealth = 0; // this hurtbox breaks if it takes this much damage. 0 to disable
    [SerializeField] int priority = 0; // No magic numbers to this. Just set it higher than hurtboxes you want it to take priority over
    [SerializeField] bool distanceMatters = false; // Whether priority only matters if the center of the collider is closer
    public bool projectileAutoCritical = false; // Whether projectiles auto-crit on this hurtbox
    [SerializeField] EventReference hitSound;

    Entity owningEntity;
    int remainingHurtBoxHealth;

    public void Start()
    {
        owningEntity = GetComponentInParent<Entity>();
        remainingHurtBoxHealth = hurtBoxHealth;
    }

    public HitInfo Damage(int damage, Element damageType, HitType hitType, Entity damageDealer = null, int impact = 0, bool forceCritical = false, bool forceStatus = false, int originEffectInstanceId = 0, HitboxHitData hitboxHitData=null, bool allowHitSound=true, bool scaleTick = false)
    {
        int adjustedDamage = damage;
        if (damageType != Element.TrueDamage)
        {
            adjustedDamage = (int)(damageMult * damage);

            if (adjustedDamage == 0 && damage != 0)
            {
                adjustedDamage = 1;
            }
        }

        bool useParentAudio = true;
        if (!hitSound.IsNull)
        {
            AudioManager.Singleton.PlayOneShot(hitSound, transform.position);
            useParentAudio = false;
        }

        if (hurtBoxHealth != 0)
        {
            remainingHurtBoxHealth -= adjustedDamage;
            if (remainingHurtBoxHealth <= 0)
            {
                Break();
            }
        }

        return owningEntity.Damage(adjustedDamage, damageType, hitType, damageDealer, impact, forceCritical, forceStatus, allowHitSound:useParentAudio);
    }

    public Entity GetDamageableEntity()
    {
        return owningEntity;
    }

    public int GetDamageablePriority()
    {
        return priority;
    }

    public bool DamageableColliderDistanceMatters()
    {
        return distanceMatters;
    }

    public bool IsInvulnerable()
    {
        return owningEntity.IsInvulnerable();
    }

    public void Break()
    {

    }

}
