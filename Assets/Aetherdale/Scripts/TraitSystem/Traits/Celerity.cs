using UnityEngine;

/// <summary>
/// Idol Drain Reduction Trait
/// </summary>
public class Celerity : Trait
{
    const int PERCENT_SPRINT_SPEED = 15;

    public override string GetName()
    {
        return "Celerity";
    }

    public override Rarity GetRarity()
    {
        return Rarity.Uncommon;
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"+{PERCENT_SPRINT_SPEED}% additional sprint speed.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().celeritySprite;
    }


    public Celerity()
    {
        AddStatChange(new(Stats.SprintSpeedMult, StatChangeType.Flat, PERCENT_SPRINT_SPEED * 0.01F));
    }
}
