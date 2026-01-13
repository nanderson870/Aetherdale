using UnityEngine;

/// <summary>
/// Movement Speed Trait
/// </summary>
public class Haste : Trait
{
    const int PERCENT_MOVE_SPEED = 15;
    public override string GetName()
    {
        return "Haste";
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"+{PERCENT_MOVE_SPEED}% movement speed.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().hasteSprite;
    }

    public Haste()
    {
        AddStatChange(new(Stats.MovementSpeed, StatChangeType.Multiplier, PERCENT_MOVE_SPEED * 0.01F));
    }
}
