using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemManager
{
    public static ItemData LookupItemData(string itemID)
    {
        Object[] items = Resources.LoadAll("Items", typeof(ItemData));

        foreach(Object loaded in items)
        {
            if (loaded is ItemData itemData)
            {
                if (itemData.GetItemID() == itemID)
                {
                    return itemData;
                }
            }
        }

        return null;
    }

    public static ItemData LookupItemDataByName(string itemName)
    {
        string lookupName = itemName.Replace(" ", "").ToLower();

        Object[] items = Resources.LoadAll("Items", typeof(ItemData));

        foreach(Object loaded in items)
        {
            if (loaded is ItemData itemData)
            {
                string scrubbedName = itemData.GetName().Replace(" ", "").ToLower();
                if (scrubbedName == lookupName)
                {
                    return itemData;
                }
            }
        }

        return null;
    }

    public static List<WeaponData> GetAllWeapons()
    {
        List<WeaponData> ret = new();
        
        Object[] items = Resources.LoadAll("Items", typeof(WeaponData));

        foreach (Object loaded in items)
        {
            if (loaded is WeaponData weaponData)
            {
                ret.Add(weaponData);
            }
        }

        return ret;
    }
}