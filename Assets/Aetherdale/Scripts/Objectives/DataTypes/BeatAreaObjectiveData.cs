using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Beat Area Objective", menuName = "Aetherdale/Objective Data/Beat Area", order = 0)]
public class BeatAreaObjectiveData : ObjectiveData
{
    [field:SerializeField] public Area AreaToBeat {get; private set;}

    public override Objective GetInstance()
    {
        return new BeatAreaObjective(this);
    }
}
