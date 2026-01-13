using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackpackMenu : Menu
{
    [SerializeField] ItemSlot itemSlotPrefab;
    [SerializeField] Transform itemSlotGroup;

    public override void Close()
    {
        foreach (Transform slot in itemSlotGroup.transform)
        {
            Destroy(slot.gameObject);
        }

        base.Close();
    }

    public void SetItems(List<Item> inventory)
    {
        foreach (Item item in inventory)
        {
            ItemSlot slot = Instantiate(itemSlotPrefab, itemSlotGroup);
            slot.SetItem(item);
        }
    }
}
