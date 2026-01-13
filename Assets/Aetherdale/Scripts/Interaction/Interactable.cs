using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable
{
    GameObject gameObject { get ; }

    public void Interact(ControlledEntity interactingEntity);

    public bool IsInteractable(ControlledEntity interactingEntity);
    public bool IsSelectable();

    public string GetInteractionPromptText(ControlledEntity interactingEntity);

    public string GetTooltipTitle(ControlledEntity interactingEntity);
    public string GetTooltipText(ControlledEntity interactingEntity);

}
