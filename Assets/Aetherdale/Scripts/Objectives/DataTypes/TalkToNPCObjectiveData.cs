using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Talk To NPC Objective", menuName = "Aetherdale/Objective Data/Talk To NPC", order = 0)]
public class TalkToNPCObjectiveData : ObjectiveData
{
    [SerializeField] NonPlayerCharacterName target;
    [SerializeField] DialogueTopic topic;

    public override Objective GetInstance()
    {
        return new TalkToNPCObjective(this);
    }

    public NonPlayerCharacterName GetSpeakerName()
    {
        return target;
    }

    public DialogueTopic GetRequiredTopic()
    {
        return topic;
    }
}
