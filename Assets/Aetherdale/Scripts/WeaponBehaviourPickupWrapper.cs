using UnityEngine;


/*
This script "converts" a weapon behaviour into a "Pickup"
*/

public class WeaponBehaviourPickupWrapper : Pickup, IInteractable
{
    public WeaponBehaviour weaponBehaviour;

    public string GetInteractionPromptText(ControlledEntity interactingEntity)
    {
        return $"Pick up {weaponBehaviour.weaponData.GetName()}";
    }

    public string GetTooltipText(ControlledEntity interactingEntity)
    {
        return $"{weaponBehaviour.weaponData.GetDescription()}";
    }

    public string GetTooltipTitle(ControlledEntity interactingEntity)
    {
        return $"{weaponBehaviour.weaponData.GetName()}";
    }

    public bool IsInteractable(ControlledEntity interactingEntity)
    {
        return true;
    }

    protected override void OnPickup(Entity entity)
    {
        if (entity is not IWeaponBehaviourWielder)
        {
            throw new System.Exception("A non-weapon-wielder tried to pick up a weapon behaviour wrapper");
        }

        weaponBehaviour.transform.SetParent(null);

        IWeaponBehaviourWielder wielder = (IWeaponBehaviourWielder)entity;
        wielder.EquipWeaponBehaviour(weaponBehaviour, dropPrevious:true);

        Destroy(gameObject);
    }
}
