
using System;
using System.Collections.Generic;
using UnityEngine;

public class RepairPortalObjective : Objective
{
    List<AreaPortal> portals = new();

    public RepairPortalObjective() : base("Find a portal and repair it", 1)
    {
    }

    public override string GetDescription()
    {
        if (IsAnyPortalActive())
        {
            return "Continue through the portal";
        }
        
        foreach (AreaPortal portal in portals)
        {
            if (portal.rebuilding)
            {
                return $"Repair the portal - {(int) (portal.currentRebuild * 100)}%";
            } 
        }

        return "Find a portal and repair it";
    }

    public override void RegisterCallbacks(Player owningPlayer)
    {
        AreaPortal.OnPortalRebuildStart += PortalStarted;
    }

    public override void UnregisterCallbacks(Player owningPlayer)
    {
        AreaPortal.OnPortalRebuildStart -= PortalStarted;
    }

    void ProgressObjective(AreaPortal portal)
    {
        ProgressObjective();
    }

    void PortalStarted(AreaPortal portal)
    {
        portals.Add(portal);
        portal.OnRebuildValueChanged += PortalRebuildingChanged;
        portal.OnFinishedRebuilding += PortalCharged;
        portal.OnFinishedRebuilding += ProgressObjective;
    }

    void PortalCharged(AreaPortal portal)
    {
        portal.OnFinishedRebuilding -= PortalCharged;
        portals.Remove(portal);
    }

    bool IsAnyPortalActive()
    {
        foreach (AreaPortal portal in portals)
        {
            if (portal.portalActive)
            {
                return true;
            }
        }
        return false;
    }

    void PortalRebuildingChanged(AreaPortal portal, float value)
    {
        if (IsAnyPortalActive())
        {
            return;
        }

        OnObjectiveUpdated?.Invoke(this);
    }
}   