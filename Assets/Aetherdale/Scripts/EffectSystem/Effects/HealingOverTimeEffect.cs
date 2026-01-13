using UnityEngine;

[CreateAssetMenu(fileName = "Healing Over Time Effect", menuName = "Aetherdale/Effects/Healing Over Time Effect", order = 0)]
public class HealingOverTimeEffect : ProcEffect
{
    [SerializeField] bool showHealingPopup = false;
    [SerializeField] float healingPerProc;
    [SerializeField] StatValueCalculationMode valueCalculationMode;

    
    public StatValueCalculationMode GetValueCalculationMode() { return valueCalculationMode; }

    public override void Proc(EffectInstance instance, Entity target, Entity origin)
    {
        if (target.isServer)
        {
            float actualStatChange = healingPerProc;
            if (valueCalculationMode == StatValueCalculationMode.PercentageOfMaximum)
            {
                float healthPortionHealed = healingPerProc / 100.0F;
                actualStatChange = target.GetMaxHealth() * healthPortionHealed;
            }

            actualStatChange =  actualStatChange * instance.magnitude;

            target.Heal(actualStatChange, origin, showHealingPopup);
        }
    }
}