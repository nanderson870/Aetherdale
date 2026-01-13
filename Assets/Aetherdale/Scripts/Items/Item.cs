using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

[System.Serializable]
public class Item : IShopOffering
{
    public readonly ItemData itemData;
    int quantity;

    public Item(ItemData data, int quantity = 1)
    {
        itemData = data;
        this.quantity =  quantity;
    }

    public Item(string itemId, int quantity = 1)
    {
        itemData = ItemManager.LookupItemData(itemId);
        this.quantity = quantity;
    }

    public string GetItemID()
    {
        return itemData.GetItemID();
    }

    public string GetName()
    {
        return itemData.GetName();
    }

    public string GetDescription()
    {
        if (!Player.GetLocalPlayer().GetPlayerData().HasItem(GetItemID()))
        {
            return itemData.GetUnlockHint();
        }

        return itemData.GetDescription();
    }

    public string GetStatsDescription(Player targetPlayer = null)
    {
        return itemData.GetStatsDescription();
    }

    public void SetQuantity(int quantity)
    {
        this.quantity = quantity;
    }

    public int GetQuantity()
    {
        return quantity;
    }

    public virtual Rarity GetRarity()
    {
        return itemData.GetRarity();
    }

    public int GetValue()
    {
        return itemData.GetValue();
    }

    public bool IsUnique()
    {
        return itemData.IsUnique();
    }

    public virtual Sprite GetIcon()
    {
        return itemData.GetIcon();
    }


    public void AddQuantity(int added)
    {
        quantity += added;
    }

    public static string Serialize(Item item)
    {
        return $"{item.GetItemID()}|{item.quantity}";
    }

    public static Item Deserialize(string itemString)
    {
        string[] splitItemString = itemString.Split("|");
        
        ItemData itemData = ItemManager.LookupItemData(splitItemString[0]);
        int quantity = int.Parse(splitItemString[1]);
        if (itemData != null)
        {
            return new Item(itemData, quantity);
        }

        throw new System.Exception("Item could not be found for id " + splitItemString[0]);
    }

    
    public ShopOfferingInfo GetInfo()
    {
        return new()
        {
            type = ShopOfferingType.Item,
            name = GetName(),
            typeName = nameof(Item),
            statsDescription = GetStatsDescription(),
            description = GetDescription(),
            goldCost = GetShopCost()
        };
    }

    public GameObject GetPreviewPrefab()
    {
        return itemData.GetMesh();
    }

    public virtual int GetShopCost()
    {
        return itemData.GetValue();
    }

    // IShopOffering override
    public virtual void GiveToPlayer(Player player)
    {
        throw new Exception("Give to player as shop offering not implemented");
    }


    public bool PlayerMeetsRequirements(Player player)
    {
        return true;
    }


}