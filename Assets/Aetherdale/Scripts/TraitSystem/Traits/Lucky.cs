
using UnityEngine;

/// <summary>
/// Pure skill
/// </summary>
public class Lucky : Trait
{
    const float LUCK_MOD = 15;

    public override string GetName()
    {
        return "Lucky";
    }

    public override Rarity GetRarity()
    {
        return Rarity.Legendary;
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"Received traits have a chance to be of a higher rarity.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().luckySprite;
    }


    public Lucky()
    {
        maxStacks = 3;

        AddStatChange(new(Stats.Luck, StatChangeType.Flat, LUCK_MOD));
    }
}
