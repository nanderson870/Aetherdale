

using Aetherdale;
using Mirror;
using UnityEngine;

public class Absorption : Trait
{
    public const int ABSORB_CHANCE = 20;

    GameObject bubbleShieldPrefab;


    public override string GetName()
    {
        return "Absorption";
    }

    public override Rarity GetRarity()
    {
        return Rarity.Rare;
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"+{ABSORB_CHANCE}% chance to absorb incoming hits.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().absorptionSprite;
    }


    public Absorption()
    {
        maxStacks = 3;
        
        AddStatChange(new(Stats.AbsorbChance, StatChangeType.Flat, ABSORB_CHANCE));
        
        bubbleShieldPrefab = VisualEffectIndex.GetDefaultEffectIndex().waterEliteVFX;
    }

    // public override void OnTraitAcquired(Player player, TraitList receivingList)
    // {
    //     if (!player.GetControlledEntity().HasEphemera())
    //     {
    //         player.GetControlledEntity().AddEliteEphemera(Element.Water);
    //     }
    // }

    // public override void OnTransform(Entity previous, Entity newEntity)
    // {
    //     if (!newEntity.HasEphemera())
    //     {
    //         newEntity.AddEliteEphemera(Element.Water);
    //     }
    // }

}