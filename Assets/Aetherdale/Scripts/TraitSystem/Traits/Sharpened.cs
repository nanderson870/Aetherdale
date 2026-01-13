using UnityEngine;

public class Sharpened : Trait
{
    public int PERCENT_ADDITIONAL_TRUE_DAMAGE = 10;
    public override string GetName()
    {
        return "Sharpened";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().sharpenedSprite;
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"Deal an additional {GetPercentAdditionalDamage()}% damage as True Damage, ignoring armor and other resistances. ({GetPercentAdditionalDamage() * 2}% on critical hits)";
    }

    int GetPercentAdditionalDamage()
    {
        return PERCENT_ADDITIONAL_TRUE_DAMAGE * numberOfStacks;
    }

    public override Rarity GetRarity()
    {
        return Rarity.Uncommon;
    }

    public override void OnHit(HitInfo hitResult)
    {
        if (hitResult.hitType == HitType.Attack || hitResult.hitType == HitType.Ability)
        {
            float multiplier = GetPercentAdditionalDamage() * 0.01F;
            if (hitResult.criticalHit)
            {
                multiplier *= 2;
            }

            int additionalDamage = (int)(multiplier * hitResult.premitigationDamage);
            if (additionalDamage < 1) additionalDamage = 1;

            hitResult.entityHit.Damage(additionalDamage, Element.TrueDamage, HitType.Effect, hitResult.damageDealer);
        }
    }
}