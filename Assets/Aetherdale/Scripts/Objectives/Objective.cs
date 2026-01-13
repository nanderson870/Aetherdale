using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

/*
Runtime representation of a quest objective, representing an Objective

This is the one given to a player during a quest - tracks progress towards the objective
*/

public abstract class Objective
{
    public readonly string objectiveDataName = string.Empty;

    // Received from ScriptableObject data
    protected string description;
    protected readonly int repetitionsRequired = 1;
    protected int currentRepetitions = 0;

    // Runtime
    public bool IsTracked {get; private set;} = false; // is this objective currently being tracked?

    public delegate void ObjectiveAction(Objective obj);
    public Action<Objective> OnObjectiveUpdated; // Any kind of update to values, etc
    public Action<Objective> OnObjectiveProgress;
    public Action<Objective> OnObjectiveCompleted;
    public Action<Objective> OnObjectiveStarted;
    public Action<Objective> OnObjectiveFailed;


    public Objective(ObjectiveData objectiveData)
    {
        objectiveDataName = objectiveData.name;
        description = objectiveData.GetDescription();
        repetitionsRequired = objectiveData.GetRepetitionsRequired();
    }

    public Objective(string objectiveDescription, int repetitionsRequired)
    {
        this.description = objectiveDescription;
        this.repetitionsRequired = repetitionsRequired;
    }

    
    // Register callbacks for player-specific objectives.
    public abstract void RegisterCallbacks(Player owningPlayer);
    public abstract void UnregisterCallbacks(Player owningPlayer);


    /* All-purpose progression function */
    public void ProgressObjective()
    {
        if (!IsTracked)
        {
            return;
        }

        currentRepetitions++;

        OnObjectiveProgress?.Invoke(this);

        if (IsObjectiveComplete())
        {
            CompleteObjective();
        }
    }


    /* Start tracking this objective */
    public virtual void StartTracking()
    {
        IsTracked = true;

        // Register player callbacks for client-side objective
        if (NetworkClient.active && Player.GetLocalPlayer().isOwned)
        {
            RegisterCallbacks(Player.GetLocalPlayer());
        }
        
        OnObjectiveStarted?.Invoke(this);
    }

    /* Function to mark this objective as complete. Will be called automatically in many circumstances, but can be manually called as well */
    public virtual void CompleteObjective()
    {
        IsTracked = false;

        // Unregister player callbacks for client-side objective
        if (NetworkClient.active && Player.GetLocalPlayer().isOwned)
        {
            UnregisterCallbacks(Player.GetLocalPlayer());
        }

        OnObjectiveCompleted?.Invoke(this);
    }

    public virtual bool IsObjectiveComplete()
    {
        return currentRepetitions >= repetitionsRequired;
    }

    public virtual void FailObjective()
    {
        OnObjectiveFailed?.Invoke(this);
    }

    public virtual string GetDescription()
    {
        if (repetitionsRequired > 1)
        {
            return description + " (" + currentRepetitions + "/" + repetitionsRequired + ")";
        }
        
        return description;
    }

    public void SetCurrentRepetitions(int value)
    {
        currentRepetitions = value;
    }

    public int GetCurrentRepetitions()
    {
        return currentRepetitions;
    }

    public int GetRepetitionsRequired()
    {
        return repetitionsRequired;
    }
}
