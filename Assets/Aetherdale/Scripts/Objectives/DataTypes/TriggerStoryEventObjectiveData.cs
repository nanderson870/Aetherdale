

using UnityEngine;

[CreateAssetMenu(fileName = "Trigger Event Objective", menuName = "Aetherdale/Objective Data/Trigger Event Objective", order = 0)]
public class TriggerStoryEventObjectiveData : ObjectiveData
{
    public string eventID;

    public override Objective GetInstance()
    {
        return new TriggerStoryEventObjective(this);
    }

}
