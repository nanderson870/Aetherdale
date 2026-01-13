using UnityEngine;

/// <summary>
/// Attack Speed Trait
/// </summary>
public class Acrobat : Trait
{
    public override string GetName()
    {
        return "Acrobat";
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"+1 jump";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().acrobatSprite;
    }

    public override Rarity GetRarity()
    {
        return Rarity.Rare;
    }

    public Acrobat()
    {
        AddStatChange(new(Stats.NumberOfJumps, StatChangeType.Flat, 1));
    }
}