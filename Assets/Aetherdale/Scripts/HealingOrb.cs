

using UnityEngine;

public class HealingOrb : Pickup
{
    [SerializeField] int healingAmount;

    protected override void OnPickup(Entity entity)
    {
        entity.Heal(healingAmount, null, false);
    }

    public void SetHealingAmount(int amount)
    {
        healingAmount = amount;
    }
}