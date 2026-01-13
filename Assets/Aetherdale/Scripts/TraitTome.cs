using System;
using Mirror;
using UnityEngine;

public class TraitTome : Pickup, IInteractable
{

    [SerializeField] TraitTomeMesh traitTomeMesh;
    Trait trait;

    [SyncVar(hook=nameof(TraitChangedClient))] string traitName = "";

    public override void Start()
    {
        base.Start();

        if (trait == null && NetworkServer.active)
        {
            Trait trait = Trait.GetRandomTraitAccountingForRarity();

            SetTrait(trait);
        }

        if (isClient && traitName != "")
        {
            trait = (Trait) Activator.CreateInstance(Type.GetType(traitName.Replace(" ", "")));
            SetTrait(trait);
        }
    }

    void TraitChangedClient(string prevName, string newName)
    {
        Type traitType = Type.GetType(traitName.Replace(" ", ""));
        if (traitType == null)
        {
            Debug.LogError($"Trait Type was null for trait tome trait {newName}");
            return;
        }

        trait = (Trait) Activator.CreateInstance(traitType);
        SetTrait(trait);
    }

    protected override void OnPickup(Entity entity)
    {
        if (entity.GetOwningPlayer() != null)
        {
            entity.GetOwningPlayer().AddTrait(trait);
        }
    }

    public void SetTrait(Trait trait)
    {
        this.trait = trait;
        this.traitName = trait.GetType().Name;
        traitTomeMesh.SetTrait(trait);
    }

    public bool IsInteractable(ControlledEntity interactingEntity)
    {
        return true;
    }

    public string GetInteractionPromptText(ControlledEntity interactingEntity)
    {
        return "Acquire Trait: " + trait.GetName();
    }

    public string GetTooltipTitle(ControlledEntity interactingEntity)
    {
        return "Trait Tome: " + trait.GetName();
    }

    public string GetTooltipText(ControlledEntity interactingEntity)
    {
        return trait.GetStatsDescription();
    }
}
