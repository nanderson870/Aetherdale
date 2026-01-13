using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

public class DialogueAgent : NetworkBehaviour, IOnLocalPlayerReadyTarget
{
    const float CHATTER_STAY_DURATION = 5.0F;

    [SerializeField] int maxPriorityTopicsPerMeeting = 1;
    [SerializeField] List<DialogueTopic> priorityTopics;

    [Tooltip("Topics that are spoken at random when there is no priority topic.")]
    [SerializeField] List<DialogueTopic> chatterTopics = new();

    [SerializeField][FormerlySerializedAs("dialogueBubbleTransform")] Transform chatBubbleTransform;

    /// <summary> Topics this NPC has spoken to a player, that are currently open </summary>
    Dictionary<Player, DialogueTopic> dialogueSessionInfo = new();

    public static Action<DialogueAgent, Player, DialogueTopic> OnTopicSpoken;
    public static Action<DialogueAgent, string> OnChatter;
    public static Action<DialogueAgent> OnChatterClear;


    DialogueTopic currentChatterTopic = null;

    // List to prevent frequent chatter duplicates
    List<DialogueTopic> chatterTopicsSpoken = new();

    int spokenPriorityTopics = 0;


    public void OnLocalPlayerReady(Player player)
    {
    }

    DialogueTopic GetHighestPriorityTopic(Player player)
    {
        // Currently we just grab the first topic that can be spoken
        foreach (DialogueTopic topic in priorityTopics)
        {
            if (topic.AppearanceConditionsMet(player))
            {
                return topic;
            }
        }

        return null;
    }

    List<DialogueTopic> GetAvailableChatterTopics(Player player)
    {
        List<DialogueTopic> ret = new();

        foreach (DialogueTopic dialogueTopic in chatterTopics)
        {
            if (dialogueTopic.AppearanceConditionsMet(player) && !chatterTopicsSpoken.Contains(dialogueTopic))
            {
                ret.Add(dialogueTopic);
            }
        }

        return ret;
    }

    public bool CanSpeakPriorityTopics()
    {
        return maxPriorityTopicsPerMeeting == 0 || spokenPriorityTopics < maxPriorityTopicsPerMeeting;
    }

    public bool HasPriorityTopic()
    {
        return GetHighestPriorityTopic(Player.GetLocalPlayer()) != null;
    }

    [Client]
    public void Talk(ControlledEntity speakingEntity)
    {
        Player playerAddressed = speakingEntity.GetOwningPlayer();
        DialogueTopic priorityTopic = GetHighestPriorityTopic(playerAddressed);

        if (priorityTopic != null && CanSpeakPriorityTopics())
        {
            SpeakTopicToPlayer(priorityTopic, playerAddressed);
            spokenPriorityTopics++;
        }
        else if (currentChatterTopic == null)
        {
            List<DialogueTopic> chatterTopics = GetAvailableChatterTopics(playerAddressed);
            
            if (chatterTopics.Count > 0)
            {
                DialogueTopic chosenTopic = chatterTopics[UnityEngine.Random.Range(0, chatterTopics.Count)];
                Chatter(chosenTopic);
            }
        }
    }
    
    [Client]
    private void SpeakTopicToPlayer(DialogueTopic topicToShow, Player playerAddressed)
    {
        topicToShow.GetResponseDataLists(playerAddressed, out List<int> responseIndices, out List<string> responseText);
        
        List<string> textToShow = topicToShow.GetText();

        // Show repeat text instead if applicable, and we've already spoken this topic to the player
        if (topicToShow.HasRepeatText() && DialogueManager.HasTopicBeenSpokenToPlayer(topicToShow, playerAddressed))
        {
            textToShow = topicToShow.GetRepeatText();
        }

        // Send topic to player
        playerAddressed.ShowDialogueText(this, textToShow, responseIndices, responseText);

        // Track currently spoken topics
        dialogueSessionInfo.Add(playerAddressed, topicToShow);

        // Add to the player's heard topic list
        if (!playerAddressed.GetPlayerData().HeardTopics.Contains(topicToShow))
        {
            playerAddressed.GetPlayerData().HeardTopics.Add(topicToShow);
        }

        // Topic spoken callback
        OnTopicSpoken?.Invoke(this, playerAddressed, topicToShow);

        if (topicToShow.GetStoryEventTriggered() != "")
        {
            StoryEvent.Send(topicToShow.GetStoryEventTriggered());
        }
    }
    

