using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueResponse
{
    public string responseText;
    [SerializeField] DialogueTopic resultingTopic;
    [SerializeField] List<Condition> appearanceConditions;

    public DialogueTopic GetResultingTopic()
    {
        return resultingTopic;
    }

    public bool CheckConditions(Player player)
    {
        foreach (Condition cond in appearanceConditions)
        {
            if (!cond.Check(player))
            {
                return false;
            }
        }

        return true;
    }

}
