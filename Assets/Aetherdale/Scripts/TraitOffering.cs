using System;
using Mirror;
using UnityEngine;

public class TraitOffering : NetworkBehaviour, IInteractable
{
    [SerializeField] Renderer iconRenderer;

    [SyncVar(hook = nameof(TraitChangedClient))] string traitName = "";

    Trait trait;

    public Action<Player> OnTaken;


    public void Start()
    {
        if (trait == null && NetworkServer.active)
        {
            trait = Trait.GetRandomCursedTrait();
            SetTrait(trait);
        }

        if (isClient && traitName != "")
        {
            trait = (Trait) Activator.CreateInstance(Type.GetType(traitName.Replace(" ", "")));
            SetTraitIcon(trait); 
        }
    }

    [Server]
    public void SetTrait(Trait trait)
    {
        this.trait = trait;
        this.traitName = trait.GetType().Name;
    }

    void TraitChangedClient(string prevName, string newName)
    {
        trait = (Trait) Activator.CreateInstance(Type.GetType(traitName.Replace(" ", "")));
        SetTraitIcon(trait);
    }

    public void SetTraitIcon(Trait trait)
    {
        Sprite sprite = trait.GetIcon();

        Color rarityColor = ColorPalette.GetColorForRarity(trait.GetRarity());

        iconRenderer.material.SetTexture("_Texture", sprite.texture);
        
        Color.RGBToHSV(rarityColor, out float h, out float s, out float v);

        if (s == 0)
        {
            v = 0.6F; // Pure white is a bit too bright to have 1.0 value and emission
        }
        else
        {
            v = 1.0F; // Crank that puppy up
        }

        Color emissiveRarityColor = Color.HSVToRGB(h, s, v);
        iconRenderer.material.SetColor("_EmissiveColor", emissiveRarityColor * 1.2F);
    }

    public void Interact(ControlledEntity interactingEntity)
    {
        interactingEntity.GetOwningPlayer().AddTrait(trait);

        OnTaken?.Invoke(interactingEntity.GetOwningPlayer());

        NetworkServer.UnSpawn(gameObject);
        Destroy(gameObject);
    }

    public bool IsInteractable(ControlledEntity interactingEntity)
    {
        return true;
    }

    public bool IsSelectable()
    {
        return true;
    }

    public string GetInteractionPromptText(ControlledEntity interactingEntity)
    {
        return $"Receive {trait.GetName()}"; 
    }

    public string GetTooltipTitle(ControlledEntity interactingEntity)
    {
        return trait.GetName();
    }

    public string GetTooltipText(ControlledEntity interactingEntity)
    {
        return trait.GetStatsDescription();
    }
}
