using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[CreateAssetMenu(fileName = "Drop Table", menuName = "Aetherdale/Drop Table", order = 0)]
public class DropTable : ScriptableObject
{
    /// <summary>Primary drop list</summary>
    [SerializeField] List<DropTableEntry> drops;

    /// <summary>Use for quest objective drops, and anything else that shouldn't count as consuming a roll</summary>
    [SerializeField] List<DropTableEntry> additionalDrops;



    /// <summary>
    /// Rolls drops from this table
    /// </summary>
    /// <param name="number">Number of different drops</param>
    /// <returns></returns>
    [Server]
    public List<DropInstance> GetDrops(int number, float quantityMultiplier = 1.0F)
    {
        List<DropInstance> rolledDrops = new();

        // Roll primary drops
        for (int i = 0; i < number; i++)
        {
            // Calculate the total probability, then take a random number between 1 and that
            int totalProbability = 0;
            foreach (DropTableEntry entry in drops)
            {
                totalProbability += entry.probability;
            }

            int rolledProbability = UnityEngine.Random.Range(0, totalProbability) + 1;


            // Iterate through drops taking a running total of their probabilities
            int runningTotal = 0;
            foreach (DropTableEntry entry in drops)
            {
                runningTotal += entry.probability;

                if (runningTotal > rolledProbability)
                {
                    if (entry.item == null)
                    {
                        continue;
                    }

                    int scaledMinQuantity = entry.minQuantity;
                    int scaledMaxQuantity = entry.maxQuantity;
                    if (!entry.item.IsUnique()) // Don't scale unique items
                    {
                        scaledMinQuantity = (int) (entry.minQuantity * quantityMultiplier);
                        scaledMaxQuantity = (int) (entry.maxQuantity * quantityMultiplier);
                    }

                    // Stop when our random number is <= running total
                    rolledDrops.Add(new DropInstance()
                    {
                        item = entry.item,
                        quantity = UnityEngine.Random.Range(scaledMinQuantity, scaledMaxQuantity),
                        requirements = entry.requirements
                    });
                    break;
                }
            }  
        }

        // Roll additional drops
        foreach (DropTableEntry additionalEntry in additionalDrops)
        {
            if (UnityEngine.Random.Range(0, 100) < additionalEntry.probability)
            {
                int scaledMinQuantity = additionalEntry.minQuantity;
                int scaledMaxQuantity = additionalEntry.maxQuantity;
                if (!additionalEntry.item.IsUnique()) // Don't scale unique items
                { 
                    scaledMinQuantity = (int) (additionalEntry.minQuantity * quantityMultiplier);
                    scaledMaxQuantity = (int) (additionalEntry.maxQuantity * quantityMultiplier);
                }

                rolledDrops.Add(new()
                {
                    item = additionalEntry.item,
                    quantity = UnityEngine.Random.Range(scaledMinQuantity, scaledMaxQuantity),
                    requirements = additionalEntry.requirements
                });
            }
        }

        return rolledDrops;
    }


    // TODO move into entity
    /// <summary>
    /// Rolls and drops drops from this table
    /// </summary>
    /// <param name="number">Number of actual drops, NOT including "additional" drops</param>
    /// <returns>A list of drops rolled</returns>
    [Server]
    public void DropLoot(List<DropInstance> drops, Vector3 where)
    {
        foreach (DropInstance drop in drops)
        {
            foreach (LootItem droppedItem in LootItem.DropLootItems(drop.item, drop.quantity, where))
            {
                drop.OnDropped?.Invoke(droppedItem);
            }
        }
    }
}

// The editor data for an item that can be dropped
[System.Serializable]
public class DropTableEntry
{
    public ItemData item;
    public int minQuantity = 1;
    public int maxQuantity = 1;
    public int probability;

    public List<Condition> requirements;
}

// An instance of an item dropped
public class DropInstance
{
    public ItemData item;
    public int quantity = 1;
    public List<Condition> requirements;
    public Action<LootItem> OnDropped;
}
