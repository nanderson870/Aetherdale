

using System;
using System.Collections;
using UnityEngine;

public abstract class TutorialSequence
{
    bool started = false;

    void StartMe()
    {
        if (!started)
        {
            TutorialManager.StartSequence(this);
        }
    }

    public bool Completed()
    {
        return Player.GetLocalPlayer().GetPlayerData().CompletedTutorialNames.Contains(this.GetType().Name);
    }


    // Override this to run the tutorial. It's an IEnumerator for ease of realtime tracking
    // Then, at some point call CompleteTutorial() to succeed the tutorial, or CancelTutorial() to end it prematurely
    public abstract IEnumerator RunTutorial();

    public void SetStartAction(ref Action action)
    {
        Debug.Log("Setting start action for " + this);
        action += StartMe;
    }

    protected void CompleteTutorial()
    {
        Player.GetLocalPlayer().GetPlayerData().CompletedTutorial(this.GetType().Name);
    }
    
    protected void CancelTutorial()
    {
        
    }
}