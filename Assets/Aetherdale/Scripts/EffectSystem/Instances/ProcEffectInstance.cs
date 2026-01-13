using UnityEngine;

public class ProcEffectInstance : EffectInstance
{
    ProcEffect procEffect;

    readonly float procInterval;

    int procsRemaining;

    float timeUntilNextProc;

    public ProcEffectInstance(ProcEffect effect, Entity origin) : base(effect, origin)
    {
        procEffect = effect;
        procInterval = effect.GetProcInterval();
    }

    // perform actions to setup and teardown the effect
    public override void EffectStart()
    {
        base.EffectStart();

        procsRemaining = procEffect.GetNumberOfProcs();

        if (procEffect.IsInstant())
        {
            // Apply first tick on start if instant
            procEffect.Proc(this, target, origin);
            procsRemaining--;
        }

        timeUntilNextProc = procInterval;

        if (procsRemaining <= 0)
        {
            RemoveStack();
        }
    }

    // this should be called every FixedUpdate
    public override void Process()
    {
        timeUntilNextProc -= Time.deltaTime;

        if (timeUntilNextProc <= 0.0F)
        {
            procEffect.Proc(this, target, origin);
            timeUntilNextProc = procInterval;
            procsRemaining--;
        }


        if (procsRemaining <= 0)
        {
            RemoveStack();
        }
    }

    public override void RemoveStack()
    {
        procsRemaining = procEffect.GetNumberOfProcs();
        base.RemoveStack();
    }
}