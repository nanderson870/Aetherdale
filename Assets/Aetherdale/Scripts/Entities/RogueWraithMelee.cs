using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RogueWraithMelee : StatefulCombatEntity
{
    [SerializeField] Hitbox slashHitbox;
    [SerializeField] int slashDamage = 12;
    [SerializeField] Hitbox stabHitbox;
    [SerializeField] int stabDamage = 18;
    
    string[] attackTriggers = {"StabAttack", "SlashAttack"};

    public override void Attack(Entity target = null)
    {
        AudioManager.Singleton.PlayOneShot(attackSound, transform.position);
        lastAttack = Time.time;
        SetAnimatorTrigger(attackTriggers[Random.Range(0, attackTriggers.Length)]);
        SetAttacking();
    }

    // Called by animator when slash strikes
    public void AttackSlashHit()
    {
        slashHitbox.HitOnce(slashDamage, Element.Physical, this, impact:50);
    }

    // Called by animator when stab strikes
    public void AttackStabHit()
    {
        stabHitbox.HitOnce(stabDamage, Element.Physical, this, impact:50);
    }
}
