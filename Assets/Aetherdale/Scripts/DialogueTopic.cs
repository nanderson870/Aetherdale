using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
[CreateAssetMenu(fileName = "New Topic", menuName = "Aetherdale/Dialogue Topic", order = 0)]
public class DialogueTopic : ScriptableObject
{
    [SerializeField] string topicID = "";

    [SerializeField] bool repeatable = false;
    
    /// <summary>Spoken when the player receives this topic</summary>
    [TextArea(2, 5)][SerializeField] List<string> text;

    /// <summary>Spoken when the player has already heard this topic.</summary>
    [TextArea(2, 5)][SerializeField] List<string> repeatedTopicText = new();

    /// <summary>If pointed to a next topic, no responses will be offered. The next topic will be spoken when continuing, instead</summary>
    [SerializeField] DialogueTopic nextTopic;

    [SerializeField] List<DialogueResponse> responses;

    [SerializeField] List<Condition> appearanceConditions = new();

    /// <summary>This quest will be started when this line is spoken</summary>
    [SerializeField] QuestData givesQuest;

    [SerializeField] string storyEventTriggered = "";
    
    public void OnValidate()
    {
        #if UNITY_EDITOR
            if (topicID == "")
            {
                topicID = GUID.Generate().ToString();
                EditorUtility.SetDirty(this);
            }
            
        #endif
    }

    public string GetID()
    {
        return topicID;
    }

    public List<string> GetText()
    {
        List<string> textCopy = new();

        foreach (string str in text)
        {
            textCopy.Add(str);
        }

        return textCopy;
    }

    public bool HasRepeatText()
    {
        return repeatedTopicText != null && repeatedTopicText.Count > 0;
    }

    public List<string> GetRepeatText()
    {
        List<string> repeatTextCopy = new();

        foreach (string str in repeatedTopicText)
        {
            repeatTextCopy.Add(str);
        }

        return repeatTextCopy;
    }

    public DialogueTopic GetNextTopic()
    {
        return nextTopic;
    }

    public List<DialogueResponse> GetResponses()
    {
        return responses;
    }

    public List<Condition> GetAppearanceConditions()
    {
        return appearanceConditions;
    }

    public bool AppearanceConditionsMet(Player player)
    {
        if (!repeatable && player.GetPlayerData().HeardTopics.Contains(this))
        {
            return false;
        }

        foreach (Condition condition in appearanceConditions)
        {
            if (!condition.Check(player))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    // Package up the available responses for the given player
    // Client only sees an index and text, for each response available
    /// </summary>
    /// <param name="respondingPlayer">The player to check response conditions for</param>
    /// <param name="indices">Returns with the response indices</param>
    /// <param name="text">Returns with the corresponding response text</param>
    public void GetResponseDataLists(Player respondingPlayer, out List<int> indices, out List<string> text)
    {
        indices = new();
        text = new();

        int numResponses = 0;
        foreach (DialogueResponse response in GetResponses())
        {
            if (response.CheckConditions(respondingPlayer))
            {
                indices.Add(numResponses);
                text.Add(response.responseText);
            }
            numResponses++; // Always increment numResponses - the response index is the overall index, not the final filtered index
        }
    }


    public QuestData GetGivenQuest()
    {
        return givesQuest;
    }

    public string GetStoryEventTriggered()
    {
        return storyEventTriggered;
    }
}
