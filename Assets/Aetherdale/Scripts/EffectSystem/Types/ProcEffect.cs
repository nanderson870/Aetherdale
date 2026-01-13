using UnityEngine;

public abstract class ProcEffect : Effect
{
    [SerializeField] int numberOfProcs = 1;
    [SerializeField] float procInterval = 0;
    
    /// <summary>True if the first proc should be instant, false if it should wait one procInterval first</summary>
    [SerializeField] bool instant = true;
    
    public int GetNumberOfProcs() { return numberOfProcs; }
    public float GetProcInterval() { return procInterval; }
    public bool IsInstant() { return instant; }


    public override EffectInstance CreateEffectInstance(Entity origin)
    {
        return new ProcEffectInstance(this, origin);
    }
    
    public virtual void Proc(EffectInstance instance, Entity target, Entity origin) {}
}