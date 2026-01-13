using System;
using System.Collections;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class AetherdalePostProcessing : MonoBehaviour
{
    const float DAMAGE_VIGNETTE_START_RATIO = 0.50F; // At what ratio of player health remaining does vignette start showing
    const float DAMAGE_VIGNETTE_MIN_WEIGHT = 0.50F;
    const float DAMAGE_VIGNETTE_MAX_WEIGHT = 0.9F;

    const float PULSE_INTENSITY_INCREASE = 0.5F;
    const float PULSE_INTENSITY_FADE_PER_SECOND = 1.0F;

    [SerializeField] FullScreenPassRendererFeature burningEffect;
    static float burningEffectStrength = 0;

    [SerializeField] Volume damageVignetteVolume;
    [SerializeField] Volume wraithVisionVolume;
    static float pulseIntensityBonus = 0.0F;

    static float damageVignetteTargetWeight = 0.0F;

    float damageVignetteWeightLerpInterpolation = 3.0F;


    float wraithVisionTargetWeight = 0;

    static AetherdalePostProcessing singleton;


    // Start is called before the first frame update
    void Start()
    {
        if (singleton != null)
        {
            Destroy(gameObject);
            return;
        }

        singleton = this;
        DontDestroyOnLoad(gameObject);

        SetSecondaryPostProcessingWeight(0);

        burningEffect.SetActive(true);
        burningEffect.passMaterial.SetFloat("VignetteStrength", 0);
    }

    void OnDestroy()
    {
        burningEffect.SetActive(false);
        burningEffect.passMaterial.SetFloat("_VignetteStrength", 0);
    }

    void Update()
    {
        if (Player.GetLocalPlayer() == null)
        {
            return;
        }

        Entity localPlayerEntity = Player.GetLocalPlayer().GetControlledEntity();
        if (localPlayerEntity == null)
        {
            return;
        }

        damageVignetteTargetWeight = 0;

        if (Settings.settings.generalSettings.damageVignetteEnabled)
        {
            if (localPlayerEntity != null)
            {
                float playerHealth = localPlayerEntity.GetHealthRatio();
                if (playerHealth < DAMAGE_VIGNETTE_START_RATIO)
                {
                    damageVignetteTargetWeight = 1 - (playerHealth / DAMAGE_VIGNETTE_START_RATIO);

                    // convert 0->1 to min->1
                    damageVignetteTargetWeight = damageVignetteTargetWeight * (DAMAGE_VIGNETTE_MIN_WEIGHT / 1.0F) + DAMAGE_VIGNETTE_MIN_WEIGHT;

                    damageVignetteTargetWeight = Mathf.Clamp(damageVignetteTargetWeight, DAMAGE_VIGNETTE_MIN_WEIGHT, DAMAGE_VIGNETTE_MAX_WEIGHT);
                }
            }
        }

        // Apply damage pulse if applicable
        if (Settings.settings.generalSettings.damagePulseEnabled && pulseIntensityBonus > 0)
        {
            pulseIntensityBonus -= PULSE_INTENSITY_FADE_PER_SECOND * Time.deltaTime;
            damageVignetteTargetWeight += pulseIntensityBonus;
            damageVignetteTargetWeight = Mathf.Clamp(damageVignetteTargetWeight, 0, 1.0F);

            // instantly apply a large shift to reflect immediacy of damage 
            damageVignetteVolume.weight = Mathf.Lerp(damageVignetteVolume.weight, damageVignetteTargetWeight, 0.4F);
        }

        if (!Mathf.Approximately(damageVignetteVolume.weight, damageVignetteTargetWeight))
        {
            damageVignetteVolume.weight = Mathf.Lerp(damageVignetteVolume.weight, damageVignetteTargetWeight, damageVignetteWeightLerpInterpolation * Time.deltaTime);
        }


        if (wraithVisionVolume.weight != wraithVisionTargetWeight)
        {
            wraithVisionVolume.weight = Mathf.Lerp(wraithVisionVolume.weight, wraithVisionTargetWeight, 4F * Time.deltaTime);
        }


        float secondaryTargetWeight = 1 - wraithVisionTargetWeight;
        SetSecondaryPostProcessingWeight(secondaryTargetWeight);

        ApplyFireEliteScreenEffects();
    }

    public static void DamagePulse(HitInfo hitResult)
    {
        if (Settings.settings.generalSettings.damagePulseEnabled && hitResult.damageDealt > 0)
        {
            pulseIntensityBonus += PULSE_INTENSITY_INCREASE;
        }
    }

    public static void TurnOnWraithVision()
    {
        SetSecondaryPostProcessingWeight(0);

        singleton.wraithVisionTargetWeight = 1;
    }

    public static void TurnOffWraithVision()
    {
        singleton.wraithVisionTargetWeight = 0;

        SetSecondaryPostProcessingWeight(1);
    }

    /// <summary>
    /// Disables post processing for environment, essentially
    /// </summary>
    static void SetSecondaryPostProcessingWeight(float weight)
    {
        foreach (GameObject gameObject in GameObject.FindGameObjectsWithTag("SecondaryPostProcessing"))
        {
            if (gameObject.TryGetComponent(out Volume volume))
            {
                volume.weight = weight;
            }
        }
    }

    void ApplyFireEliteScreenEffects()
    {
        burningEffectStrength = Mathf.Lerp(burningEffectStrength, burningEffect.passMaterial.GetFloat("_VignetteStrength"), 2.0F * Time.deltaTime);
        if (burningEffectStrength > 0)
        {
            burningEffect.passMaterial.SetFloat("_VignetteStrength", Mathf.Clamp(burningEffectStrength, 0, 2.5F));
        }
        else
        {
            burningEffect.passMaterial.SetFloat("_VignetteStrength", 0);
        }
    }

    public static void AddBurningEffectStrength(float value)
    {
        burningEffectStrength = Mathf.Clamp(burningEffectStrength + value, 0, 3.0F);
    }

    public static void ClearBurningEffectStrength()
    {
        burningEffectStrength = 0;
    }

}
