
using Mirror;
using UnityEngine;


/*
Need to rename. Possibilities:
    -ServerControlledSceneObject
    -PerhapsDisabledNetworkBehavior
*/
public class StatefullyActiveNetworkBehaviour : NetworkBehaviour
{
    public enum ActiveState
    {
        Undefined = 0,
        OrderedActive = 1,
        OrderedInactive = 2
    }

    [SyncVar(hook = nameof(OrderedStateChanged))] public ActiveState orderedState = ActiveState.Undefined;

    [Server]
    public void OrderState(ActiveState orderedState)
    {
        this.orderedState = orderedState;
    }

    void OrderedStateChanged(ActiveState previousState, ActiveState newState)
    {
        if (orderedState == ActiveState.OrderedActive)
        {
            gameObject.SetActive(true);
        }
        else if (orderedState == ActiveState.OrderedInactive)
        {
            gameObject.SetActive(false);
        }
    }

    void Start()
    {
        if (orderedState == ActiveState.Undefined)
        {
            gameObject.SetActive(false);
        }
    }
}