using UnityEngine;

/// <summary>
/// Ability Strength Trait
/// </summary>
public class Attunement : Trait
{
    const int PERCENT_ABILITY_DAMAGE = 25;
    public override string GetName()
    {
        return "Attunement";
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"+{PERCENT_ABILITY_DAMAGE}% ability strength.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().attunementSprite;
    }

    public Attunement()
    {
        AddStatChange(new(Stats.AbilityStrength, StatChangeType.Multiplier, PERCENT_ABILITY_DAMAGE * 0.01F));
    }
}
