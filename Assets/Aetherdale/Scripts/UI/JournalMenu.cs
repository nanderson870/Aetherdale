using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JournalMenu : Menu
{
    [SerializeField] QuestButton questButtonPrefab;
    [SerializeField] VerticalLayoutGroup questListLayoutGroup;

    [SerializeField] TextMeshProUGUI questTitleText;
    [SerializeField] TextMeshProUGUI questDescriptionText;
    [SerializeField] TextMeshProUGUI objectiveDescriptionText;
    [SerializeField] TextMeshProUGUI rewardsDescriptionText;

    List<QuestButton> questButtons = new();

    public override void Close()
    {
        gameObject.SetActive(false);

        for (int i = questButtons.Count - 1; i >= 0; i--)
        {
            Destroy(questButtons[i].gameObject);
        }

        questButtons.Clear();
    }

    public override void Open()
    {
        questTitleText.text = "";
        questDescriptionText.text = "";
        objectiveDescriptionText.text = "";
        rewardsDescriptionText.text = "";
        
        PlayerUI owningUI = GetOwningUI();
        Player owningPlayer = owningUI.GetOwningPlayer();

        /*
        List<Quest> runningQuests = owningPlayer.GetRunningQuests();
        
        foreach(Quest inst in runningQuests)
        {
            QuestButton btn = Instantiate(questButtonPrefab, questListLayoutGroup.transform);
            btn.SetAssociatedQuest(inst);
            
            Button uiButton = btn.GetComponent<Button>();
            if (uiButton != null)
            {
                uiButton.onClick.AddListener(() => SelectQuest(inst));
            }

            questButtons.Add(btn);
        }

        if (runningQuests.Count > 0)
        {
            SelectQuest(runningQuests[0]);
        }

        gameObject.SetActive(true);
        */
    }

    public override void ProcessInput()
    {
    }

    public void SelectQuest(Quest quest)
    {
        questTitleText.text = quest.GetName();
        questDescriptionText.text = quest.GetQuestDescription();

        /*
        string objectivesDesc = "";
        foreach (Objective runningObj in quest.GetRunningObjectiveInstances())
        {
            objectivesDesc += runningObj.GetDescription();
        }
        objectiveDescriptionText.text = objectivesDesc;
        */
    }
}
