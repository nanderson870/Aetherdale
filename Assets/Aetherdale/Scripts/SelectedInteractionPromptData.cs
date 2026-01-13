using System;
using Mirror;
using UnityEngine;

[System.Serializable]
public class SelectedInteractionPromptData
{
    public bool selectable = false;
    public bool interactable = false;
    public TooltipData tooltipData;

    public string interactionPromptText = "";

    public NetworkBehaviour associatedObject;
    public Vector3 position;

    public bool canTalk = false;

    public SelectedInteractionPromptData()
    {
        
    }

    public SelectedInteractionPromptData(InteractionPrompt prompt, ControlledEntity entity)
    {
        IInteractable interactable = prompt.GetInteractable();
        
        if (interactable != null)
        {
            this.selectable = interactable.IsSelectable();
            this.interactable = interactable.IsInteractable(entity);

            this.tooltipData = new();
            this.tooltipData.titleText = interactable.GetTooltipTitle(entity);
            this.tooltipData.descriptionText = interactable.GetTooltipText(entity);

            this.interactionPromptText = interactable.GetInteractionPromptText(entity);
        }

        this.position = prompt.transform.position;

        if (prompt.TryGetComponent(out NetworkBehaviour nwb))
        {
            associatedObject = nwb;
        }

        this.canTalk = prompt.TryGetComponent(out DialogueAgent _);

        // Special case for dialogue agentsa
        if (interactable == null && canTalk)
        {
            this.selectable = true;
        }
    }
}