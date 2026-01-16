using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.VFX;


//[ExecuteInEditMode]
public abstract class Elite : MonoBehaviour
{
    const int TRAIT_TOME_DROP_CHANCE = 10;
    const int AETHER_DROP_CHANCE = 0;

    public static System.Type GetElementalEliteType(Element element)
    {
        return element switch
        {
            Element.Fire => typeof(FireElite),
            Element.Nature => typeof(NatureElite),
            Element.Water => typeof(WaterElite),
            Element.Storm => typeof(StormElite),
            Element.Light => typeof(LightElite),
            Element.Dark => typeof(DarkElite),
            _ => null,
        };
    }

    public static void CreateElite(Entity entity, Element element)
    {
        System.Type eliteType = GetElementalEliteType(element);

        entity.gameObject.name = $"{element} Elite - " + entity.gameObject.name;

        if (entity.isServer)
        {
            entity.RpcAddEliteComponent(eliteType.FullName);
        }
    }

    const float ELITE_SCALE_MULT = 1.2F;

    protected Entity entity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public virtual void Start()
    {
        entity = GetComponent<Entity>();
        if (entity == null)
        {
            Debug.LogError("Elite script was not attached to an Entity");
            Destroy(this);
        }

        if (NetworkServer.active)
        {
            entity.OnDeath += OnEntityDeath;
            entity.OnHitEntity += OnHitEntity;
        }

        // Adjust scale
        entity.transform.localScale *= ELITE_SCALE_MULT;

        // Adjust colors
        AdjustDefaultEntityMaterialsColors();
    }

    void AdjustDefaultEntityMaterialsColors()
    {
        Color primaryColor = GetPrimaryColor();
        Color secondaryColor = GetSecondaryColor();

        foreach (RendererMaterialColor rmc in entity.defaultMaterialsColors)
        {
            foreach(MaterialColor mc in rmc.materialsColors)
            {
                if (mc == null)
                {
                    Debug.LogError("MaterialColor was null for elite - " + gameObject);
                    return;
                }
                
                if (mc.eliteOverride == EliteOverrideMode.OverrideWithPrimary)
                {
                    Color.RGBToHSV(primaryColor, out float original_h, out float original_s, out float original_v);
                    Color.RGBToHSV(primaryColor, out float new_h, out float new_s, out float v);

                    Color primaryAdjusted = Color.HSVToRGB(new_h, Mathf.Lerp(original_s, new_s, 0.5F), original_v);

                    mc.color = primaryAdjusted;
                }
                else if (mc.eliteOverride == EliteOverrideMode.OverrideWithSecondary)
                {
                    Color.RGBToHSV(secondaryColor, out float original_h, out float original_s, out float original_v);
                    Color.RGBToHSV(secondaryColor, out float new_h, out float new_s, out float new_v);

                    Color secondaryAdjusted = Color.HSVToRGB(new_h, Mathf.Lerp(original_s, new_s, 0.5F), original_v);

                    mc.color = secondaryAdjusted;
                }
            }
        }

        entity.ResetMaterials();
        entity.ResetColors();

        if (GetAddons().TryGetComponent(out VisualEffect visualEffect) && visualEffect != null)
        {
            SkinnedMeshRenderer[] smrs = GetComponentsInChildren<SkinnedMeshRenderer>();

            // Set the rate of each visual effect according to the total number so we don't go nuts when an entity has a ton of SMRs
            float rate = visualEffect.GetFloat("Rate");
            rate /= smrs.Count();

            foreach (SkinnedMeshRenderer smr in smrs)
            {
                VisualEffect visEffInst = Instantiate(visualEffect, transform);
                visEffInst.SetSkinnedMeshRenderer("SkinnedMeshRenderer", smr);
                visEffInst.SetFloat("Rate", rate);
            }
        }
        else
        {
            GameObject addons = GetAddons();
            Instantiate(addons, transform);
        }
    }

    
    public virtual void OnEntityDeath(Entity entity, Entity killer)
    {
        if (UnityEngine.Random.Range(0, 100) < TRAIT_TOME_DROP_CHANCE)
        {
            DropTraitTome(killer);
        }
    }

    private void GetDropPositionAndVelocity(out Vector3 pos, out Vector3 velocity)
    {
        Vector2 horizOffset = UnityEngine.Random.insideUnitCircle;
        horizOffset.y = Mathf.Abs(horizOffset.y) + 0.1F;
        horizOffset.y = Mathf.Clamp(horizOffset.y, -.9F, .9F);

        pos = transform.position + new Vector3(horizOffset.x, 0, horizOffset.y);
        float verticalVelocity = UnityEngine.Random.Range(1F, 1.5F);
        velocity = transform.TransformVector(new(horizOffset.x, verticalVelocity, horizOffset.y));
    }
    
    void DropTraitTome(Entity killer)
    {
        Vector3 pos, velocity;
        GetDropPositionAndVelocity(out pos, out velocity);

        TraitTome tome = Pickup.Create(AetherdaleData.GetAetherdaleData().traitTomePrefab, pos, 1, true);

        // Trait tome will assign itself a trait automatically if left alone

        tome.GetComponent<Rigidbody>().linearVelocity = velocity;
    }

    
    void DropAether()
    {
        Vector3 pos, velocity;
        GetDropPositionAndVelocity(out pos, out velocity);
        
        AetherdaleData.GetAetherdaleData().aetherItem.Drop(pos, velocity:velocity);
    }




    public abstract string GetElitePrefix();
    public abstract Color GetPrimaryColor();
    public abstract Color GetSecondaryColor();


    public abstract float ModifyDamageWithEliteResistances(float originalDamage, Element damageElement);

    public virtual GameObject GetAddons()
    {
        return null;
    }

    public virtual void OnHitEntity(HitInfo hitResult)
    {
        
    }
    
}
