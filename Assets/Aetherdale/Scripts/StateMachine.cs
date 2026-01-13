using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine
{
#pragma warning disable CS0414 // Remove unread private members
    [SerializeField] string currentStateName;
#pragma warning restore CS0414 // Remove unread private members

    private State currentState;

    float lastStateUpdate = 0;

    public void ChangeState(State newState)
    {
        currentState?.OnExit();

        currentState = newState;
        currentStateName = nameof(currentState);

        currentState?.OnEnter();
    }

    public void Update()
    {
        if (currentState.updateInterval > 0 && (Time.time - lastStateUpdate) < currentState.updateInterval)
        {
            // There is an update interval for this state and we aren't ready to update yet
            return;
        }

        lastStateUpdate = Time.time;
        currentState?.Update();
    }

    public bool HasState()
    {
        return currentState != null;
    }

    public State GetState()
    {
        return currentState;
    }
}
