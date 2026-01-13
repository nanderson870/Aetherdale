
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Simple Spawn List", menuName = "Aetherdale/Simple Spawn List", order = 0)]
public class SimpleSpawnList : SpawnList
{
    [SerializeField] List<SpawnListEntry> entries;
    public Boss[] bosses;

    public override Entity GetEntity(int level)
    {
        float totalWeight = 0;
        foreach (SpawnListEntry entry in entries)
        {
            if (entry.enabled)
            {
                totalWeight += entry.weight;
            }
        }

        float rolledWeight = Random.Range(0, totalWeight);
        float runningTotal = 0;
        foreach (SpawnListEntry entry in entries)
        {
            if (entry.enabled)
            {
                runningTotal += entry.weight;
                if (rolledWeight <= runningTotal)
                {
                    return entry.entity;
                }
            }
        }

        return null;
    }

    public override Boss GetBoss(int level)
    {
        return bosses[Random.Range(0, bosses.Length)];
    }

    public override List<SpawnListEntry> GetPossibleEntries(int level)
    {
        List<SpawnListEntry> ret = new();
        foreach (SpawnListEntry entry in entries.Where(entry => entry.enabled))
            ret.Add(entry);

        return ret;
    }
}