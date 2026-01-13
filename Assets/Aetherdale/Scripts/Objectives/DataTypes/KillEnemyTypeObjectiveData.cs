using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Kill Enemy Type Objective", menuName = "Aetherdale/Objective Data/Kill Enemy Type", order = 0)]
public class KillEntitiesObjectiveData : ObjectiveData
{
    [SerializeField] EntityType typeRequired;

    public override Objective GetInstance()
    {
        return new KillEntitiesObjective(this);
    }

    public EntityType GetTypeRequired()
    {
        return typeRequired;
    }
}
