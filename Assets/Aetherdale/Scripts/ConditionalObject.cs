using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ConditionalObject : MonoBehaviour
{
    [SerializeField] bool ignoreConditions=false;
    [SerializeField] List<Condition> appearanceConditions = new();

    public static void ReevaluateAll()
    {
        foreach (ConditionalObject target in FindObjectsByType<ConditionalObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            try
            {
                target.EvaluateConditionalAppearance();
            }
            catch {}
        }
    }

    [ClientCallback]
    void Start()
    {
        if (Player.IsLocalPlayerReady)
        {
            // We're past initialization, (i.e. player returning to area, loading new area) so should check conditions here too
            EvaluateConditionalAppearance();
        }
    }
    
    void EvaluateConditionalAppearance()
    {
        if (ignoreConditions)
        {
            return;
        }

        if (appearanceConditions.Count > 0)
        {
            foreach (Condition condition in appearanceConditions)
            {
                if (!condition.Check(Player.GetLocalPlayer()))
                {
                    gameObject.SetActive(false);
                    return;
                }
            }

            gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("No conditions on conditional object");
        }
    }
}