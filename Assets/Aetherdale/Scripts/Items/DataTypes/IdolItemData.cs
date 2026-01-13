using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Aetherdale/Item Data/Idol", order = 0)]
public class IdolItemData : ItemData
{
    [SerializeField] IdolForm associatedForm;
    [SerializeField] Sprite formIcon;
    [SerializeField] Element idolElement;

    public IdolForm GetAssociatedForm()
    {
        return associatedForm;
    }

    public Element GetElement()
    {
        return idolElement;
    }
}
