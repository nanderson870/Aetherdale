using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public abstract class FloatingUIElement : MonoBehaviour
{
    int id = -1;

    [SerializeField] FloatingUIElementBehaviorType behaviorType;
    [SerializeField] protected float minScale = 0.05F;
    [SerializeField] protected float maxScale = 3.0F;
    [SerializeField] protected bool centerOnColliders = true;
    protected Vector3 setWorldPosition = Vector3.negativeInfinity;
    protected Vector3 offset = new();

    public virtual bool IsValid()
    {
        return setWorldPosition != null;
    }

    public virtual void Hide()
    {
        //Debug.Log("HIDE");
        gameObject.SetActive(false);
    }

    public virtual void Show()
    {
        gameObject.SetActive(true);
    }

    public virtual bool IsShown()
    {
        return gameObject.activeSelf;
    }
    
    // Call base.Start() if inheriting and using start!
    public virtual void Start()
    {
        transform.SetAsFirstSibling(); // Sets this behind other UI elements

        if (setWorldPosition != Vector3.negativeInfinity)
        {
            // Make sure position is set from start if we have one
            UpdatePosition();
        }
    }

    // Call base.Update() if inheriting and using update!
    public virtual void LateUpdate()
    {
        if (NetworkClient.active)
        {
            if (setWorldPosition == Vector3.negativeInfinity || Camera.main == null)
            {
                Hide();
                return;
            }

            if (!InRange() || !OnScreen())
            {
                Hide();
                // but update position still, no return
            }

            UpdatePosition();
        }
    }

    public void SetOffset(Vector3 offset)
    {
        this.offset = offset;
    }

    void UpdatePosition()
    {
        if (setWorldPosition != Vector3.negativeInfinity)
        {
            transform.position = Camera.main.WorldToScreenPoint(setWorldPosition + offset);
        }
        // Collider trackedCollider = worldPositionTransform.gameObject.GetComponent<Collider>();
        // if (centerOnColliders && trackedCollider != null)
        // {
        //     transform.position = Camera.main.WorldToScreenPoint(trackedCollider.bounds.center + offset);
        // }

        // transform.position = Camera.main.WorldToScreenPoint(worldPositionTransform.position + offset);
    }

    public bool OnScreen()
    {
        if (setWorldPosition == Vector3.negativeInfinity)
        {
            return false;
        }

        Vector3 currentScreenPosition = Camera.main.WorldToScreenPoint(setWorldPosition);
        return currentScreenPosition.x >= 0 && currentScreenPosition.x <= Screen.width 
            && currentScreenPosition.y >= 0 && currentScreenPosition.y <= Screen.height
            && currentScreenPosition.z > 0;
    }

    public virtual bool InRange()
    {
        if (setWorldPosition == Vector3.negativeInfinity)
        {
            return false;
        }

        return GetDistanceFromCamera() <= PlayerUI.floatingUIRenderDistance;
    }

    public float GetDistanceFromCamera()
    {
        return Vector3.Distance(Camera.main.transform.position, setWorldPosition);
    }

    public void SetWorldPosition(Vector3 position)
    {
        setWorldPosition = position;
        transform.position = Camera.main.WorldToScreenPoint(setWorldPosition + offset);
    }

    public FloatingUIElementBehaviorType GetBehaviorType()
    {
        return behaviorType;
    }

    Vector3 CalculateScaleBasedOnDistance()
    {
        float fractionOfSize = 1.0F / (GetDistanceFromCamera() + 1);
        float size = maxScale * fractionOfSize;

        if (size < minScale)
        {
            size = minScale;
        }

        return Vector3.one * size;
    }

    protected virtual void SetScale(float scale)
    {

    }
}
