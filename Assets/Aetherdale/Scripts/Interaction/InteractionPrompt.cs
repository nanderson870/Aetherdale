using Mirror;
using UnityEngine;

/*

Add this to an object to give it an interaction range and prompt

*/

public class InteractionPrompt : MonoBehaviour
{
    [SerializeField] float interactRange = 6.0F;
    [SerializeField] IInteractable associatedInteractable;

    void Awake()
    {
        if (!NetworkServer.active)
        {
            Debug.Log("Destroy client-only interaction prompt");
            Destroy(this);
            return;
        }

        if (associatedInteractable == null)
        {
            associatedInteractable = GetComponent<IInteractable>();
        }
    }

    protected void Update()
    {
        foreach (ControlledEntity playerEntity in Player.GetPlayerEntities())
        {
            // We are on the server here, but player entity's interaction prompts are only updated on client
            if (!playerEntity.HasInteractionPrompt(this) && gameObject != playerEntity.gameObject)
            {
                if (Vector3.Distance(transform.position, playerEntity.transform.position) <= interactRange)
                {
                    playerEntity.AddInteractionPrompt(this);
                }
            }
            else
            {
                if (Vector3.Distance(transform.position, playerEntity.transform.position) > interactRange)
                {
                    playerEntity.TargetRemoveInteractionPrompt(this);
                }
            }
        }

    }

    public IInteractable GetInteractable()
    {
        return associatedInteractable;
    }

    public DialogueAgent GetAttachedDialogueAgent()
    {
        return GetComponent<DialogueAgent>();
    }

    public virtual void Interact(ControlledEntity interactingEntity)
    {
        if (GetInteractable() is IInteractable interactable)
        {
            interactable.Interact(interactingEntity);
        }

    }
}
