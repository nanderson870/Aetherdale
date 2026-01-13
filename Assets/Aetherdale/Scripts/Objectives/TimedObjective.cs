using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// An Objective that succeeds or fails based on a timer. Determine success/fail conditions
/// by registering the appropriate callbacks
/// 
/// NOTE: You MUST call Evaluate() on this objective on a regular basis, from whatever is using it.
/// The objective can only complete when Evaluated
/// </summary>

public class TimedObjective : Objective
{
    readonly int timeInSeconds;

    float startTime;


    public TimedObjective(string objectiveDescription, int repetitionsRequired, int seconds) : base(objectiveDescription, repetitionsRequired)
    {
        timeInSeconds = seconds;
    }

    public override void StartTracking()
    {
        base.StartTracking();
        startTime = Time.time;
    }

    public override void FailObjective()
    {
        base.FailObjective();
    }

    public override string GetDescription()
    {
        return description + "(" + (int) GetTimeLeft() + "s)";
    }

    public float GetTimeLeft()
    {
        float elapsed = Time.time - startTime;
        return timeInSeconds - elapsed;
    }

    public void Evaluate()
    {
        if (GetTimeLeft() <= 0 && IsTracked)
        {
            ProgressObjective();
            startTime = Time.time;
        }
    }

    public override void RegisterCallbacks(Player owningPlayer)
    {
    }
    
    public override void UnregisterCallbacks(Player owningPlayer)
    {
    }
}
