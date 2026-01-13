using UnityEngine;

/// <summary>
/// Attack Speed Trait
/// </summary>
public class Agility : Trait
{
    const int PERCENT_ATTACK_SPEED = 10;

    public override string GetName()
    {
        return "Agility";
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"+{PERCENT_ATTACK_SPEED}% attack speed.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().agilitySprite;
    }

    public Agility()
    {
        AddStatChange(new(Stats.AttackSpeed, StatChangeType.Multiplier, PERCENT_ATTACK_SPEED * 0.01F));
    }
}