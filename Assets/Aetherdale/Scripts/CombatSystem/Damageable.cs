using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface Damageable
{
    GameObject gameObject { get ; }

    // Returns whether killed destroyed
    public HitInfo Damage(int damage, Element damageType, HitType hitType, Entity damageDealer = null, int impact = 0, bool forceCritical = false, bool forceStatus = false, int originEffectInstanceId = 0, HitboxHitData hitboxHitData=null, bool allowHitSound=true, bool scaleTick = true);

    public Entity GetDamageableEntity();
    public int GetDamageablePriority();
    public bool IsInvulnerable();
    public bool DamageableColliderDistanceMatters();
}

