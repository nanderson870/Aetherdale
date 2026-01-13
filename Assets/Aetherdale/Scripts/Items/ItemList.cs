using System.Collections.Generic;
using UnityEngine;

public class ItemList
{
    List<Item> items = new();

    public void Add(Item addedItem)
    {
        foreach (Item item in items)
        {
            if (item.GetItemID() == addedItem.GetItemID())
            {
                item.AddQuantity(addedItem.GetQuantity());
                return;
            }
        }

        items.Add(addedItem);
    }

    public List<Item> GetItems()
    {
        return items;
    }

    public static string Serialize(ItemList itemList)
    {
        string serialized = "";

        for (int i = 0; i < itemList.items.Count; i++)
        {
            serialized += Item.Serialize(itemList.items[i]);

            if (i < itemList.items.Count - 1)
            {
                serialized += ";";
            }
        }

        return serialized;
    }

    public static ItemList Deserialize(string serialized)
    {
        ItemList itemList = new();

        if (serialized == "")
        {
            return itemList;
        }

        string[] serializedItems = serialized.Split(";");

        foreach (string serializedItem in serializedItems)
        {
            itemList.Add(Item.Deserialize(serializedItem));
        }

        return itemList;
    }
}