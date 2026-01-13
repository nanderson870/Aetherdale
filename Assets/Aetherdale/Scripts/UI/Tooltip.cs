using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.InputSystem;

public class Tooltip : MonoBehaviour
{
    [SerializeField] Vector2 padding;

    [SerializeField] ItemDescriptionPanel itemDescriptionPanel;

    TooltipDisplayMode displayMode = TooltipDisplayMode.Fixed;

    Vector3 defaultPosition;
    GameObject hoverObject;

    public static void Show(GameObject hoverObject, Item item)
    {
        Tooltip tooltip = FindObjectsByType<Tooltip>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];

        tooltip.hoverObject = hoverObject;
        tooltip.gameObject.SetActive(true);
        tooltip.itemDescriptionPanel.SetItem(item);
        tooltip.RefreshPosition();
    }

    public static void Show(GameObject hoverObject, ShopOfferingInfo offeringInfo)
    {
        Tooltip tooltip = FindObjectsByType<Tooltip>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];

        tooltip.hoverObject = hoverObject;
        tooltip.gameObject.SetActive(true);
        tooltip.itemDescriptionPanel.SetShopOffering(offeringInfo);
        tooltip.RefreshPosition();
    }


    public static void Show(GameObject hoverObject, string title, string description, string flavorDescription = "")
    {
        Tooltip tooltip = FindObjectsByType<Tooltip>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];

        tooltip.hoverObject = hoverObject;
        tooltip.gameObject.SetActive(true);
        tooltip.itemDescriptionPanel.SetFields(title, description, flavorDescription);
        tooltip.RefreshPosition();
    }

    public static void Hide()
    {
        Tooltip tooltip = FindObjectsByType<Tooltip>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
        
        tooltip.gameObject.SetActive(false);
        tooltip.displayMode = TooltipDisplayMode.Fixed;
    }

    public static void SetDisplayMode(TooltipDisplayMode displayMode)
    {
        Tooltip tooltip = FindObjectsByType<Tooltip>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];

        tooltip.displayMode = displayMode;
    }

    void Awake()
    {
        defaultPosition = transform.position;
        Hide();
    }

    public void Update()
    {
        RefreshPosition();

        if (InputSystem.actions.FindAction("Cancel").WasPressedThisFrame())
        {
            gameObject.SetActive(false);
        }
    }

    public void RefreshPosition()
    {
        if (displayMode == TooltipDisplayMode.Fixed)
        {
            transform.position = defaultPosition;
        }
        // else if (displayMode == TooltipDisplayMode.Hover)
        // {
        //     if (hoverObject == null)
        //     {
        //         return;
        //     }

        //     if (hoverObject.layer == LayerMask.NameToLayer("UI"))
        //     {
        //         HoverUIElement();
        //     }
        //     else
        //     {
        //         HoverWorldObject();
        //     }
            
        // }

    }

    void HoverUIElement()
    {
        // // Object in world space
        // RectTransform rectTransform = GetComponent<RectTransform>();
        // Vector3 screenPos = hoverObject.transform.position;
        // RectTransformUtility.ScreenPointToLocalPointInRectangle(transform.parent.GetComponent<RectTransform>(), screenPos, null, out Vector2 initialLocalRectPoint);
        // transform.localPosition = initialLocalRectPoint;
        
        // Vector3 screenPosOffset = (Vector3) (new Vector2(rectTransform.rect.width / 2, 0) + padding);
        // if (initialLocalRectPoint.x > rectTransform.rect.width * 0.9F)
        // {
        //     // Flip tooltip to stay towards center of screen
        //     screenPosOffset *= -1;
        // }

        // transform.localPosition = (Vector3) initialLocalRectPoint + screenPosOffset;
    }

    void HoverWorldObject()
    {
        /*
        RectTransform rectTransform = GetComponent<RectTransform>();

        Vector3 screenPosOffset = (Vector3) (new Vector2(rectTransform.rect.width / 2, 0) + padding);
        
        Vector3 screenPos = Camera.main.WorldToScreenPoint(hoverObject.transform.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(transform.parent.GetComponent<RectTransform>(), screenPos, null, out Vector2 initialLocalRectPoint);
        if (initialLocalRectPoint.x > rectTransform.rect.width * 0.9F)
        {
            // Flip tooltip to stay towards center of screen
            screenPosOffset *= -1;
        }
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(transform.parent.GetComponent<RectTransform>(), screenPos + screenPosOffset, Camera.main, out Vector2 finalLocalRectPoint);

        transform.position = screenPos;
        */
    }
}


public enum TooltipDisplayMode
{
    Fixed = 0,
    Hover = 1
}