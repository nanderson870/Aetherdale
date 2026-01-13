using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;

public class LootItem : Pickup, IInteractable
{   
    public ItemData data;
    [SerializeField] int quantity = 1;


    public override void Start()
    {
        if (data == null)
        {
            Debug.LogError("Loot item " + name + " has no ItemData attached to it");
        }

        base.Start();
    }

    public int GetQuantity()
    {
        return quantity;
    }

    [Server]
    public void SetQuantity(int quantity)
    {
        this.quantity = quantity;
    }

    public bool IsInteractable(ControlledEntity interactingEntity)
    {
        return true;
    }

    public string GetInteractionPromptText(ControlledEntity interactingEntity)
    {
        return $"Pick up {data.GetName()}";
    }

    public string GetTooltipTitle(ControlledEntity interactingEntity)
    {
        return data.GetName();
    }

    public string GetTooltipText(ControlledEntity interactingEntity)
    {
        return data.GetDescription();
    }

    protected override void OnPickup(Entity entity)
    {
        entity.GetOwningPlayer().PickupItem(new Item(data, quantity));
    }

    [ClientRpc]
    public void RpcSetVelocity(Vector3 velocity)
    {
        gameObject.GetComponent<Rigidbody>().linearVelocity = velocity;
    } 
    
    [ClientRpc]
    public void RpcSetQuantity(int quantity)
    {
        this.quantity = quantity;
    }


    /// <summary>
    /// Takes ItemData and quantity, splits the drops into its appropriate denominations, and drops them within a random radius of target position
    /// </summary>
    /// <param name="item"></param>
    /// <param name="quantity"></param>
    /// <param name="position"></param>
    /// <param name="radius"></param>
    /// <param name="initialVelocity"></param>
    public static List<LootItem> DropLootItems(ItemData item, int quantity, Vector3 position, float radius = 3, Vector3 initialVelocity = new())
    {
        List<LootItem> dropped = new();

        int quantityLeftToDrop = quantity;

        List<LootItem> denominations = item.GetLootItemDenominations();

        denominations.Sort(delegate (LootItem i1, LootItem i2)
        {
            return i2.GetQuantity().CompareTo(i1.GetQuantity());
        });


        int prevQuantityLeft = quantityLeftToDrop;
        foreach (LootItem denomination in denominations)
        {
            while (quantityLeftToDrop >= denomination.GetQuantity())
            {
                Vector2 horizOffset = UnityEngine.Random.insideUnitCircle * radius;
                Vector3 randomizedPos = position + new Vector3(horizOffset.x, 0, horizOffset.y);
                LootItem lootItem = (LootItem)denomination.Drop(randomizedPos, velocity: initialVelocity);
                dropped.Add(lootItem);

                quantityLeftToDrop -= denomination.GetQuantity();

                if (quantityLeftToDrop == prevQuantityLeft)
                {
                    throw new System.Exception("ERROR - Loot item denomination did not decrease quantity left!");
                }
            }
        }

        return dropped;
    }
}
