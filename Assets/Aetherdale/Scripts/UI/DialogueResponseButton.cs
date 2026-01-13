using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DialogueResponseButton : MonoBehaviour
{
    [SerializeField] DialogueMenu menu;
    [SerializeField] TextMeshProUGUI tmp;
    int selectionIndex = -1;

    public void SetResponseData(int selectionIndex, string text)
    {
        this.selectionIndex = selectionIndex;
        tmp.text = text;
        gameObject.SetActive(true);
    }

    public void Clear()
    {
        gameObject.SetActive(false);
        selectionIndex = -1;
    }

    public bool HasResponse()
    {
        return selectionIndex >= 0;
    }

    public void OnButtonPress()
    {
        menu.Respond(selectionIndex);
    }
}
