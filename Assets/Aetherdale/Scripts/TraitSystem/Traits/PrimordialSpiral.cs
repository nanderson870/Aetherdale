using UnityEngine;

/// <summary>
/// Elemental damage repeats
/// </summary>
public class PrimordialSpiral : Trait
{
    const float PERCENT_CHANCE = 61.8F;
    const float PERCENT_OF_ORIGINAL = 61.8F;

    const float REPEAT_HIT_DELAY = 0.5F;

    public override string GetName()
    {
        return "Primordial Spiral";
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"Elemental damage you deal has a +{PERCENT_CHANCE}% to repeat itself, at {PERCENT_OF_ORIGINAL}% of the original damage.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().primordialSpiralSprite;
    }

    public override Rarity GetRarity()
    {
        return Rarity.Legendary;
    }

    public PrimordialSpiral()
    {
        maxStacks = 1;
    }

    public override void OnHit(HitInfo hitResult)
    {
        if (hitResult.damageType == Element.Physical)
        {
            return;
        }

        if (Random.Range(0, 100) < PERCENT_CHANCE && hitResult.damageDealt > 1)
        {
            int damage = (int) (hitResult.premitigationDamage * (PERCENT_OF_ORIGINAL * 0.01F));
            if (damage > 0)
            {
                AudioManager.Singleton.PlayOneShot(AetherdaleData.GetAetherdaleData().soundData.spiralProcSound, hitResult.hitPosition);

                GameObject.Instantiate(VisualEffectIndex.GetDefaultEffectIndex().spiralProcVFX, hitResult.hitPosition, Quaternion.identity)
                    .transform.SetParent(hitResult.entityHit.transform);
                    //.transform.localScale *= hitResult.entityHit.GetSize();

                hitResult.entityHit.DamageInSeconds(0.5F, damage, hitResult.damageType, HitType.Ability, hitResult.damageDealer);
            }
        }
    }
}
