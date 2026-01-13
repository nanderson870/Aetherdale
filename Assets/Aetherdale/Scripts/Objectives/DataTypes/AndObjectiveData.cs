using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AND", menuName = "Aetherdale/Objective Data/Logic/AND", order = 0)]
public class AndObjectiveData : ObjectiveData
{
    [Header("Required Data")]
    [SerializeField] List<ObjectiveData> objectives = new();

    public override Objective GetInstance()
    {
        return new AndObjective(this);
    }

    public List<Objective> GetObjectives()
    {
        List<Objective> ret = new();
        foreach (ObjectiveData objData in objectives)
        {
            ret.Add(objData.GetInstance());
        }

        return ret;
    }

}
