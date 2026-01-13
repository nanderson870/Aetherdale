

using FMODUnity;
using UnityEngine;

public class Transcendent : Trait
{
    public override string GetName()
    {
        return "Transcendent";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().transcendentSprite;
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        if (numberOfStacks <= 1)
        {
            return "Once per area, return from death with full health.";
        }
        else
        {
            return $"{numberOfStacks} times per area, return from death with full health.";
        }
    }

    public override Rarity GetRarity()
    {
        return Rarity.Epic;
    }

    public override void OnNewArea(Player player)
    {
        Debug.Log("On new area for Transcendent: " + player);
        player.GetControlledEntity().AddEffect(AetherdaleData.GetAetherdaleData().transcendentEffect, null, numberOfStacks);
    }

    public override EventReference GetAcquiredSound()
    {
        return AetherdaleData.GetAetherdaleData().soundData.traits.transcendentAcquired;
    }
}