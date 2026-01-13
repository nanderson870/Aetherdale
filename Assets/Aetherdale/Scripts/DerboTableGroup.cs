using System;
using System.Collections.Generic;
using Aetherdale;
using Mirror;
using UnityEngine;


/// <summary>
/// A group of Derbo Tables, ensures coherent/nonconflicting results for each table
/// </summary>
public class DerboTableGroup : MonoBehaviour
{
    [Header("Tables")]
    [SerializeField][Range(3, 10)] int minTables = 4;
    [SerializeField][Range(6, 20)] int maxTables = 6;


    int offeringsCreated = 0;

    void Start()
    {
        if (NetworkServer.active)
        {
            SetupChildTables();
        }    
    }

    [Server]
    IShopOffering CreateOffering()
    {
        offeringsCreated++;

        // First three are constant - weapon, muffin, bomb
        if (offeringsCreated == 1)
        {
            return ShopOffering.CreateWeaponOffering(AreaSequencer.GetAreaSequencer().GetAreaLevel());
        }
        else if (offeringsCreated == 2)
        {
            return new BrindleberryMuffin();
        }
        else if (offeringsCreated == 3)
        {
            return new BlazeBomb();
        }

        // Give traits after that
        else
        {
            return ShopOffering.CreateTraitOffering(AreaSequencer.GetAreaSequencer().GetAreaLevel());
        }

    }

    [Server]
    void SetupChildTables()
    {
        List<Tuple<DerboTable, StatefullyActiveNetworkBehaviour>> potentialTables = new();
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent(out DerboTable derboTable) && child.TryGetComponent(out StatefullyActiveNetworkBehaviour sanb))
            {
                potentialTables.Add(new (derboTable, sanb));
            }
        }

        int numTables = UnityEngine.Random.Range(minTables, maxTables + 1);
        List<Tuple<DerboTable, StatefullyActiveNetworkBehaviour>> toggledTables = new();
        for (int i = 0; i < numTables && potentialTables.Count != 0; i++)
        {
            Tuple<DerboTable, StatefullyActiveNetworkBehaviour> tableTuple = potentialTables[UnityEngine.Random.Range(0, potentialTables.Count)];
            potentialTables.Remove(tableTuple);

            tableTuple.Item2.OrderState(StatefullyActiveNetworkBehaviour.ActiveState.OrderedInactive);
            toggledTables.Add(tableTuple);
        }

        // Initialize selected tables
        foreach (Tuple<DerboTable, StatefullyActiveNetworkBehaviour> toggledTableTuple in toggledTables)
        {
            toggledTableTuple.Item1.SetOffering(CreateOffering());
            toggledTableTuple.Item2.OrderState(StatefullyActiveNetworkBehaviour.ActiveState.OrderedActive);
        }
    }
}
