using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TalkToNPCObjective : Objective
{
    NonPlayerCharacterName speaker;
    DialogueTopic requiredTopic; // topic that must be heard to complete this objective

    public TalkToNPCObjective(TalkToNPCObjectiveData objectiveData) : base(objectiveData)
    {
        speaker = objectiveData.GetSpeakerName();
        requiredTopic = objectiveData.GetRequiredTopic();
    }

    public void ProgressObjective(DialogueAgent agent, Player addressedPlayer, DialogueTopic spokenTopic)
    {
        NonPlayerCharacter npc = agent.GetComponent<NonPlayerCharacter>();
        if (npc == null || npc.GetNpcName() != speaker)
        {
            return;
        }

        if (requiredTopic == spokenTopic)
        {
            // No end topic defined, we progress the objective when the original topic is spoken
            ProgressObjective();
        }
    }

    public NonPlayerCharacterName GetSpeakerName()
    {
        return speaker;
    }

    public DialogueTopic GetTopic()
    {
        return requiredTopic;
    }

    public override void RegisterCallbacks(Player owningPlayer)
    {
        DialogueAgent.OnTopicSpoken += ProgressObjective;
    }
    
    public override void UnregisterCallbacks(Player owningPlayer)
    {
        DialogueAgent.OnTopicSpoken -= ProgressObjective;
    }
}
