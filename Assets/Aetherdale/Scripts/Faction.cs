using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*

Represents an in-game faction, used to help define combat behaviors

Each faction may have allies, and enemies. Entities in a faction should be supportive
to entities in their own and ally factions, and hostile towards those in enemy factions

Note that these are not necessarily mutual - just because faction A lists B as an ally/enemy,
does not necessitate that faction B lists faction A in any particular way

*/

[System.Serializable]
[CreateAssetMenu(fileName = "Faction", menuName = "Aetherdale/Faction", order = 0)]
public class Faction : ScriptableObject
{
    public enum FactionDefaultBehavior
    {
        Friendly, // help other entities on sight if possible
        Neutral, // no default response to other entities
        Hostile // attack other entities on sight if possible
    };

    [SerializeField] string factionName;
    [SerializeField] bool alliedWithSelf = true;

    [SerializeField] FactionDefaultBehavior defaultBehavior = FactionDefaultBehavior.Neutral;

    [SerializeField] List<Faction> allyFactions = new List<Faction>();
    [SerializeField] List<Faction> enemyFactions = new List<Faction>();

    public string GetName()
    {
        return factionName;
    }

    public bool IsAlliesWithFaction(Faction other)
    {
        if (other == this)
        {
            return alliedWithSelf;
        }

        if (defaultBehavior == FactionDefaultBehavior.Friendly)
        {
            return !enemyFactions.Contains(other);
        }

        return allyFactions.Contains(other);
    }

    // Whether this faction considers other to be an enemy
    public bool IsEnemiesWithFaction(Faction other)
    {
        if (other == this)
        {
            return !alliedWithSelf;
        }

        if (defaultBehavior == FactionDefaultBehavior.Hostile || (other != null && other.defaultBehavior == FactionDefaultBehavior.Hostile))
        {
            return !allyFactions.Contains(other);
        }

        return enemyFactions.Contains(other);
    }

    public bool IsHostileFaction()
    {
        return defaultBehavior == FactionDefaultBehavior.Hostile;
    }
}
