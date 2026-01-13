using UnityEngine;
using UnityEngine.Events;

public class Breakable : MonoBehaviour, Damageable
{
    public int hitpoints = 1;
    public UnityEvent OnBreak;

    void Break()
    {
        Debug.Log("BREAK");
        OnBreak?.Invoke();
    }

    public HitInfo Damage(int damage, Element damageType, HitType hitType, Entity damageDealer = null, int impact = 0, bool forceCritical = false, bool forceStatus = false, int originEffectInstanceId = 0, HitboxHitData hitboxHitData = null, bool allowHitSound = true, bool scaleTick = true)
    {
        HitInfo info = new();
        info.damageDealt = damage;
        info.damageType = damageType;
        info.hitType = hitType;
        info.damageDealer = damageDealer;

        hitpoints -= damage;
        if (hitpoints <= 0)
        {
            Break();
        }

        return info;
    }

    public Entity GetDamageableEntity()
    {
        return null;
    }

    public int GetDamageablePriority()
    {
        return 0;
    }

    public bool IsInvulnerable()
    {
        return false;
    }

    public bool DamageableColliderDistanceMatters()
    {
        return false;
    }
}
