using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillEntitiesObjective : Objective
{
    readonly EntityType typeRequired = EntityType.None;

    public KillEntitiesObjective(KillEntitiesObjectiveData objectiveData) : base(objectiveData)
    {
        typeRequired = objectiveData.GetTypeRequired();
    }

    public KillEntitiesObjective(string objectiveDescription, int repetitionsRequired, EntityType typeRequired) : base(objectiveDescription, repetitionsRequired)
    {
        this.typeRequired = typeRequired;
    }

    public void ProgressObjective(Entity entity)
    {
        if (typeRequired == EntityType.None || entity.IsEntityType(typeRequired))
        {
            ProgressObjective();
        }
    }

    public override void RegisterCallbacks(Player owningPlayer)
    {
    }
    
    public override void UnregisterCallbacks(Player owningPlayer)
    {
    }
}
