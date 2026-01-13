using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectItemObjective : Objective
{
    ItemData requiredItem;
    int quantityRequired;

    public CollectItemObjective(CollectItemObjectiveData objectiveData) : base(objectiveData)
    {
        requiredItem = objectiveData.GetRequiredItem();
        quantityRequired = objectiveData.GetRepetitionsRequired();
    }

    public void ProgressObjective(Item item)
    {
        if (item.GetItemID() == requiredItem.GetItemID())
        {
            for (int i = 0; i < item.GetQuantity() && IsTracked; i++)
            {
                ProgressObjective();
            }
        }
    }

    public override void RegisterCallbacks(Player owningPlayer)
    {
        owningPlayer.OnAcquireItem += ProgressObjective;
    }

    public override void UnregisterCallbacks(Player owningPlayer)
    {
        owningPlayer.OnAcquireItem -= ProgressObjective;
    }
}
