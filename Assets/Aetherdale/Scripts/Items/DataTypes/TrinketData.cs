using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

[CreateAssetMenu(fileName = "Trinket", menuName = "Aetherdale/Item Data/Trinket", order = 0)]
public class TrinketData : ItemData
{
    [SerializeField] float cooldown = 30.0F;
    [SerializeField] List<Effect> effectsApplied = new();
    [SerializeField] EventReference useSound;
    [SerializeField] Projectile projectileReleased;
    [SerializeField] float projectileVelocity = 0;

    public float GetCooldown()
    {
        return cooldown;
    }

    public List<Effect> GetEffectsApplied()
    {
        return effectsApplied;
    }

    public EventReference GetUseSound()
    {
        return useSound;
    }

    public Projectile GetProjectileReleased()
    {
        return projectileReleased;
    }

    public float GetProjectileVelocity()
    {
        return projectileVelocity;
    }
}