using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TraitsDisplay : MonoBehaviour, IOnLocalPlayerReadyTarget
{
    [SerializeField] TraitIcon traitIconPrefab;

    void Start()
    {
        // if (Player.IsLocalPlayerReady)
        // {
        //     SetTraits(Player.GetLocalPlayer().GetTraits());
        // }
    }


    public void OnLocalPlayerReady(Player player)
    {
        SetTraits(player.GetTraits());
        player.OnTraitsChangedOnClient += (TraitList traitList) => {SetTraits(traitList);};
    }

    public void SetTraits(TraitList traits)
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        foreach (Trait trait in traits)
        {
            TraitIcon icon = Instantiate(traitIconPrefab, transform);
            icon.SetTrait(trait);
        }
    }

}
