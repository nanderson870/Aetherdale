

using System.Collections;
using UnityEngine;

public class ConsumableWidget : CooldownWidget
{
    [SerializeField] Consumable.ConsumableSlotType slotType;
    Entity trackedEntity;


    void Start()
    {
        StartCoroutine(WaitAndInitialize());
    }

    void OnDestroy()
    {
        if (Player.GetLocalPlayer() != null)
        {
            UnregisterPlayerCallbacks(Player.GetLocalPlayer().GetInventory(), Player.GetLocalPlayer());
        }
    }

    void SetTrackedEntity(Entity entity)
    {
        trackedEntity = entity;
        if (trackedEntity != null) SetAvailable(trackedEntity is PlayerWraith);
    }


    IEnumerator WaitAndInitialize()
    {
        while (Player.GetLocalPlayer() == null)
        {
            yield return null;
        }

        Player localPlayer = Player.GetLocalPlayer();
        Inventory inventory = localPlayer.GetInventory();
        RegisterPlayerCallbacks(inventory, localPlayer);

        SetTrackedEntity(localPlayer.GetControlledEntity());
        localPlayer.OnEntityChangedOnClient += SetTrackedEntity;

        SetConsumable(inventory.GetConsumableSlot(slotType).GetConsumable());


        if (trackedEntity != null) SetAvailable(trackedEntity is PlayerWraith);
    }

    void RegisterPlayerCallbacks(Inventory inventory, Player player)
    {
        ConsumableSlot inventorySlot = inventory.GetConsumableSlot(slotType);

        inventorySlot.OnConsumableChanged += ConsumableIdChanged;
        inventorySlot.OnQuantityChanged += ConsumableQuantityChanged;
    }


    void UnregisterPlayerCallbacks(Inventory inventory, Player player)
    {
        ConsumableSlot inventorySlot = inventory.GetConsumableSlot(slotType);

        inventorySlot.OnConsumableChanged += ConsumableIdChanged;
        inventorySlot.OnQuantityChanged += ConsumableQuantityChanged;
    }

    
    public void ConsumableIdChanged(string id) {SetConsumable(Player.GetLocalPlayer().GetInventory().GetConsumableSlot(slotType).GetConsumable());}
    public void SetConsumable(Consumable newConsumable)
    {
        if (newConsumable == null)
        {
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
            SetIcon(newConsumable.GetIcon());
            SetInfo(newConsumable.GetName(), newConsumable.GetStatsDescription(), newConsumable.GetDescription());
        }

        if (trackedEntity != null) SetAvailable(trackedEntity is PlayerWraith);
    }
    
    public void ConsumableQuantityChanged(int newQuantity)
    {
        SetQuantity(newQuantity);

        if (trackedEntity != null) SetAvailable(trackedEntity is PlayerWraith);
    }
}