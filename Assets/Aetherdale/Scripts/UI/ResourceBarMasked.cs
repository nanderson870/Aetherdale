using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceBarMasked : ResourceBar
{
    [SerializeField] RectMask2D mainSliderMask;
    [SerializeField] RectMask2D delayedSliderMask;

    void Start()
    {
        valuesTMP.text = "";
        defaultColor = fillImage.color;
    }


    void Update()
    {
        if (Time.time - lastDelayBarDesyncTime >= DELAYED_BAR_DELAY)
        {
            targetDelayedValue = targetValue;
        }

        float mainValue = Mathf.Clamp01(1 - (mainSliderMask.padding.z / GetMaskWidth()));
        if (!Mathf.Approximately(mainValue, targetValue))
        {
            float newVal = Mathf.Lerp(mainValue, targetValue, RESOURCE_BAR_LERP_SPEED * Time.deltaTime);
            mainSliderMask.padding = new
            (
                x:mainSliderMask.padding.x,
                y:mainSliderMask.padding.y,
                z:(1 - newVal) * GetMaskWidth(),
                w:mainSliderMask.padding.w
            );
        }
        
        
        float delayedValue = Mathf.Clamp01(1 - (delayedSliderMask.padding.z / GetMaskWidth()));
        if (!Mathf.Approximately(delayedValue, targetValue))
        {
            float newVal = Mathf.Lerp(delayedValue, targetValue, RESOURCE_BAR_LERP_SPEED * Time.deltaTime);
            delayedSliderMask.padding = new
            (
                x:delayedSliderMask.padding.x,
                y:delayedSliderMask.padding.y,
                z:(1 - newVal) * GetMaskWidth(),
                w:delayedSliderMask.padding.w
            );
        }
    }


    public override void Show()
    {
        mainSliderMask.gameObject.SetActive(true);
    }

    public override void Hide()
    {
        mainSliderMask.gameObject.SetActive(false); 
    }

    public float GetMaskWidth()
    {
        return mainSliderMask.GetComponent<RectTransform>().rect.width;
    }
}
