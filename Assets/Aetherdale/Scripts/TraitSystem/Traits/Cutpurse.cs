

using System.Collections.Generic;
using UnityEngine;

public class Cutpurse : Trait
{
    public const int PERCENTAGE_EXTRA_GOLD = 30;
    public override string GetName()
    {
        return "Cutpurse";
    }

    public override Rarity GetRarity()
    {
        return Rarity.Common;
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"Enemies drop {numberOfStacks *  PERCENTAGE_EXTRA_GOLD}% extra gold.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().cutpurseSprite;
    }

    public override int GetProcOrder()
    {
        return -1;
    }


    public Cutpurse()
    {
    }
    
    float GetQuantityMultiplier()
    {
        return 1 + (numberOfStacks *  (PERCENTAGE_EXTRA_GOLD / 100.0F));
    }


    public override void ModifyEnemyItemDrops(Entity killer, List<DropInstance> drops)
    {
        foreach (DropInstance dropInstance in drops)
        {
            if (dropInstance.item == AetherdaleData.GetAetherdaleData().goldCoinsItem)
            {
                dropInstance.quantity = (int) (dropInstance.quantity * GetQuantityMultiplier());
            }
        }
    }
}
