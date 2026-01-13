using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;


public class NonPlayerCharacter : Entity
{
    public const float FACE_PLAYER_RANGE = 6.0F;
    
    [SerializeField] NonPlayerCharacterName npcName = NonPlayerCharacterName.Unnamed;
    [SerializeField] string displayName;
    [SerializeField] float rotationSpeed = 3.0F;
    [SerializeField] Transform nameplateTransform;


    [SerializeField] string tooltipDescription = "";

    WorldManager worldManager;


    public override void Start()
    {
        base.Start();
        worldManager = WorldManager.GetWorldManager();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    // Update is called once per frame
    [ServerCallback]
    public override void Update()
    {
        base.Update();

        if (worldManager == null)
        {
            worldManager = WorldManager.GetWorldManager();
            return;
        }
        
        // Face player if they are near
        ControlledEntity nearestPlayerEntity = worldManager.GetNearestControlledEntity(transform.position, FACE_PLAYER_RANGE);

        if (nearestPlayerEntity == null)
        {
            return;
        }

        Vector3 playerPosition = nearestPlayerEntity.transform.position;
        Vector3 lookDirection = (playerPosition - transform.position).normalized;
        lookDirection.y = 0;

        Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);

    }

    public override void Move(Vector3 magnitude)
    {
        throw new NotImplementedException();
    }

    [Server]
    public virtual void Interact(ControlledEntity interactingEntity)
    {
    }

    public virtual bool IsInteractable(ControlledEntity interactingEntity)
    {
        return false;
    }

    public bool IsSelectable()
    {
        return true;
    }
    
    public virtual string GetInteractionPromptText(ControlledEntity interactingEntity)
    {
        return $"Talk to {GetName()}";
    }
    

    protected override void Animate()
    {
    }

    public NonPlayerCharacterName GetNpcName()
    {
        return npcName;
    }

    public Transform GetNamePlateTransform()
    {
        return nameplateTransform != null ? nameplateTransform : transform;
    }

    
    public override Vector3 GetVelocity()
    {
        return Vector3.zero;
    }

    public string GetTooltipTitle(ControlledEntity interactingEntity)
    {
        return GetName();
    }

    public string GetTooltipText(ControlledEntity interactingEntity)
    {
        return tooltipDescription;
    }

}


[Serializable]
public class NpcTopicEntry
{

}