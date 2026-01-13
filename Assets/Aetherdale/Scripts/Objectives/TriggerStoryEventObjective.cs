
using UnityEngine;

public class TriggerStoryEventObjective : Objective
{
    string eventID;
    
    public TriggerStoryEventObjective(string description, int repetitionsRequired) : base(description, repetitionsRequired)
    {
    }

    public TriggerStoryEventObjective(TriggerStoryEventObjectiveData data) : base(data)
    {
        this.eventID = data.eventID;
    }

    public override void RegisterCallbacks(Player owningPlayer)
    {
        Debug.Log("Trigger story event start");
        owningPlayer.OnStoryEvent += OnPlayerReceivedStoryEvent;
    }

    public override void UnregisterCallbacks(Player owningPlayer)
    {
        owningPlayer.OnStoryEvent -= OnPlayerReceivedStoryEvent;
    }

    void OnPlayerReceivedStoryEvent(string storyEventID)
    {
        if (storyEventID == eventID)
        {
            ProgressObjective();
        }
    }
}