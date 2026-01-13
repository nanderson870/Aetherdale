using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class IdolItem : Item
{
    readonly IdolItemData data;

    public IdolItem(IdolItemData data) : base(data)
    {
        this.data = data;
    }

    public IdolForm GetAssociatedForm()
    {
        return data.GetAssociatedForm();
    }

    public Color GetResourceColor()
    {
        return ColorPalette.GetPrimaryColorForElement(data.GetElement());
    }

    public static string Serialize(IdolItem idol)
    {
        return $"{idol.GetItemID()}";
    }

    public static new IdolItem Deserialize(string serialized)
    {
        IdolItemData idolData = ItemManager.LookupItemData(serialized) as IdolItemData;

        return new(idolData);
    }


}