    /// <summary>
    /// Tell server to process a player's dialogue response, signified by its index given by the server
    /// </summary>
    /// <param name="speakingPlayer">The player who selected this response</param>
    /// <param name="index">The index of the response selected</param>
    [Client]
    public void GiveDialogueResponse(Player speakingPlayer, int index)
    {
        if (!dialogueSessionInfo.ContainsKey(speakingPlayer))
        {
            Debug.LogError("A player is trying to speak when not in dialogue");
            return;
        }

        DialogueTopic currentTopic = dialogueSessionInfo[speakingPlayer];
        dialogueSessionInfo.Remove(speakingPlayer);

        List<DialogueResponse> topicResponses = currentTopic.GetResponses();

        // If there are no responses to this dialogue, any response is accepted as continue/close
        if (topicResponses.Count == 0)
        {
            DialogueTopic followupTopic = currentTopic.GetNextTopic();
            if (followupTopic != null)
            {
                // Still a next topic to say
                SpeakTopicToPlayer(followupTopic, speakingPlayer);
                return;
            }
            else
            {
                //Ending dialogue, no more to say

                // Offer a quest if there is one
                if (currentTopic.GetGivenQuest() is QuestData offeredQuest && offeredQuest != null)
                {
                    Quest givenQuest = new(offeredQuest);
                    speakingPlayer.GiveQuest(givenQuest);
                }
                
                speakingPlayer.EndDialogue();
                return;
            }
        }

        // Check for invalid index
        if (topicResponses.Count <= index)
        {
            Debug.LogError("A player is trying to select an invalid response");
            return;
        }

        DialogueResponse attemptedResponse = currentTopic.GetResponses()[index];

        if (!attemptedResponse.CheckConditions(speakingPlayer))
        {
            Debug.LogError("ERROR: A player is trying to select a response they do not meet the conditions for");
            return;
        }


        // Now we are certain that this is an acceptable response for our player to have chosen

        // Check the next topic
        DialogueTopic nextTopic =  attemptedResponse.GetResultingTopic();
        if (nextTopic == null)
        {
            // No follow-up, close dialogue
            speakingPlayer.EndDialogue();
            return;
        }

        SpeakTopicToPlayer(nextTopic, speakingPlayer);
    }

    [Client]
    public void CloseDialogue(Player player)
    {
        if (dialogueSessionInfo.ContainsKey(player))
        {
            dialogueSessionInfo.Remove(player);
        }
    }

    [Client]
    void Chatter(DialogueTopic topic)
    {
        if (!Player.GetLocalPlayer().GetPlayerData().HeardTopics.Contains(topic))
        {
            Player.GetLocalPlayer().GetPlayerData().HeardTopics.Add(topic);
        }

        chatterTopicsSpoken.Add(topic);

        OnTopicSpoken?.Invoke(this, Player.GetLocalPlayer(), topic);
        OnChatter?.Invoke(this, topic.GetText()[0]);

        if (TryGetComponent(out NonPlayerCharacter npc))
        {
            Player.SendNpcMessage(npc, topic.GetText()[0]);
        }

        currentChatterTopic = topic;

        Invoke(nameof(ClearChatter), CHATTER_STAY_DURATION);
    }

    [Client]
    void ClearChatter()
    {
        currentChatterTopic = null;

        OnChatterClear?.Invoke(this);
    }

    public Transform GetChatBubbleTransform()
    {
        return chatBubbleTransform;
    }

}