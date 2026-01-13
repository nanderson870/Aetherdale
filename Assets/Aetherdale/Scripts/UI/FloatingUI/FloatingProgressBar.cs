using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using UnityEngine.Serialization;

public class FloatingProgressBar : FloatingUIElement
{
    [Header("Config")]
    [SerializeField] Slider progressSlider;
    [SerializeField] GameObject progressSliderFill;

    public float valueBarChangeSpeed = 3.0F; // percent of bar per second
    public float timeVisibleAfterChange = 2.0F;

    float progressTargetValue = 1.0F;

    float lastChange;
    bool visible;

    Progressable progressable;

    Dictionary<Effect, Image> effects = new();

    public override void Start()
    {
        base.Start();

        if (progressable != null)
        {
            // may be created with an entity that has received damage already, use existing values
            progressSlider.value = progressable.GetProgress();
        }
        else
        {
            Debug.LogWarning("floating progress bar Start() was called without a set progressable");
            progressSlider.value = 1.0F;
        }

        transform.localScale = CalculateScaleBasedOnDistance();

        progressTargetValue = progressSlider.value;

        if (visible) // already supposed to be visible, prefab might not be visible though
        {
            progressSlider.gameObject.SetActive(progressTargetValue > 0);
        }
    }

    public override void LateUpdate()
    {
        base.LateUpdate();

        if (progressable == null)
        {
            Debug.Log("Destroy floating progress bar because progressable was null");
            Destroy(gameObject);
            return;
        }

        //Debug.Log(IsShown());
        if (OnScreen() && InRange() && progressable.gameObject.activeSelf)
        {
            Show();
        }
        else
        {
            Hide();
        }

        if (!IsShown())
        {
            return;
        }


        transform.localScale = CalculateScaleBasedOnDistance();

        progressTargetValue = progressable.GetProgress();
        if (progressTargetValue > 0)
        {
            lastChange = Time.time;
        }
        
        
        if ((Time.time - lastChange) >= timeVisibleAfterChange)
        {
            Hide();
        }


        if (progressSlider.value != progressTargetValue)
        {
            if (progressTargetValue == 0)
            {
                progressSlider.value = 0;
            }
            else
            {
                progressSlider.value = Mathf.MoveTowards(progressSlider.value, progressTargetValue, valueBarChangeSpeed * Time.deltaTime);
            }

        }
    }

    public override void Hide()
    {
        visible = false;

        progressSlider.gameObject.SetActive(false);
    }

    public override void Show()
    {
        if (!InRange() || !OnScreen())
        {
            return;
        }

        visible = true;
        progressSlider.gameObject.SetActive(true);
    }

    public override bool IsShown()
    {
        return visible;
    }

    public void SetProgressable(Progressable progressable)
    {
        this.progressable = progressable;
    }


    public void OnHeldInteractionPromptDeath()
    {
        Destroy(gameObject);
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
}
