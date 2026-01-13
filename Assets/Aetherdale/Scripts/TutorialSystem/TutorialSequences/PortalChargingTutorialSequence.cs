
using UnityEngine;
using System.Collections;

public class PortalChargingTutorialSequence : TutorialSequence
{
    public override IEnumerator RunTutorial()
    {
        Debug.Log("START PORTAL CHARGE TUTORIAL");
        TutorialHint hint = new("You've found a portal! Activate the portal by approaching it and pressing F.");
        TutorialManager.SetHint(hint);

        yield return null;

        // TODO this doesnt work
        while (AreaManager.CurrentAreaManager.GetRandomChargingPortal() == null)
        {
            yield return null;
        }

        hint = new("After activating a portal, charge it by killing enemies in range of it.");
        TutorialManager.SetHint(hint, 10.0F);

        AreaPortal areaPortal = AreaManager.CurrentAreaManager.GetRandomChargingPortal();
        while (areaPortal != null && areaPortal.currentRebuild < 1.0F)
        {
            yield return null;
        }
    }
}