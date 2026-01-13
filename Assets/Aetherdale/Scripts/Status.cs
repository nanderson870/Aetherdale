using UnityEngine;
using UnityEngine.VFX;

[CreateAssetMenu(fileName = "Status", menuName = "Aetherdale/Status", order = 0)]
public class Status : ScriptableObject
{
    [SerializeField] string statusName;
    [SerializeField] Sprite icon;

    
    [SerializeField] StatTarget statTarget;
    [SerializeField] StatValueCalculationMode calculationMode;
    [SerializeField] float statusStrength;

    [SerializeField] VisualEffect visualEffectApplied;


    public string GetName()
    {
        return statusName;
    }

    public Sprite GetIcon()
    {
        return icon;
    }

    public StatTarget GetStatTarget()
    {
        return statTarget;
    }

    public StatValueCalculationMode GetStatValueCalculationMode()
    {
        return calculationMode;
    }

    public float GetStatusStrength()
    {
        return statusStrength;
    }

    public VisualEffect GetVisualEffect()
    {
        return visualEffectApplied;
    }

    public enum StatTarget
    {
        None = 0,
        DamageReceived = 1,
    }
}