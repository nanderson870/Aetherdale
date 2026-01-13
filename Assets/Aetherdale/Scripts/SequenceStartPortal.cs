using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class SequenceStartPortal : NetworkBehaviour, IInteractable
{
    [Server]
    public void Interact(ControlledEntity interactingEntity)
    {
        //Player interactingPlayer = interactingEntity.GetOwningPlayer();
        //PlayerUI interactingPlayerUI = interactingPlayer.GetUI();

        //interactingPlayerUI.OpenSequenceStartMenu();

        AreaSequencer.GetAreaSequencer().StartAreaSequence(AreaSequencer.SequenceMode.Normal);

        RpcSequenceStartEntered();
    }

    [ClientRpc]
    void RpcSequenceStartEntered()
    {
        //Debug.Log("send story event");
        StoryEvent.Send("EnterBluePortal");
    }

    public bool IsInteractable(ControlledEntity interactingEntity)
    {
        return true;
    }

    public bool IsSelectable()
    {
        return true;
    }
    
    public string GetInteractionPromptText(ControlledEntity interactingEntity)
    {
        return "Embark";
    }

    public string GetTooltipText(ControlledEntity interactingEntity)
    {
        return "";
    }

    public string GetTooltipTitle(ControlledEntity interactingEntity)
    {
        return "";
    }
}
