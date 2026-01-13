using System;
using Mirror;
using UnityEngine;

public class ConsumableSlot : NetworkBehaviour
{
    
    Consumable consumable;
    [SyncVar(hook = nameof(QuantityChanged))] int count = 0;
    [SyncVar (hook = nameof(ConsumableChanged))] string consumableType = "";
    public Action<string> OnConsumableChanged;
    public Action<int> OnQuantityChanged;

    public Consumable.ConsumableSlotType slotType;

    public void Clear()
    {
        consumable = null;
        count = 0;
        consumableType = "";
    }
    
    public void SetConsumable(Consumable newConsumable)
    {
        if (newConsumable == null)
        {
            Debug.Log("Null consumable");
            consumable = null;
            count = 0;
            consumableType = "";
            return;
        }

        if (consumable != null && consumable.GetType() == newConsumable.GetType())
        {
            if (count < newConsumable.GetMaxCount())
            {
                // Adding count of our current consumable
                count = count + 1;
            }
        }
        else
        {
            // Switching/new consumable, reset count
            count = 1;

            newConsumable.OnConsumed += DecrementConsumableCount;
            consumable = newConsumable;

            // TODO drop old one on ground if not null
        }

        consumableType = newConsumable.GetType().ToString();
    }
    
    public Consumable GetConsumable()
    {
        if (consumable == null && consumableType != "")
        {
            return (Consumable) Activator.CreateInstance(Type.GetType(consumableType));
        }
        return consumable;
    }

    public int GetConsumableCount()
    {
        return count;
    }

    void DecrementConsumableCount()
    {
        count = count - 1;
        if (count <= 0)
        {
            Clear();
        }
    }

    void ConsumableChanged(string oldConsumable, string newConsumable)
    {
        OnConsumableChanged?.Invoke(newConsumable);
    }

    void QuantityChanged(int oldQuantity, int newQuantity)
    {
        OnQuantityChanged?.Invoke(newQuantity);
    }
}