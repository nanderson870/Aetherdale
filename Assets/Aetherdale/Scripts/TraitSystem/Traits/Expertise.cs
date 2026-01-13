

using UnityEngine;

/// <summary>
/// Crit Chance Trait
/// </summary>
public class Expertise : Trait
{
    const int PERCENT_CRIT_CHANCE = 10;

    public override string GetName()
    {
        return "Expertise";
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"+{PERCENT_CRIT_CHANCE}% chance to critically hit.";
    }
    
    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().expertiseSprite;
    }


    public Expertise()
    {
        AddStatChange(new(Stats.CriticalChance, StatChangeType.Flat, PERCENT_CRIT_CHANCE));
    }
}