

using System.Collections.Generic;
using UnityEngine;

public class CursedCoin : Trait
{
    public const int DAMAGE = 5;
    public const int PERCENTAGE = 25;
    public override string GetName()
    {
        return "Cursed Coin";
    }

    public override Rarity GetRarity()
    {
        return Rarity.Cursed;
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"Coins dropped from enemies can become seeking projectiles.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().cursedCoinSprite;
    }


    public CursedCoin()
    {
        maxStacks = 1;
    }

    public int GetPercentReplaced()
    {
        return numberOfStacks * PERCENTAGE;
    }

    public override void ModifyEnemyItemDrops(Entity killer, List<DropInstance> drops)
    {
        Debug.Log(killer);
        foreach (DropInstance dropInstance in drops)
        {
            if (dropInstance.item == AetherdaleData.GetAetherdaleData().goldCoinsItem)
            {
                int quantity = dropInstance.quantity;

                int cursedQuantity = (int) (quantity * GetPercentReplaced() / 100.0F);

                drops.Add(new ()
                {
                    item = AetherdaleData.GetAetherdaleData().cursedCoinItemData,
                    quantity = cursedQuantity,
                    requirements = new(),
                    OnDropped = (LootItem lootItem) => {InitializeCursedCoin(killer, lootItem);}
                });

                dropInstance.quantity -= cursedQuantity;
                return;
            }
        }
    }

    void InitializeCursedCoin(Entity progenitor, LootItem cursedCoin)
    {
        GuaranteedSeekingProjectile projectile = cursedCoin.GetComponent<GuaranteedSeekingProjectile>();
        projectile.progenitor = progenitor.gameObject;
        projectile.faction = progenitor.GetFaction();
        projectile.SetDamage(DAMAGE);

        projectile.SetActive(false);
        //projectile.SetActive(true, 0.5F);
    }
}
