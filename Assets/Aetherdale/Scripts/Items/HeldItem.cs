using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/* class for item's space in an Inventory */
[System.Serializable]
public class HeldItem
{
    public ItemData item;
    public int quantity;

    public HeldItem(ItemData item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
    }

    public HeldItem Clone()
    {
        return new HeldItem(item, quantity);
    }
}