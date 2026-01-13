using UnityEngine;

/// <summary>
/// Additional trait option
/// </summary>
public class Potential : Trait
{
    const int ADDITIONAL_TRAIT_OPTIONS = 1;

    public override string GetName()
    {
        return "Potential";
    }

    public override Rarity GetRarity()
    {
        return Rarity.Rare;
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"Gain an additional option when selecting traits.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().potentialSprite;
    }


    public Potential()
    {
        maxStacks = 1;

        AddStatChange(new(Stats.TraitOptions, StatChangeType.Flat, ADDITIONAL_TRAIT_OPTIONS));
    }
}
