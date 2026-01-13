using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;

public class TutorialManager : MonoBehaviour, IOnLocalPlayerReadyTarget
{
    /// <summary>
    /// A Tutorial "Hint" is a step of the tutorial. It has its own hint prompt, and
    /// expected actions to be completed.
    /// </summary>

    public static TutorialManager Singleton;
    public static TutorialHint currentHint;

    // Keeps references to available tutorials around, not inherently tied to the starting mechanisms after adding callbacks
    List<TutorialSequence> availableSequences = new();

    public void OnLocalPlayerReady(Player player)
    {
        if (Singleton != null && Singleton != this)
        {
            Destroy(gameObject);
            return;
        }

        Singleton = this;
        DontDestroyOnLoad(gameObject);

        InitializeTutorials();
    }
    
    void InitializeTutorials()
    {
        // PortalChargingTutorialSequence portalTut = new PortalChargingTutorialSequence();
        // if (!portalTut.Completed())
        // {
        //     portalTut.SetStartAction(ref AreaPortal.OnAreaPortalDiscovered);
        //     availableSequences.Add(portalTut);
        // }
    }

    public static void StartSequence(TutorialSequence sequence)
    {
        Debug.Log("START TUTORIAL " + sequence);
        Singleton.StartCoroutine(sequence.RunTutorial());
    }



    public static void SetHint(string hintText, float timeout = -1)
    {
        if (!Settings.settings.generalSettings.tutorialsEnabled)
        {
            return;
        }

        currentHint = new(hintText);

        SetHint(currentHint, timeout);
    }

    public static void SetHint(TutorialHint hint, float timeout = -1)
    {
        if (!Settings.settings.generalSettings.tutorialsEnabled)
        {
            return;
        }

        currentHint = hint;

        TutorialOverlay.Find().SetHint(currentHint, timeout);
    }


    public static void ClearHint()
    {
        currentHint = null;
    }

}
