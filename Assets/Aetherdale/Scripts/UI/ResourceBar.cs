using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class ResourceBar : MonoBehaviour
{
    protected const float RESOURCE_BAR_LERP_SPEED = 12.0F;
    protected const float DELAYED_BAR_DELAY = 1F;

    [SerializeField] protected Image fillImage;

    [SerializeField] protected Image background;
    [SerializeField] protected TextMeshProUGUI valuesTMP;


    protected float targetDelayedValue = 1.0F;
    protected float targetValue = 1.0F;

    protected float lastDelayBarDesyncTime = 0;


    public Color defaultColor;

    public void SetColor(Color color)
    {
        fillImage.color = color;
    }

    public void ResetColor()
    {
        fillImage.color = defaultColor;
    }


    public void SetValues(int current, int max)
    {
        float newTargetValue = (float) current / max;

        if (newTargetValue < targetValue)
        {
            // Resource expended, prevents delay bar changing on regen etc
            targetDelayedValue = targetValue;
            lastDelayBarDesyncTime = Time.time;
        }

        targetValue = newTargetValue;

        if (valuesTMP != null)
        {
            valuesTMP.text = $"{current} / {max}";
        }
    }

    public abstract void Show();
    public abstract void Hide();

}
