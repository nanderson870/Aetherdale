using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

public class ObjectiveDisplay : NetworkBehaviour
{
    [SerializeField] TextMeshProUGUI currentObjectiveLabel;
    [SerializeField] TextMeshProUGUI currentObjectiveDescription;

    [SerializeField] TextMeshProUGUI questObjectiveLabel;
    [SerializeField] TextMeshProUGUI questObjectiveDescription;

    static List<ObjectiveDisplay> objectiveDisplays = new();


    // Tells ObjectiveDisplays to update the objective descriptionW
    public static void UpdateObjectiveDescription(string label, string desc) 
    {
        foreach (ObjectiveDisplay objectiveDisplay in objectiveDisplays)
        {
            objectiveDisplay.TargetUpdateAreaObjectiveGUI(label, desc);
        }
    } 

    public static void ClearObjectiveGUI() 
    {
        foreach (ObjectiveDisplay objectiveDisplay in objectiveDisplays)
        {
            objectiveDisplay.TargetClearAreaObjectiveGUI();
        }
    }

    void Awake()
    {
        questObjectiveLabel.gameObject.SetActive(false);
        questObjectiveDescription.gameObject.SetActive(false);
    }

    void Start()
    {
        if (NetworkClient.active)
        {
            PlayerDataRuntime.OnTrackedQuestChanged += SetTrackedQuest;
            PlayerDataRuntime.OnTrackedQuestCleared += ClearTrackedQuest;


            if (GetComponentInParent<PlayerUI>().isOwned)
            {
                currentObjectiveLabel.gameObject.SetActive(false);
                currentObjectiveDescription.gameObject.SetActive(false);
            }

            Quest trackedQuest = Player.GetLocalPlayer().GetPlayerData().TrackedQuest;
            if (trackedQuest != null)
            {
                SetTrackedQuest(trackedQuest);
            }
        }
        
        if (!objectiveDisplays.Contains(this))
        {
            objectiveDisplays.Add(this);
        }

        
    }

    void OnDestroy()
    {
        if (objectiveDisplays.Contains(this))
        {
            objectiveDisplays.Remove(this);
        }
    }

    [TargetRpc]
    void TargetClearAreaObjectiveGUI()
    {
        currentObjectiveLabel.gameObject.SetActive(false);
        currentObjectiveDescription.gameObject.SetActive(false);
    }
    
    [TargetRpc]
    public void TargetUpdateAreaObjectiveGUI(string label, string description)
    {
        SetObjectiveLabel(label);
        SetCurrentObjectiveDescription(description);
    }

    void SetObjectiveLabel(string objectiveLabelText)
    {
        currentObjectiveLabel.gameObject.SetActive(true);
        currentObjectiveLabel.text = objectiveLabelText;
    }

    public void SetCurrentObjectiveDescription(string desc)
    {
        currentObjectiveDescription.gameObject.SetActive(true);
        currentObjectiveDescription.text = desc;
    }

    #region Quest Objective
    public void SetTrackedQuest(Quest quest)
    {
        //questObjectiveLabel.gameObject.SetActive(true);
        //questObjectiveDescription.gameObject.SetActive(true);
//
        //questObjectiveDescription.text = quest.GetCurrentObjective().GetDescription();
//
        //quest.OnQuestObjectiveStarted += UpdateQuestObjective;
    }

    public void UpdateQuestObjective(Objective objective)
    {
        //questObjectiveLabel.gameObject.SetActive(true);
        //questObjectiveDescription.gameObject.SetActive(true);
//
        //questObjectiveDescription.text = objective.GetDescription();
    }

    public void ClearTrackedQuest(Quest quest)
    {
        //questObjectiveLabel.gameObject.SetActive(false);
        //questObjectiveDescription.gameObject.SetActive(false);
//
        //quest.OnQuestObjectiveStarted -= UpdateQuestObjective;
    }
    #endregion
}
