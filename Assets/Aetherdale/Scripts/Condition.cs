using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
A condition on dialogue, quests, etc

Only valid when checked on the server
*/

public enum ConditionSubject
{
    PlayerHasQuest,
    PlayerCompletedQuest,
    PlayerHasObjective,
    PlayerHasAether,
    PlayerUnlockedIdols,
    PlayerUnlockedTrinkets,
    RunsCompleted,
    RunsVictorious,
    CurrentRegionIs,
}
 
public enum ConditionPreposition
{
    Equals,
    DoesNotEqual,
    GreaterThan,
    LessThan,
}

[System.Serializable]
public class Condition
{
    public ConditionSubject subject;
    public ConditionPreposition preposition;
    public int value;
    public GameObject optionalGameObject;
    public ScriptableObject optionalScriptableObject;

    // Check if this condition is true with respect to the given player
    public bool Check(Player player)
    {
        int evaluation;

        QuestData quest = optionalScriptableObject as QuestData;
        ObjectiveData obj = optionalScriptableObject as ObjectiveData;
        ItemData item = optionalScriptableObject as ItemData;
        Region region = optionalScriptableObject as Region;

        switch (subject)
        {
            case ConditionSubject.PlayerHasQuest:
                if (quest == null || player == null)
                {
                    return false;
                }

                evaluation = player.GetPlayerData().HasQuest(quest.GetID()) ? 1 : 0;

                break;

            case ConditionSubject.PlayerCompletedQuest:
                if (quest == null || player == null)
                {
                    return false;
                }

                evaluation = player.GetPlayerData().CompletedQuest(quest.GetID()) ? 1 : 0;

                break;

            case ConditionSubject.PlayerHasObjective:
                if (obj == null || player == null)
                {
                    return false;
                }

                evaluation = player.GetPlayerData().HasObjective(obj) ? 1 : 0;
                break;

            case ConditionSubject.PlayerUnlockedIdols:
                if (player == null)
                {
                    return false;
                }

                evaluation = player.GetPlayerData().UnlockData.idolsUnlocked ? 1 : 0;
                break;

            case ConditionSubject.PlayerUnlockedTrinkets:
                if (player == null)
                {
                    return false;
                }

                evaluation = player.GetPlayerData().UnlockData.trinketsUnlocked ? 1 : 0;
                break;

            case ConditionSubject.RunsCompleted:
                if (player == null)
                {
                    return false;
                }

                evaluation = player.GetPlayerData().statistics.runsCompleted;
                break;

            case ConditionSubject.RunsVictorious:
                if (player == null)
                {
                    return false;
                }

                evaluation = player.GetPlayerData().statistics.runsVictorious;
                break;

            case ConditionSubject.CurrentRegionIs:
                AreaManager areaManager = MonoBehaviour.FindAnyObjectByType<AreaManager>(FindObjectsInactive.Include);

                evaluation = areaManager.GetArea().region == region ? 1 : 0;

                break;
            default:
                return false;
        }

        switch(preposition)
        {
            case ConditionPreposition.Equals:
                return evaluation == value;

            case ConditionPreposition.DoesNotEqual:
                return evaluation != value;

            case ConditionPreposition.GreaterThan:
                return evaluation > value;

            case ConditionPreposition.LessThan:
                return evaluation < value;
            
            default:
                return false;
        }
    }
}

