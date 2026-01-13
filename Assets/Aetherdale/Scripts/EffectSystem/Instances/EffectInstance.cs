using System;
using UnityEngine;

/*
An effect, to be attached to an entity
*/

public class EffectInstance
{
    public Entity target;
    public Entity origin;

    public readonly Effect effect;

    public delegate void EffectAction(Entity target, Entity origin, Vector3 position);
    public event EffectAction OnTargetDeath;

    public Action<EffectInstance> OnEffectStart;
    public Action<EffectInstance> OnEffectEnd;
    public Action<EffectInstance> OnStackChange;

    
    float appliedTime;

    int stacks = 1;
    public float magnitude = 1.0F;


    public EffectInstance(Effect effect, Entity origin)
    {
        this.effect = effect;
        this.origin = origin;
    }

    public void Attach(Entity target)
    {
        this.target = target;
    }

    public int GetNumberOfStacks()
    {
        return stacks;
    }

    public void SetStacks(int number)
    {
        stacks = number;
        OnStackChange?.Invoke(this);

        if (effect.Refreshes())
        {
            appliedTime = Time.time;
        }
    }
    
    public void AddStacks(int number = 1)
    {
        stacks += number;

        OnStackChange?.Invoke(this);

        if (effect.Refreshes())
        {
            appliedTime = Time.time;
        }
    }

    public virtual void RemoveStack()
    {
        if (effect.LoseAllStacksAtOnce() || stacks == 0)
        {
            //Debug.Log("Remove effect instantly");
            target.RemoveEffect(effect);
        }
        else
        {
            appliedTime = Time.time;
            stacks--;
            
            //Debug.Log($"Remove one stack of {effect.GetName()} - now at {stacks}");

            OnStackChange?.Invoke(this);
        }

        if (stacks == 0)
        {
            //Debug.Log("Remove effect because zero stacks");
            target.RemoveEffect(effect);
        }

    }


    // perform actions to setup and teardown the effect
    public virtual void EffectStart()
    {
        //Debug.Log("EffectStart");
        if (target != null)
        {
            target.OnDamaged += TargetEntityHit;
        }

        stacks = 1;
        appliedTime = Time.time;

        effect.OnEffectStart(this, target, origin);
        OnEffectStart?.Invoke(this);

    }

    // this should be called every FixedUpdate
    public virtual void Process()
    {
        if (effect.GetDuration() > 0 && (Time.time - appliedTime) >= effect.GetDuration())
        {
            // Effect has expired, remove stack or remove effect
            RemoveStack();
        }
    }

    public virtual void EffectEnd()
    {
        if (target != null)
        {
            target.OnDamaged -= TargetEntityHit;
        }

        effect.OnEffectEnd(this, target, origin);
        OnEffectEnd?.Invoke(this);
    }

    public virtual void TargetEntityDeath()
    {
        OnTargetDeath?.Invoke(target, origin, target.transform.position);

        effect.OnEffectTargetDied(this);
    }
    

    public virtual void TargetEntityHit(HitInfo hitResult)
    {
        effect.OnEffectTargetDamaged(this, target, hitResult);

        if (effect.endsOnDamaged)
        {
            target.RemoveEffect(effect);
        }
    }

    public void SetMagnitude(float magnitude)
    {
        this.magnitude = magnitude;
    }

}
