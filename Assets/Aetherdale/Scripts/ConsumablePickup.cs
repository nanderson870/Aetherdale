
using System;
using UnityEngine;

public class ConsumablePickup : Pickup, IInteractable
{
    [SerializeField] string pickupName;
    
    [TextArea(10,5)]
    [SerializeField] string tooltip;

    [SerializeField] ConsumableType consumableType;

    public string GetInteractionPromptText(ControlledEntity interactingEntity)
    {
        return $"Pick up {pickupName}";
    }

    public string GetTooltipText(ControlledEntity interactingEntity)
    {
        return $"{tooltip}";
    }

    public string GetTooltipTitle(ControlledEntity interactingEntity)
    {
        return $"{pickupName}";
    }

    public bool IsInteractable(ControlledEntity interactingEntity)
    {
        return true;
    }


    protected override void OnPickup(Entity entity)
    {
        switch (consumableType)
        {
            case ConsumableType.BrindleberryMuffin:
                entity.GetOwningPlayer().GetInventory().DefensiveConsumableSlot.SetConsumable(new BrindleberryMuffin());
                break;
            
            case ConsumableType.BlazeBomb:
                entity.GetOwningPlayer().GetInventory().OffensiveConsumableSlot.SetConsumable(new BlazeBomb());
                break;
                
            case ConsumableType.None:
                throw new ArgumentException("Invalid consumable type");       
        }
    }
}

public enum ConsumableType
{
    None = 0,
    BrindleberryMuffin = 1,
    BlazeBomb = 2
}