using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

public enum EffectCategory
{
    Buff = 0,
    Debuff = 1,
    Neutral = 2
}

public class Effect : ScriptableObject
{
    public string effectID;
    [SerializeField] string effectName;
    [SerializeField] EventReference startNoise;
    
    [SerializeField] Sprite effectIcon;
    [SerializeField] Color effectIconColor = Color.white;
    [SerializeField] VisualEffect visualEffectApplied;

    [SerializeField] EffectCategory category = EffectCategory.Neutral;

    [SerializeField] int maxStacks = 1;
    [SerializeField] float duration = 10.0F;
    [SerializeField] bool refreshDurationOnStackChange = true;
    [SerializeField] bool loseAllStacksAtOnce = true;

    public bool endsOnDamaged = false;

    public virtual void OnValidate()
    {
        #if UNITY_EDITOR
            if (effectID == "")
            {
                effectID = GUID.Generate().ToString();
                EditorUtility.SetDirty(this);
            }
        #endif
    }

    public static Effect GetEffect(string effectID)
    {
        Effect[] effects = Resources.LoadAll<Effect>("Effects");
        foreach (Effect effect in effects)
        {
            if (effect.effectID == effectID)
            {
                return effect;
            }
        }

        return null;
    }

    public virtual EffectInstance CreateEffectInstance(Entity origin)
    {
        return new EffectInstance(this, origin);
    }

    public string GetName() { return effectName; }

    public Sprite GetIcon() { return effectIcon; }
    public Color GetIconColor() { return effectIconColor; }

    public VisualEffect GetVisualEffectApplied()
    {
        return visualEffectApplied;
    }

    public virtual void OnEffectStart(EffectInstance instance, Entity target, Entity origin) 
    {
        if (!startNoise.IsNull)
        {
            AudioManager.Singleton.PlayOneShot(startNoise, target.transform.position);
        }
    }

    public virtual void OnEffectEnd(EffectInstance instance, Entity target, Entity origin) 
    {
        // TODO remove visual effect
    }

    public void ClientEffectStart(Effect effect)
    {

    }

    public void ClientEffectEnd(Effect effect)
    {

    }

    public virtual void OnEffectTargetDied(EffectInstance effectInstance)
    {
        
    }

    public virtual void OnEffectTargetDamaged(EffectInstance effectInstance, Entity target, HitInfo damageHitResult)
    {

    }

    public virtual bool CanAddEffect(Entity target)
    {
        return true;
    }

    
    public int GetMaxStacks()
    {
        return maxStacks;
    }
    
    public float GetDuration()
    {
        return duration;
    }

    public bool Refreshes()
    {
        return refreshDurationOnStackChange;
    }

    public bool LoseAllStacksAtOnce()
    {
        return loseAllStacksAtOnce;
    }
}