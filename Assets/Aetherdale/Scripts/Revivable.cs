using Mirror;
using UnityEngine;

public class Revivable : Progressable, IInteractable
{
    [SyncVar] public Player owningPlayer;

    [Server]
    public void Interact(ControlledEntity interactingEntity)
    {
        owningPlayer.Restore(0.5F, 0);

        // NetworkServer.UnSpawn(gameObject);
        Destroy(gameObject);
    }

    public string GetInteractionPromptText(ControlledEntity interactingEntity)
    {
        if (owningPlayer == null)
        {
            return "???";
        }
        
        return $"Revive {owningPlayer.GetDisplayName()}";
    }

    public string GetTooltipText(ControlledEntity interactingEntity)
    {
        return "";
    }

    public string GetTooltipTitle(ControlledEntity interactingEntity)
    {
        return "";
    }

    public bool IsInteractable(ControlledEntity interactingEntity)
    {
        return true;
    }

    public bool IsSelectable()
    {
        return true;
    }

}