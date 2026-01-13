using UnityEngine;
using Mirror;

public class HeldInteractionPrompt : InteractionPrompt
{
    public const float HOLD_TIMEOUT_INTERVAL = 0.25F;
    public const float HOLD_PROGRESS_DURATION = 0.1F;

    public float requiredHoldTime = 3.0F;

    float currentHoldTime = 0; //TODO this was a syncvar before

    ControlledEntity lastInteracter; 
    float lastHoldTime = -10.0F;

    void LateUpdate()
    {
        float timeSinceLastInteraction = Time.time - lastHoldTime;
        if (timeSinceLastInteraction >= HOLD_TIMEOUT_INTERVAL)
        {
            currentHoldTime = 0;
            lastInteracter = null;
        }
        else
        {
            currentHoldTime += Time.deltaTime;
        }

        if (GetProgress() >= 1.0F && timeSinceLastInteraction < HOLD_PROGRESS_DURATION)
        {
            if (GetInteractable() is IInteractable interactable)
            {
                interactable.Interact(lastInteracter);
            }
        }


        if (TryGetComponent(out Progressable progressable))
        {
            progressable.SetProgress(GetProgress());
        }
    }


    public void Hold(ControlledEntity holdingEntity)
    {
        lastInteracter = holdingEntity;
        lastHoldTime = Time.time;
    }


    public override void Interact(ControlledEntity holdingEntity)
    {
        // No-op to prevent instantaneous usage
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>0.0F-1.0F depending on progress (normalized)</returns>
    public float GetProgress()
    {
        return currentHoldTime / requiredHoldTime;
    }

}