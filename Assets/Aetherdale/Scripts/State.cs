using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Abstract definition of a character's state. Processed by a StateMachine

public abstract class State
{
    public float updateInterval = 0F;

    public virtual void OnEnter() { }
    public virtual void OnExit() { }
    public virtual void Update() { }

    public abstract bool ReadyForExit();
}