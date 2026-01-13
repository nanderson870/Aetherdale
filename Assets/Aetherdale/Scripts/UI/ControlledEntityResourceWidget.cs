using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ControlledEntityResourceWidget : MonoBehaviour
{
    [SerializeField] ResourceBar healthBar;
    [SerializeField] ResourceBar secondaryBar;


    [Header("Prefabs")]
    [SerializeField] Image iconImagePrefab;
    [SerializeField] Transform effectIconsGroup;


    ControlledEntity trackedEntity;

    public void Update()
    {
        if (trackedEntity is IdolForm idolForm)
        {
            if (idolForm.GetDeathTimeout() > 0)
            {
                healthBar.SetColor(Color.grey);
                secondaryBar.SetColor(Color.grey);
            }
            else //if (onRecharge && trackedEntity is IdolForm idolForm2 && idolForm2.GetDeathTimeout() <= 0)
            {
                healthBar.ResetColor();
                secondaryBar.SetColor(idolForm.GetSecondaryResourceColor());
            }
        }
    }

    public void SetTrackedEntity(ControlledEntity controlledEntity)
    {
        // Teardown previous entity
        if (trackedEntity != null)
        {
            trackedEntity.OnStatChanged -= OnStatChanged;

        }

        foreach (Transform child in effectIconsGroup)
        {
            Destroy(child.gameObject);
        }

        trackedEntity = controlledEntity;

        // Setup new entity
        if (trackedEntity != null)
        {
            trackedEntity.OnStatChanged += OnStatChanged;
            trackedEntity.OnEffectsChanged += RefreshEffects;

            if (trackedEntity.HasSecondaryResource())
            {
                secondaryBar.Show();
                secondaryBar.gameObject.SetActive(true);

                secondaryBar.SetColor(trackedEntity.GetSecondaryResourceColor());
            }
            else
            {
                secondaryBar.Hide();
            }

            RefreshEffects();
        }
    }

    public void RefreshEffects()
    {
        foreach (Transform child in effectIconsGroup)
        {
            Destroy(child.gameObject);
        }

        foreach (EffectInstance effectInstance in trackedEntity.GetActiveEffects())
        {
            AddEffect(effectInstance);
        }
    }
    
    public void AddEffect(EffectInstance instance)
    {
        Image img = Instantiate(iconImagePrefab, effectIconsGroup);
        img.sprite = instance.effect.GetIcon();
        img.color = instance.effect.GetIconColor();

        TextMeshProUGUI tmp = img.GetComponentInChildren<TextMeshProUGUI>();
        tmp.text = instance.GetNumberOfStacks().ToString();
    }

    void OnStatChanged(string statName, float value)
    {
        if (statName == null || statName == "")
        {
            return;
        }

        if (statName == "CurrentHealth" || statName == "MaxHealth")
        {
            healthBar.SetValues((int) trackedEntity.GetStat(Stats.CurrentHealth), (int) trackedEntity.GetStat(Stats.MaxHealth));
        }
        else if (statName.Contains("Energy"))
        {
            // TODO FIX
            secondaryBar.SetValues((int) trackedEntity.GetStat(Stats.CurrentEnergy), (int) trackedEntity.GetStat(Stats.MaxEnergy));
        }
    }
}