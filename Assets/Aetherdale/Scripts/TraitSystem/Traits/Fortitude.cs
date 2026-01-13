using System.Diagnostics;
using UnityEngine;

/// <summary>
/// Max Health Trait
/// </summary>
public class Fortitude : Trait
{
    const int MAX_HEALTH_BONUS = 50;

    public override string GetName()
    {
        return "Fortitude";
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"+{MAX_HEALTH_BONUS} maximum health.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().fortitudeSprite;
    }

    public Fortitude()
    {
        AddStatChange(new(Stats.MaxHealth, StatChangeType.Flat, MAX_HEALTH_BONUS));
    }
}
