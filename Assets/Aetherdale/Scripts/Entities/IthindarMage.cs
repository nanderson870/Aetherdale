using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class IthindarMage : StatefulCombatEntity
{
    [Header("Attacks")]
    [SerializeField] AreaOfEffect attackAOEPrefab;
    [SerializeField] int attackAOEDamage = 40;
    [SerializeField] Transform attackAOEOrigin;

    [Header("Special Attack")]
    [SerializeField] AreaOfEffect specialExplosion;
    float specialCooldown = 5;
    [SerializeField] int specialMaxRounds = 3; // Spec will be repeated up to this many times while conditions permit
    float specialBurstDelay = 0.01F;


    bool castingSpec = false;
    int currentNumberSpecs = 0;
    float lastSpec = -900;


    public void OnDrawGizmos()
    {
        // foreach (Vector3 pos in GetSpecPositions())
        // {
        //     Gizmos.DrawSphere(pos, 2.2F);
        // }
    }

    public List<Vector3> GetSpecPositions()
    {

        int variant = UnityEngine.Random.Range(0, 4);

        if (variant == 0)
        {
            Vector3[] directions = new Vector3[4];
            directions[0] = transform.forward;
            directions[1] = -transform.forward;
            directions[2] = transform.right;
            directions[3] = -transform.right;
            return GetDirectionalHitPositions(directions);
        }
        else if (variant == 1)
        {
            Vector3[] directions = new Vector3[4];
            directions[0] = (transform.forward + transform.right).normalized;
            directions[1] = (-transform.forward + transform.right).normalized;
            directions[2] = (transform.forward - transform.right).normalized;
            directions[3] = (-transform.forward - transform.right).normalized;
            return GetDirectionalHitPositions(directions);
        }
        else
        {
            return GetRandomHitPositions();
        }
    }

    List<Vector3> GetDirectionalHitPositions(Vector3[] directions)
    {
        List<Vector3> ret = new();


        int iterations = 10;
        float gap = 3F;
        float currentSpacing = 4;
        for (int i = 0; i < iterations; i++)
        {
            foreach (Vector3 direction in directions)
            {
                Vector3 flatPoint = transform.TransformPoint(currentSpacing * direction);

                if (Physics.Raycast(flatPoint + Vector3.up * 5, Vector3.down, out RaycastHit hit, 10, LayerMask.GetMask("Default")))
                {
                    ret.Add(hit.point + Vector3.up * 0.1F);
                }
            }

            currentSpacing += gap;
        }

        return ret;
    }
    
    List<Vector3> GetRandomHitPositions()
    {
        List<Vector3> ret = new();

        int numberOfPositions = 30;
        int range = 25;

        for (int i = 0; i < numberOfPositions; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * range;

            Vector3 position = transform.position + new Vector3(offset.x, 0, offset.y);
            if (Physics.Raycast(position + Vector3.up * 5, Vector3.down, out RaycastHit hit, 10, LayerMask.GetMask("Default")))
            {
                ret.Add(hit.point + Vector3.up * 0.1F);
            }
        }

        return ret;
    }

    public override bool CanAttack(Entity target)
    {
        if (castingSpec)
        {
            return false;
        }

        if (CanSpec(target))
        {
            return true;
        }

        return base.CanAttack(target);
    }

    bool CanSpec(Entity target)
    {
        return !attacking && (castingSpec || Time.time - lastSpec > specialCooldown);
    }

    public override void Attack(Entity target = null)
    {
        if (CanSpec(target))
        {
            SpecialAttack(target);
        }
        else if (CanAttack(target))
        {   
            currentImpactResistance = Mathf.Max(currentImpactResistance, 100);
            base.Attack(target);
        }
        
    }
    
    public void SpecialAttack(Entity target)
    {
        if (!castingSpec)
        {
            currentNumberSpecs = 0;
        }

        if (castingSpec)
        {
            currentNumberSpecs++;
        }

        PlayAnimation("SpecialAttack", 0.05F);
        lastSpec = Time.time;
        lastAttack = Time.time;
        attacking = true;

        if (currentNumberSpecs > specialMaxRounds)
        {
            // Reset
            castingSpec = false;
            currentNumberSpecs = 0;
        }
    }

    public void AttackHit()
    {
        if (isServer)
        {
            AreaOfEffect.AOEProperties properties = AreaOfEffect.Create(attackAOEPrefab, attackAOEOrigin.position, this, HitType.Attack, 30, attackAOEOrigin, useParentRotation: true);
            properties.damage = attackAOEDamage;
        }
    }

    public void SpecialAttackHit()
    {
        if (isServer)
        {
            StartCoroutine(ConjureSpecialTelegraphers(GetSpecPositions()));
        }
    }
    
    IEnumerator ConjureSpecialTelegraphers(List<Vector3> positions)
    {
        foreach (Vector3 position in positions)
        {
            AreaOfEffect.Create(specialExplosion, position, this, HitType.Ability, 30);
            yield return new WaitForSeconds(specialBurstDelay);
        }
    }

}
