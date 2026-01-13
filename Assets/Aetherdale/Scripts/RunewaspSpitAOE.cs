using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class RunewaspSpitAOE : AreaOfEffect
{
    const int BASE_DAMAGE = 1;
    const float INTERVAL = 0.25F; //s

    static float hitTimeRemaining = 0;

    protected override void Start()
    {
        base.Start();

        if (Physics.Raycast(transform.position, Vector3.down, out var hitInfo, Mathf.Infinity, LayerMask.GetMask("Default")))
        {
            transform.position = hitInfo.point + new Vector3(0, 0.5F, 0);
        }
    }

    protected override void UpdateAOE()
    {
        hitTimeRemaining -= Time.deltaTime;
    }

    [ServerCallback]
    void OnTriggerStay(Collider collider)
    {
        Entity entity = collider.gameObject.GetComponentInParent<Entity>();
        if (entity == null || !damageDealer.IsEnemy(entity))
        {
            return;
        }

        // Manually ensure entity is near ground
        if (!Physics.Raycast(entity.transform.position, Vector3.down, 1F, LayerMask.GetMask("Default")))
        {
            return;
        }

        if (hitTimeRemaining <= 0)
        {
            entity.Damage(BASE_DAMAGE, Element.Nature, HitType.Ability, damageDealer);

            hitTimeRemaining = INTERVAL;
        }
    }
}
