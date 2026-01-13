using Mirror;
using UnityEngine;

public class LoadoutStation : NetworkBehaviour, IInteractable
{
    public string GetInteractionPromptText(ControlledEntity interactingEntity)
    {
        return "Manage Loadout";
    }

    public string GetTooltipText(ControlledEntity interactingEntity)
    {
        return "";
    }

    public string GetTooltipTitle(ControlledEntity interactingEntity)
    {
        return "";
    }

    public void Interact(ControlledEntity interactingEntity)
    {
        interactingEntity.GetOwningPlayer().GetUI().TargetOpenLoadoutMenu();
    }

    public bool IsInteractable(ControlledEntity interactingEntity)
    {
        return true;
    }

    public bool IsSelectable()
    {
        return true;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
