using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager
{
    public static Quest GetQuestInstance(string questName)
    {
        QuestData questData = LookupQuestData(questName);

        if (questData != null)
        {
            Quest inst = new Quest(questData);
            return inst;
        }
        else
        {
            return null;
        }
    }

    public static QuestData LookupQuestData(string questId)
    {
        Object[] quests = Resources.LoadAll("Quests", typeof(QuestData));

        foreach(Object loaded in quests)
        {
            if (loaded is QuestData questData)
            {
                if (questData.GetID() == questId)
                {
                    return questData;
                }
            }
        }

        Debug.LogError("Could not find data for quest ID " + questId);
        return null;
    }

    public static QuestData LookupQuestDataByName(string questName)
    {
        Object[] quests = Resources.LoadAll("Quests", typeof(QuestData));

        foreach(Object loaded in quests)
        {
            if (loaded is QuestData questData)
            {
                if (questData.GetName() == questName)
                {
                    return questData;
                }
            }
        }

        Debug.LogError("Could not find data for a quest named " + questName);
        return null;
    }
}
