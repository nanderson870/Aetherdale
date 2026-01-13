
using System;

[System.Serializable]
public class StatChange
{
    public string stat;
    public StatChangeType calcMode;
    public float amount = 0;

    public StatChange(StatChange other)
    {
        stat = other.stat;
        calcMode = other.calcMode;
        amount = other.amount;
    }

    public StatChange(String stat, StatChangeType calcMode, float amount)
    {
        this.stat = stat;
        this.calcMode = calcMode;
        this.amount = amount;
    }

    
    public float GetFinalChangeAmount(float originalStatValue)
    {
        if (calcMode == StatChangeType.Flat)
        {
            return amount;
        }
        else if (calcMode == StatChangeType.Multiplier)
        {
            return amount * originalStatValue;
        }

        throw new Exception("Stat change was not of a supported type: " + ToString());
    }
}


/// <summary>
/// Stacks of a Trait with a stat change add up these bonuses. Multiplier gets 1 added to it to be a multiplier from baseline
/// </summary>
public enum StatChangeType
{
    Flat = 0,
    Multiplier = 1,
}