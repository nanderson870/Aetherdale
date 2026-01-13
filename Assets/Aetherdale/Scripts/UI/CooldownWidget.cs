using System.Collections;
using System.Collections.Generic;
using Aetherdale;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CooldownWidget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Color cooldownColor;
    [SerializeField] Color readyColor;

    [SerializeField] Image icon;
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] GameObject unavailableVeil;
    [SerializeField] TextMeshProUGUI cooldownTimerTMP;

    [SerializeField] TextMeshProUGUI quantityTMP;

    TooltipDisplayMode tooltipDisplayMode = TooltipDisplayMode.Fixed;

    string cooldownName;
    string cooldownDescription;
    string flavorDescription;

    bool onCooldown = false;
    float cooldownTimeRemaining;


    void FixedUpdate()
    {
        cooldownTimeRemaining -= Time.deltaTime;
        UpdateCooldownTMP();
    }

    void UpdateCooldownTMP()
    {
        if (cooldownTimeRemaining > 0)
        {
            cooldownTimerTMP.text = "" + (int) cooldownTimeRemaining + "s"; // Change this continuously while cooldown remains
        }
        else
        {
            cooldownTimerTMP.text = "";
            if (onCooldown)
            {
                CompleteCooldown();
            }
        }
    }

    public void SetAvailable(bool available)
    {
        unavailableVeil.SetActive(!available);
    }

    public void SetCooldownRemaining(float cooldownRemaining)
    {
        cooldownTimeRemaining = cooldownRemaining;
        UpdateCooldownTMP();
    }

    public void StartCooldown(float cooldownTime)
    {
        cooldownTimeRemaining = cooldownTime;
        onCooldown = true;
        if (icon != null) icon.color = cooldownColor;
        if (spriteRenderer != null) spriteRenderer.color = cooldownColor;
    }

    void CompleteCooldown()
    {
        onCooldown = false;
        if (icon != null) icon.color = readyColor;
        if (spriteRenderer != null) spriteRenderer.color = readyColor;
    }

    public void SetIcon(Sprite newIcon)
    {
        if (icon != null) icon.sprite = newIcon;
        if (spriteRenderer != null) spriteRenderer.sprite = newIcon;
    }

    public void SetQuantity(int quantity)
    {
        quantityTMP.gameObject.SetActive(true);
        quantityTMP.text = quantity.ToString();
    }

    public void SetInfo(string name, string statsDescription, string flavorDescription = "")
    {
        cooldownName = name;
        cooldownDescription = statsDescription;
        this.flavorDescription = flavorDescription;
    }

    public void SetTooltipDisplayMode(TooltipDisplayMode displayMode)
    {
        tooltipDisplayMode = displayMode;
    }

    public void Reset()
    {
        cooldownTimerTMP.text = "";
        onCooldown = false;
        if (icon != null) icon.color = readyColor;
        if (spriteRenderer != null) spriteRenderer.color = readyColor;
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        Tooltip.SetDisplayMode(tooltipDisplayMode);
        Tooltip.Show(gameObject, cooldownName, cooldownDescription, flavorDescription);
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        Tooltip.Hide();
    }

}
