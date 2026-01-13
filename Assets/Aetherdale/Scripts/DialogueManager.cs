using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Manages dialogue, provides information. Eventually should be a database/API wrapper.
/// 
/// Only reference from server
/// </summary>

public class DialogueManager
{
    static Dictionary<Player, List<DialogueTopic>> spokenTopics = new();

    public static void AddPlayer(Player player)
    {
        if (!spokenTopics.ContainsKey(player))
        {
            spokenTopics.Add(player, new());
        }
    }

    public static DialogueTopic LookupTopic(string topicID)
    {
        Object[] topics = Resources.LoadAll("Dialogue", typeof(DialogueTopic));

        foreach(Object loaded in topics)
        {
            if (loaded is DialogueTopic topic)
            {
                if (topic.GetID() == topicID)
                {
                    return topic;
                }
            }
        }

        return null;
    }

    public static bool HasTopicBeenSpokenToPlayer(DialogueTopic topic, Player player)
    {
        if (!spokenTopics.ContainsKey(player))
        {
            spokenTopics.Add(player, new());
        }

        foreach (DialogueTopic spokenTopic in spokenTopics[player])
        {
            if (spokenTopic == topic)
            {
                return true;
            }
        }

        return false;
    }

    public static void SetTopicAsSpoken(DialogueTopic topic, Player player)
    {
        if (!spokenTopics.ContainsKey(player))
        {
            AddPlayer(player);
        }

        spokenTopics[player].Add(topic);
    }

}