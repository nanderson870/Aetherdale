using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TextCore;

/*
A quest - a story-based task the player may complete for rewards

This is the server data class for a quest. It should not be referenced
by the client, and eventually may be replaced by an API call interface
*/

[System.Serializable]
[CreateAssetMenu(fileName = "Quest", menuName = "Aetherdale/Quest Data", order = 0)]
public class QuestData : ScriptableObject
{
    // Per-quest
    [SerializeField] string questId;
    [SerializeField] string questName; // name of the quest
    //[SerializeField] bool givenAtGameStart = false;

    [TextArea(15,20)]
    [SerializeField] string description;

    [SerializeField] List<QuestStageData> stages; // Objectives filled out in editor. DO NOT MODIFY OUTSIDE OF EDITOR
    [SerializeField] List<HeldItem> rewardItems;
    
    public UnityAction<Quest> OnQuestCompleted;

    public virtual void OnValidate()
    {
        #if UNITY_EDITOR
            if (questId == "")
            {
                questId = GUID.Generate().ToString();
                EditorUtility.SetDirty(this);
            }
            
            // // Sort by stage number
            // stages.Sort(delegate(QuestStageData stage1, QuestStageData stage2)
            // {
            //     return stage1.stageNumber.CompareTo(stage2.stageNumber);
            // });

        #endif
    }

    public string GetID()
    {
        return questId;
    }

    public string GetName()
    {
        return questName;
    }

    public string GetDescription()
    {
        return description;
    }

    public List<QuestStageData> GetStages()
    {
        return stages;
    }

    public List<HeldItem> GetRewards()
    {
        return rewardItems;
    }
}

/// <summary>
/// A dialogue topic to be given to a certain NPC, on completion of a quest
/// </summary>
[System.Serializable]
public class UnlockedTopic
{
    [SerializeField] NonPlayerCharacterName npcName;
    [SerializeField] DialogueTopic givenTopic;

    public NonPlayerCharacterName GetNpcName()
    {
        return npcName;
    }

    public DialogueTopic GetGivenTopic()
    {
        return givenTopic;
    }

}


[System.Serializable]
public class QuestStageData
{
    public int stageNumber;
    public ObjectiveData objective;
    [TextArea(2, 4)] public string information;
}
