using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class FallBox : NetworkBehaviour
{
    [ServerCallback]
    void OnTriggerEnter(Collider collider)
    {
        Entity collidingEntity = collider.gameObject.GetComponent<Entity>();

        if (collidingEntity == null)
        {
            return;
        }

        if (collidingEntity is ControlledEntity controlledEntity)
        {
            controlledEntity.TargetSetPosition(AetherdaleNetworkManager.singleton.GetStartPosition().position);
        }
    }
}
