using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using UnityEngine.InputSystem;

public class DialogueMenu : Menu
{
    public TextMeshProUGUI textmesh;

    [SerializeField] List<DialogueResponseButton> responseButtons;


    // Per-topic variables
    DialogueAgent dialogueTarget;
    Queue<string> queuedText = new();
    Dictionary<int, string> responseData = new();

    public override void Open()
    {
        ClearResponseData();

        base.Open();
    }

    public override void Close()
    {
        GetOwningUI().GetOwningPlayer().CmdCloseDialogue(dialogueTarget);

        ClearDialogueTarget();

        ClearResponseData();
        queuedText.Clear();

        base.Close();
    }

    public override void ProcessInput()
    {
        if (InputSystem.actions.FindAction("Submit").WasPressedThisFrame() || InputSystem.actions.FindAction("Click").WasPressedThisFrame())
        {
            Continue();
        }
    }

    /// <summary>
    /// Prompts dialogue to continue, if there are multiple lines queued up to be spoken
    /// </summary>
    public void Continue()
    {
        if (queuedText.Count > 0)
        {
            string newLine = queuedText.Dequeue();
            SetDisplayedText(newLine);
            
            if (queuedText.Count == 0 && responseData.Count > 0)
            {
                ShowResponses();
            }
        }
        else if (queuedText.Count == 0 && responseData.Count == 0)
        {
            // No remaining text nor responses, respond with anything just to continue
            Respond(0);
        }

    }

    /// <summary>
    /// Sets the currently displayed spoken line.
    /// </summary>
    /// <param name="what">What is being said</param>
    public void SetDisplayedText(string what)
    {
        ClearResponseData();
        textmesh.SetText(what);
    }

    /// <summary>
    /// Sets up lines of dialogue to be spoken after the current one.
    /// These will be dequeued as the player continues dialogue.
    /// </summary>
    /// <param name="remainingText">The text to queue, in order</param>
    public void SetQueuedText(List<string> remainingText)
    {
        foreach (string text in remainingText)
        {
            queuedText.Enqueue(text);
        }
    }

    /// <summary>
    /// Sets responses accordingly, and shows them if there is no queued text.
    /// Queue text before calling this, if you are going to queue text.
    /// </summary>
    /// <param name="responseData">Dictionary of response index to spoken text</param>
    public void SetResponseData(Dictionary<int, string> responseData)
    {
        this.responseData = responseData;

        if (queuedText.Count == 0)
        {
            ShowResponses();
        }
    }

    public void SetDialogueTarget(DialogueAgent newTarget)
    {
        dialogueTarget = newTarget;
    }
    
    public void Respond(int chosenIndex)
    {
        PlayerUI owningUI = GetComponentInParent<PlayerUI>();
        if (owningUI == null)
        {
            Debug.LogError("ERROR: Dialogue menu not parented to any PlayerUI");
        }
        
        owningUI.GetOwningPlayer().GiveDialogueResponse(dialogueTarget, chosenIndex);
    }

    private void ClearDialogueTarget()
    {
        dialogueTarget = null;
    }

    void ShowResponses()
    {
        if (responseData.Count > 0)
        {
            foreach (KeyValuePair<int, string> response in responseData)
            {
                AddResponse(response.Key, response.Value);
            }
        }
    }


    void AddResponse(int selectionIndex, string text)
    {
        foreach(DialogueResponseButton btn in responseButtons)
        {
            if (!btn.HasResponse())
            {
                btn.SetResponseData(selectionIndex, text);
                return;
            }
        }
    }

    void ClearResponseData()
    {
        foreach (DialogueResponseButton btn in responseButtons)
        {
            btn.Clear();
        }
    }
}
