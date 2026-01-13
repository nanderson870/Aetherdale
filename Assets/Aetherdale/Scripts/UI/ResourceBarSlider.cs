

using UnityEngine;
using UnityEngine.UI;

public class ResourceBarSlider : ResourceBar
{
    [SerializeField] Slider mainSlider;
    [SerializeField] Slider delayedSlider;
    void Awake()
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

        if (!Mathf.Approximately(mainSlider.value, targetValue))
        {
            mainSlider.value = Mathf.Lerp(mainSlider.value, targetValue, RESOURCE_BAR_LERP_SPEED * Time.deltaTime);
        }

        if (!Mathf.Approximately(delayedSlider.value, targetValue))
        {
            delayedSlider.value = Mathf.Lerp(delayedSlider.value, targetDelayedValue, RESOURCE_BAR_LERP_SPEED * Time.deltaTime);
        }
    }

    
    public override void Show()
    {
        mainSlider.enabled = false;
        delayedSlider.enabled = false;
        background.enabled = false;
    }

    public override void Hide()
    {
        mainSlider.enabled = true;
        delayedSlider.enabled = true;
        background.enabled = true;
    }
}