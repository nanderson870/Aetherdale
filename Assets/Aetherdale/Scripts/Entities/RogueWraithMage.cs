using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class RogueWraithMage : StatefulCombatEntity
{
    [SerializeField] Projectile mageBlastProjectile;
    [SerializeField] Transform mageBlastSpawnPoint;

    [SerializeField] float mageBlastProjectileSpeed = 10.0F;

    Entity currentAttackTarget;

    [SerializeField] public UnityEvent OnCastTest;

    public override void Attack(Entity target = null)
    {
        SetAttacking();
        currentAttackTarget = target;

        SetAnimatorTrigger("Attack");
    }

    [ServerCallback]
    public void AttackCast()
    {
        SeekingProjectile projectile = (SeekingProjectile) Projectile.FireAtEntityWithPrediction(this, currentAttackTarget, mageBlastProjectile, mageBlastSpawnPoint.position, mageBlastProjectileSpeed);
        projectile.SetTarget(currentAttackTarget.gameObject);

        currentAttackTarget = null;
    }
}