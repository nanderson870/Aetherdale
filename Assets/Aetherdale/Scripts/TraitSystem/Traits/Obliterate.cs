using System;
using UnityEngine;

/// <summary>
/// Additional Damage To Undamaged Enemies
/// </summary>
public class Obliterate : Trait
{
    const int PERCENT_ADDITIONAL_DAMAGE = 50;

    public override string GetName()
    {
        return "Obliterate";
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"+{PERCENT_ADDITIONAL_DAMAGE}% damage against enemies you have not previously damaged.";
    }
    
    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().obliterateSprite;
    }

    public override void OnHit(HitInfo hitResult)
    {
    }

    public override float ModifyDamageForTarget(Entity attacker, Entity target, float damage)
    {
        if (!target.HasBeenDamagedBy(attacker))
        {
            AudioManager.Singleton.PlayOneShot(AetherdaleData.GetAetherdaleData().soundData.obliterateSound, target.transform.position);
            float modified = damage * (1 + PERCENT_ADDITIONAL_DAMAGE * 0.01F * numberOfStacks);
            return modified;
        }

        return damage;
    }
}