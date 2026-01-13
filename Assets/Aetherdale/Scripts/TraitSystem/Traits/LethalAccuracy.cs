

using UnityEngine;

/// <summary>
/// Crit Mult Trait
/// </summary>
public class LethalAccuracy : Trait
{
    const int PERCENT_CRIT_DAMAGE = 25;

    public override string GetName()
    {
        return "Lethal Accuracy";
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"Critical hits deal +{PERCENT_CRIT_DAMAGE}% damage.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().lethalAccuracySprite;
    }

    public override Rarity GetRarity()
    {
        return Rarity.Uncommon;
    }

    public LethalAccuracy()
    {
        AddStatChange(new(Stats.CriticalDamageMultiplier, StatChangeType.Multiplier, PERCENT_CRIT_DAMAGE * 0.01F));
    }

    public override bool PlayerMeetsRequirements(Player player)
    {
        foreach (Trait trait in player.GetTraits())
        {
            if (trait is Expertise)
            {
                return true;
            }
        }

        return false;
    }
}