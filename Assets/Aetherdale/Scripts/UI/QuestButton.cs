using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class QuestButton : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI textMesh;

    Quest associatedQuest;

    public void SetAssociatedQuest(Quest quest)
    {
        associatedQuest = quest;
        SetText(quest.GetName());
    }

    public Quest GetAssociatedQuest()
    {
        return associatedQuest;
    }

    void SetText(string text)
    {
        textMesh.text = text;
    }

}
