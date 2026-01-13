using UnityEngine;

public class Featherweight : Trait
{
    const int PERCENT_MOVE_SPEED = 30;
    const int PERCENT_MAX_HEALTH = 30;
    public override string GetName()
    {
        return "Featherweight";
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"+{PERCENT_MOVE_SPEED}% movement speed\n-{PERCENT_MAX_HEALTH}% max health";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().featherweightSprite;
    }

    public override Rarity GetRarity()
    {
        return Rarity.Cursed;
    }

    public Featherweight()
    {
        maxStacks = 2;
        AddStatChange(new(Stats.MovementSpeed, StatChangeType.Multiplier, PERCENT_MOVE_SPEED * 0.01F));
        AddStatChange(new(Stats.MaxHealth, StatChangeType.Multiplier, -PERCENT_MAX_HEALTH * 0.01F));
    }
}