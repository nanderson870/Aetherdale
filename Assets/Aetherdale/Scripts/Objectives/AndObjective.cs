using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Logical AND between multiple objectives
/// </summary>
public class AndObjective : Objective
{
    public readonly List<Objective> objectives;

    public AndObjective(AndObjectiveData objectiveData) : base(objectiveData)
    {
        objectives = objectiveData.GetObjectives();

        foreach (Objective obj in objectives)
        {
            obj.OnObjectiveCompleted += ProgressObjective;
        }
    }

    public override bool IsObjectiveComplete()
    {
        foreach (Objective obj in objectives)
        {
            if (!obj.IsObjectiveComplete())
            {
                return false;
            }
        }

        return true;
    }

    public override string GetDescription()
    {
        string ret = "";
        foreach (Objective obj in objectives)
        {
            ret += obj.GetDescription() + "\n";
        }

        return ret;
    }

    public void ProgressObjective(Objective obj)
    {
        if (IsObjectiveComplete())
        {
            CompleteObjective();
        }
    }

    public override void RegisterCallbacks(Player owningPlayer)
    {
    }

    public override void UnregisterCallbacks(Player owningPlayer)
    {
    }
}