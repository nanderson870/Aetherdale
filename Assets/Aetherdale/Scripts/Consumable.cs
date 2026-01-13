
using System;
using UnityEngine;


public abstract class Consumable : IShopOffering
{
    public enum ConsumableSlotType
    {
        Offensive,
        Defensive,
        Utility
    }

    public Action OnConsumed;

    public abstract void Use(PlayerWraith playerWraith);
    public abstract void Update(PlayerWraith playerWraith);
    public abstract void Release(PlayerWraith playerWraith);

    public abstract void GiveToPlayer(Player player);

    public abstract Sprite GetIcon();

    public abstract string GetName();
    public abstract string GetStatsDescription(Player targetPlayer = null);
    public abstract string GetDescription();
    public abstract int GetShopCost();
    public abstract ShopOfferingInfo GetInfo();

    public abstract ConsumableSlotType GetSlot();

    public abstract GameObject GetPreviewPrefab();


    public virtual bool PlayerMeetsRequirements(Player player) 
    {
        if (GetMaxCount() >= 1)
        {
            Inventory inventory = player.GetInventory();
            Consumable consumable = inventory.GetConsumableSlot(GetSlot()).GetConsumable();
            if (consumable == null)
            {
                // No current consumable, slot is free
                return true;
            }

            bool differentTypeConsumable = consumable.GetType() != GetType();
            bool notAtMax = inventory.OffensiveConsumableSlot.GetConsumableCount() < GetMaxCount();

            return differentTypeConsumable || notAtMax;
        }

        return true;
    }

    public virtual int GetMaxCount()
    {
        return -1;
    }
}