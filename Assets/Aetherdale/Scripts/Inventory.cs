using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using System.Linq;

// Tracks temporary items - limited to the scope of a sequence

[Serializable]
public class Inventory : NetworkBehaviour
{
    [SerializeField] [SyncVar(hook=nameof(OnGoldCountChanged))] int gold = 0;
    public static Action<int, int> OnLocalPlayerGoldCountChanged;

    public ConsumableSlot OffensiveConsumableSlot {get; private set;}
    public ConsumableSlot DefensiveConsumableSlot {get; private set;}
    public ConsumableSlot UtilityConsumableSlot {get; private set;}

    void Start()
    {
        ConsumableSlot[] consumableSlots = GetComponents<ConsumableSlot>();

        if (consumableSlots.Length != 3)
        {
            throw new Exception("Inventory object has invalid number of consumable slots attached to it! 3 ConsumableSlots should be attached to the gameobject");
        }

        OffensiveConsumableSlot = consumableSlots[0];
        DefensiveConsumableSlot = consumableSlots[1];
        UtilityConsumableSlot = consumableSlots[2];
    }

    public ConsumableSlot GetConsumableSlot(Consumable.ConsumableSlotType slotType)
    {
        switch (slotType)
        {
            case Consumable.ConsumableSlotType.Offensive:
                return OffensiveConsumableSlot;

            case Consumable.ConsumableSlotType.Defensive:
                return DefensiveConsumableSlot;

            case Consumable.ConsumableSlotType.Utility:
                return UtilityConsumableSlot;
        }

        return null;
    }

    [Server]
    public void AddGold(int gold)
    {
        this.gold = this.gold + gold;
    }

    [Server]
    public void RemoveGold(int gold)
    {
        this.gold = this.gold - gold;

        if (this.gold < 0)
        {
            this.gold = 0;
        }
    }

    public int GetGold()
    {
        return gold;
    }

    /// <summary>
    /// SyncVar Hook
    /// </summary>
    /// <param name="previousCount"></param>
    /// <param name="currentCount"></param>
    void OnGoldCountChanged(int previousCount, int currentCount)
    {
        OnLocalPlayerGoldCountChanged?.Invoke(previousCount, currentCount);
    }
    

    [Server]
    public void Reset()
    {
        gold = 0;

        OffensiveConsumableSlot.Clear();
        DefensiveConsumableSlot.Clear();
        UtilityConsumableSlot.Clear();
    }

}
