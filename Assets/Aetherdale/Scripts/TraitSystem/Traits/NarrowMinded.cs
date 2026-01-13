using UnityEngine;

/// <summary>
/// -1 trait option, rarer traits
/// </summary>
public class NarrowMinded : Trait
{
    const int TRAIT_OPTIONS_LOST = 1;
    const float LUCK_MOD = 20;

    public override string GetName()
    {
        return "Narrow Minded";
    }

    public override Rarity GetRarity()
    {
        return Rarity.Cursed;
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"Lose an option when selecting traits, but received trait options have a chance to be a higher rarity.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().narrowMindedSprite;
    }


    public NarrowMinded()
    {
        maxStacks = 2;

        AddStatChange(new(Stats.TraitOptions, StatChangeType.Flat, -TRAIT_OPTIONS_LOST));
        AddStatChange(new(Stats.Luck, StatChangeType.Flat, LUCK_MOD));
    }
}
