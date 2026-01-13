

using UnityEngine;

public class Reckless : Trait
{
    public const int PERCENT_COOLDOWN_REDUCTION = 50;
    public const float ENERGY_REPLACEMENT = 0.33F;
    public override string GetName()
    {
        return "Reckless";
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"Ability cooldowns are reduced by {PERCENT_COOLDOWN_REDUCTION}%. {ENERGY_REPLACEMENT * 100}% of ability cost is refunded, and removed from health instead.";
    }

    public override Rarity GetRarity()
    {
        return Rarity.Cursed;
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().recklessSprite;
    }

    public Reckless()
    {
        maxStacks = 1;

        AddStatChange(new(Stats.AbilityCooldownRatio, StatChangeType.Multiplier, -PERCENT_COOLDOWN_REDUCTION * 0.01F));
    }


    public override void OnAbility(Entity owner, int energyUsed)
    {
        base.OnAbility(owner, energyUsed);

        int replacement = (int) (energyUsed * ENERGY_REPLACEMENT);

        Debug.Log("Ability use " + energyUsed);
        owner.Damage(replacement, Element.TrueDamage, HitType.None, owner);
        owner.AddEnergy(replacement);
    }
}