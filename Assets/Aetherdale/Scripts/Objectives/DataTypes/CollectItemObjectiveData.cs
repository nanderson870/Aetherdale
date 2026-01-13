using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Collect Item Objective", menuName = "Aetherdale/Objective Data/Collect Item", order = 0)]
public class CollectItemObjectiveData : ObjectiveData
{
    [Header("Required Data")]
    [SerializeField] ItemData requiredItem;

    public override Objective GetInstance()
    {
        return new CollectItemObjective(this);
    }

    public ItemData GetRequiredItem()
    {
        return requiredItem;
    }

}
