
using UnityEngine;

/// <summary>
/// +damage taken, dealt
/// </summary>
public class Berserk : Trait
{
    const int PERCENT_ADDITIONAL_DEALT_DAMAGE = 50;
    const int PERCENT_ADDITIONAL_TAKEN_DAMAGE = 50;

    public override string GetName()
    {
        return "Berserk";
    }

    public override Rarity GetRarity()
    {
        return Rarity.Cursed;
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"Deal +{PERCENT_ADDITIONAL_DEALT_DAMAGE}% damage, but also receive +{PERCENT_ADDITIONAL_TAKEN_DAMAGE}% more damage.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().berserkSprite;
    }


    public Berserk()
    {
        AddStatChange(new(Stats.GlobalDamageMultiplier, StatChangeType.Flat, PERCENT_ADDITIONAL_DEALT_DAMAGE / 100.0F));
        AddStatChange(new(Stats.DamageTakenMultiplier, StatChangeType.Flat, PERCENT_ADDITIONAL_TAKEN_DAMAGE / 100.0F));
    }
}
