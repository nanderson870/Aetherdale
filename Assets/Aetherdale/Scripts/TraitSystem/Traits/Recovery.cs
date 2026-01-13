using UnityEngine;

/// <summary>
/// Attack Damage Trait
/// </summary>
public class Recovery : Trait
{
    const int HEALTH_REGEN = 1;

    public override string GetName()
    {
        return "Recovery";
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"+{HEALTH_REGEN} health regenerated per second.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().recoverySprite;
    }


    public Recovery()
    {
        AddStatChange(new(Stats.HealthRegen, StatChangeType.Flat, HEALTH_REGEN));
    }
}
