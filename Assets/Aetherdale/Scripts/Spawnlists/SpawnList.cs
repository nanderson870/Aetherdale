using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class SpawnList : ScriptableObject
{
    public delegate int SpawnListLevelMechanism();
    public abstract Entity GetEntity(int level);
    public abstract Boss GetBoss(int level);

    public abstract List<SpawnListEntry> GetPossibleEntries(int level);

    public static Entity GetEntityFromSpawnLists(List<Tuple<SpawnList, SpawnListLevelMechanism>> spawnLists)
    {
        List<SpawnListEntry> possibleEntries = new();
        foreach (Tuple<SpawnList, SpawnListLevelMechanism> sl in spawnLists)
        {
            int input = 0;
            if (sl.Item2 != null)
            {
                input = sl.Item2.Invoke();
            }

            foreach (SpawnListEntry sle in sl.Item1.GetPossibleEntries(input))
            {
                possibleEntries.Add(sle);
            }
        }

        return Misc.RouletteRandom(
            possibleEntries.Select(entry => new Tuple<float, Entity>(entry.weight, entry.entity)).ToList()
        );
    }
}


[System.Serializable]
public class SpawnListEntry
{
    public float weight = 1.0F;
    public Entity entity;
    public bool enabled = true;
}