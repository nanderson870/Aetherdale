using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Levelled Spawn List", menuName = "Aetherdale/Levelled Spawn List", order = 0)]
public class TieredSpawnList : SpawnList
{
    [SerializeField] List<Tier> tiers;
    [SerializeField] List<Tier> bossTiers;

    

    public override Entity GetEntity(int level)
    {
        List<Tier> spawnableTiers = new();
        foreach(Tier tier in tiers)
        {
            if (tier.level <= level)
            {
                spawnableTiers.Add(tier);
            }
        }

        Tier chosenTier = spawnableTiers[UnityEngine.Random.Range(0, spawnableTiers.Count)];

        return Misc.RouletteRandom(
            chosenTier.entries.Where(entry => entry.enabled)
                .Select(entry => new Tuple<float, Entity>(entry.weight, entry.entity)).ToList()
        );
    }

    public override List<SpawnListEntry> GetPossibleEntries(int level)
    {
        List<SpawnListEntry> ret = new();
        foreach (Tier tier in tiers)
        {
            if (tier.level <= level)
            {
                foreach (SpawnListEntry entry in tier.entries.Where(entry => entry.enabled))
                    ret.Add(entry);
            }
        }

        return ret;
    }
    
    public override Boss GetBoss(int level)
    {
        List<Tier> spawnableTiers = new();
        foreach(Tier tier in bossTiers)
        {
            if (tier.level <= level)
            {
                spawnableTiers.Add(tier);
            }
        }

        Tier chosenTier = spawnableTiers[UnityEngine.Random.Range(0, spawnableTiers.Count)];

        return Misc.RouletteRandom(
            chosenTier.entries.Where(entry => entry.enabled)
                .Select(entry => new Tuple<float, Boss>(entry.weight, (Boss) entry.entity)).ToList()
        );
    }

    [System.Serializable]
    public class Tier
    {
        public int level;
        public List<SpawnListEntry> entries;
    }
}
