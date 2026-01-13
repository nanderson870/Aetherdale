using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

/*
Runtime instance of a quest, representing a Quest

The one actually given to a player - tracks progress through the quest
*/

public class Quest
{
    readonly QuestData data;

    readonly string questName;

    readonly string questDescription;

    int currentStageNumber = 0;

    readonly List<QuestStage> stages = new();

    readonly List<HeldItem> rewardItems = new();

    public UnityAction<Quest> OnQuestCompleted;

    public Action<Objective> OnQuestObjectiveStarted;
    public Action<Objective> OnQuestObjectiveCompleted;

    public Quest(QuestData quest, int startingStageNumber = -1, int currentRepetitions = 0)
    {
        data = quest;

        questName = quest.GetName();
        questDescription = quest.GetDescription();

        OnQuestCompleted = data.OnQuestCompleted;
        rewardItems = quest.GetRewards();

        foreach (QuestStageData stageData in quest.GetStages())
        {
            QuestStage stage = new(stageData);
            stages.Add(stage);
        }
        
        stages.Sort(delegate(QuestStage stage1, QuestStage stage2)
        {
            return stage1.stageNumber.CompareTo(stage2.stageNumber);
        });

        if (startingStageNumber < 0)
        {
            currentStageNumber = stages[0].stageNumber;
        }
        else
        {
            QuestStage startingStage = GetQuestStage(startingStageNumber);
            if (startingStage == null)
            {
                throw new Exception("Tried to create quest " + GetName() + " at invalid start stage: " + startingStageNumber);
            }

            currentStageNumber = startingStageNumber;
        }

        Objective obj = GetCurrentObjective();
        if (obj != null)
        {
            obj.SetCurrentRepetitions(currentRepetitions);
        }

    }

    public void Start()
    {        
        TrackStage(currentStageNumber);
    }

    void AdvanceStage()
    {
        if (GetCurrentObjective() != null)
        {
            OnQuestObjectiveCompleted?.Invoke(GetCurrentObjective());
        }

        QuestStage nextStage = GetNextStage();

        if (nextStage == null)
        {
            Debug.Log("NO MORE STAGES OF " + GetName());
            Complete();
            return;
        }

        currentStageNumber = nextStage.stageNumber;

        TrackStage(currentStageNumber);
    }

    void TrackStage(int stageNum)
    {
        Objective currentObjective = GetQuestStage(stageNum).objective;

        Debug.Log("tracking " + currentObjective);
        if (!currentObjective.IsTracked)
        {
            currentObjective.StartTracking();
            currentObjective.OnObjectiveCompleted += (Objective _) => {AdvanceStage();};
            
            OnQuestObjectiveStarted?.Invoke(currentObjective);
        }
    }

    void Complete()
    {
        Debug.Log(OnQuestCompleted);
        OnQuestCompleted?.Invoke(this);
    }

    public Objective GetCurrentObjective()
    {
        foreach (QuestStage stage in stages)
        {
            if (stage.stageNumber == currentStageNumber)
            {
                return stage.objective;
            }
        }

        return null;
    }


    public string GetName()
    {
        return questName;
    }

    public string GetQuestDescription()
    {
        return questDescription;
    }

    public int GetCurrentStageNumber()
    {
        return currentStageNumber;
    }

    public QuestStage GetQuestStage(int index)
    {
        foreach (QuestStage stage in stages)
        {
            if (stage.stageNumber == index)
            {
                return stage;
            }
        }

        return null;
    }

    public QuestStage GetNextStage()
    {
        for (int i = 0; i < stages.Count; i++)
        {
            if (stages[i].stageNumber == currentStageNumber && i + 1 < stages.Count)
            {
                return stages[i + 1];
            }
        }

        return null;
    }

    public string GetID()
    {
        return data.GetID();
    }

    public static string Serialize(Quest quest)
    {
        return $"{quest.data.GetID()}|{quest.GetCurrentStageNumber()}|{quest.GetCurrentObjective().GetCurrentRepetitions()}";
    }

    public static Quest Deserialize(string serialized)
    {
        string[] split = serialized.Split("|");

        QuestData questData = QuestManager.LookupQuestData(split[0]);
        int currentStage = int.Parse(split[1]);
        
        int currentRepetitions = 0;
        if (split.Length > 2)
        {
            currentRepetitions = int.Parse(split[2]);
        }

        Quest ret = new (questData, currentStage, currentRepetitions);
        return ret;
    }
}


public class QuestStage
{
    public QuestStage(QuestStageData data)
    {
        stageNumber = data.stageNumber;
        objective = data.objective.GetInstance();
    }

    public int stageNumber;
    public Objective objective;
}
