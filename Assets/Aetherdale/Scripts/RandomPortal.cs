using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class RandomPortal : NetworkBehaviour, IInteractable
{
    static List<RandomPortal> portals = new();

    public void Start()
    {
        if (!portals.Contains(this))
        {
            portals.Add(this);
        }
    }

    public void OnDestroy()
    {
        if (portals.Contains(this))
        {
            portals.Remove(this);
        }
    }

    Vector3 GetExitPosition()
    {
        if (Physics.Raycast(transform.position + transform.InverseTransformVector(0, 5, 5), Vector3.down, out RaycastHit hitInfo, 10, LayerMask.GetMask("Default")))
        {
            return hitInfo.point;
        }

        return transform.position + transform.forward * 3;
    }

    public void Interact(ControlledEntity interactingEntity)
    {
        List<RandomPortal> possiblePortals = portals.Where((x) => x != this).ToList();

        if (possiblePortals.Count == 0)
        {
            throw new System.Exception("No other portals available");
        }

        RandomPortal destination = possiblePortals[Random.Range(0, possiblePortals.Count - 1)];
        Vector3 destPos = destination.GetExitPosition();

        interactingEntity.TargetSetPosition(destPos);
    }

    public bool IsInteractable(ControlledEntity interactingEntity)
    {
        return true;
    }

    public bool IsSelectable()
    {
        return true;
    }

    public string GetInteractionPromptText(ControlledEntity interactingEntity) => "Teleport";
    public string GetTooltipTitle(ControlledEntity interactingEntity) => "";
    public string GetTooltipText(ControlledEntity interactingEntity) => "";
}
