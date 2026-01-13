using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trinket : Item
{
    TrinketData data;

    readonly float cooldown;
    readonly List<Effect> effectsApplied = new();

    float lastUse = -30.0F;
    Player owningPlayer;

    public Action<Trinket> OnTrinketUsed;


    public Trinket(TrinketData trinketData) : base(trinketData)
    {
        data = trinketData;
        cooldown = trinketData.GetCooldown();
        effectsApplied = trinketData.GetEffectsApplied();
    }
    
    public void Use(Entity user)
    {
        if (GetCooldownRemaining() > 0)
        {
            return;
        }

        lastUse = Time.time;
        foreach(Effect effect in effectsApplied)
        {
            user.AddEffect(effect, user);
        }

        Projectile projectile = data.GetProjectileReleased();
        if (projectile != null)
        {
            Vector3 from = user.GetWorldPosCenter() + new Vector3(0, 1.5F, 0);
            Vector3 aim = user.GetAimedPosition() - from;
            Vector3 velocity = aim.normalized * data.GetProjectileVelocity();
            Debug.Log(velocity);
            Projectile.Create(projectile, from, user.transform.rotation, user.gameObject, velocity);
        }
    }

    // An unofficial "ClientRpc" - gets called from Rpc on player since this is not a NetworkBehaviour
    public void ClientUsed(Entity user)
    {
        PlayUseSound(user.GetWorldPosCenter());
        OnTrinketUsed?.Invoke(this);
    }

    /// <summary>
    /// Get cooldown duration remaining before next use, in seconds
    /// </summary>
    /// <returns></returns>
    public float GetCooldownRemaining()
    {
        float cooldownElapsed = Time.time - lastUse;

        if (cooldownElapsed >= GetCooldown())
        {
            return 0.0F;
        }

        return GetCooldown() - cooldownElapsed;
    }

    public float GetCooldown()
    {
        if (owningPlayer != null)
        {
            //Debug.Log(cooldown);
            //Debug.Log(owningPlayer.GetStat(Stats.TrinketCooldownMultiplier, 1.0F));
            return cooldown * owningPlayer.GetStat(Stats.TrinketCooldownMultiplier, 1.0F);
        }
        
        return cooldown;
    }

    public void PlayUseSound(Vector3 position)
    {
        if (!data.GetUseSound().IsNull)
        {
            AudioManager.Singleton.PlayOneShot(data.GetUseSound(), position);
        }
    }

    public void SetOwningPlayer(Player player)
    {
        owningPlayer = player;
    }

    public static string Serialize(Trinket trinket)
    {
        return $"{trinket.GetItemID()}|{trinket.GetQuantity()}";
    }

    public static new Trinket Deserialize(string itemString)
    {
        string[] splitItemString = itemString.Split("|");
        TrinketData trinketData = ItemManager.LookupItemData(splitItemString[0]) as TrinketData;
        if (trinketData != null)
        {
            return new Trinket(trinketData);
        }

        throw new System.Exception("Item could not be found for id " + splitItemString[0]);
    }
}