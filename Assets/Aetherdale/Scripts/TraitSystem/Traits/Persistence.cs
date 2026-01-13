using UnityEngine;

/// <summary>
/// Idol Drain Reduction Trait
/// </summary>
public class Persistence : Trait
{
    const int DRAIN_REDUCTION = -25;

    public override string GetName()
    {
        return "Persistence";
    }

    public override Rarity GetRarity()
    {
        return Rarity.Uncommon;
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"Idol passive health drain reduced by {DRAIN_REDUCTION}%.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().persistenceSprite;
    }


    public Persistence()
    {
        //???
        ///AddStatChange(new(Stats.IdolHealthDrain, StatChangeType.Flat, (float) DRAIN_REDUCTION / 100));
    }
}
