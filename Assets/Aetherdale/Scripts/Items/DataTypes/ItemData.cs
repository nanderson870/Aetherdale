using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using Mirror;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using System.Linq;

[CreateAssetMenu(fileName = "Item", menuName = "Aetherdale/Item Data/Item", order = 0)]
public class ItemData : ScriptableObject
{
    [SerializeField] string itemId;

    [SerializeField] protected string itemName;
    [SerializeField] protected Rarity rarity;
    [SerializeField] protected int value;
    [SerializeField] protected bool unique;
    [SerializeField] protected bool questItem;
    [SerializeField] protected LootItem lootEquivalent;
    [SerializeField] protected List<LootItem> largerLootDenominations = new();
    [SerializeField] protected Sprite icon;
    
    
    [TextArea(10,5)]
    [SerializeField][FormerlySerializedAs("itemDescription")] private string statsDescription = "";

    [TextArea(10,5)]
    [SerializeField] private string itemDescription = "";

    [TextArea(10,5)]
    [SerializeField] string unlockHint = "You do not know where to find this item yet.";

    public virtual void OnValidate()
    {
        #if UNITY_EDITOR
            if (itemId == "")
            {
                itemId = GUID.Generate().ToString();
            }
            
        #endif
    }

    public string GetItemID()
    {
        return itemId;
    }

    public string GetName()
    {
        return itemName;
    }

    public Rarity GetRarity()
    {
        return rarity;
    }

    public int GetValue()
    {
        return value;
    }

    public virtual GameObject GetMesh()
    {
        return null;
    }

    /// <summary>
    /// </summary>
    /// <returns>The default LootItem prefab corresponding to this item</returns>
    public LootItem GetLootItem()
    {
        return lootEquivalent;
    }

    /// <summary>
    /// CREATES/instantiates AND SPAWNS the default LootItem prefab and returns an instance
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public virtual Pickup CreatePickup(Vector3 position, Quaternion rotation)
    {
        Pickup inst = Instantiate(GetLootItem(), position, rotation);
        NetworkServer.Spawn(inst.gameObject);
        return inst;
    }

    public GameObject GetLootEquivalentGameObject()
    {
        return lootEquivalent.gameObject;
    }

    public bool IsUnique()
    {
        return unique;
    }

    public bool IsQuestItem()
    {
        return questItem;
    }

    public virtual Sprite GetIcon()
    {
        if (icon == null)
        {
            return AetherdaleData.GetAetherdaleData().defaultItemIcon;
        }

        return icon;
    }

    public string GetDescription()
    {
        return itemDescription;
    }

    public virtual string GetStatsDescription(Player targetPlayer = null)
    {
        return statsDescription;
    }

    public string GetUnlockHint()
    {
        return unlockHint;
    }

    [Server]
    public LootItem Drop(Vector3 position, Vector3 velocity = new(), List<Condition> requirements = null, bool shared = false)
    {
        LootItem lootItemToDrop = lootEquivalent;

        LootItem droppedItem;
        if (this == AetherdaleData.GetAetherdaleData().goldCoinsItem)
        {
            // Draw from spawn pool for coins
            droppedItem = ItemSpawnPooler.singleton.GetGold(position, lootEquivalent.transform.rotation).GetComponent<LootItem>();
        }
        else
        {
            droppedItem = Instantiate(lootItemToDrop, position, lootEquivalent.transform.rotation);
        }
        
        droppedItem.SetQuantity(1);
        droppedItem.gameObject.GetComponent<Rigidbody>().linearVelocity = velocity;
        NetworkServer.Spawn(droppedItem.gameObject);
        
        if (requirements != null)
        {
            droppedItem.SetVisibilityConditions(requirements);
        }

        droppedItem.SetShared(shared);

        return droppedItem;
    }

    public LootItem GetLargestDenominationForQuantity(int quantity)
    {
        LootItem largestDenomination = lootEquivalent;
        foreach (LootItem item in largerLootDenominations)
        {
            if (item.GetQuantity() <= quantity && item.GetQuantity() > largestDenomination.GetQuantity())
            {
                largestDenomination = item;
            }
        }

        return largestDenomination;
    }

    public List<LootItem> GetLootItemDenominations()
    {
        List<LootItem> ret = new();
        ret.Add(lootEquivalent);

        foreach (LootItem item in largerLootDenominations)
        {
            ret.Add(item);
        }
    
        return ret;
    }
}


