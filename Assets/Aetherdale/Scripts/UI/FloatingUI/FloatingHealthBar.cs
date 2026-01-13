using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using UnityEngine.Serialization;

public class FloatingHealthBar : FloatingUIElement
{
    [Header("Config")]
    [SerializeField] GameObject mainPanel;
    [SerializeField] Slider healthSlider;
    [SerializeField] TextMeshProUGUI entityNameTMP;

    [SerializeField] TextMeshProUGUI healthText;
    [SerializeField] GameObject healthSliderFill;
    [SerializeField][FormerlySerializedAs("statusesGroup")] Transform effectIconsGroup;
    [SerializeField] Color friendColor;
    [SerializeField] Color enemyColor;

    [Header("Prefabs")]
    [SerializeField] Image iconImagePrefab;


    public float valueBarChangeSpeed = 3.0F; // percent of bar per second
    public float timeVisibleAfterChange = 2.0F;

    Entity owner;


    float healthSliderTargetValue = 1.0F;

    float lastChange;
    bool visible;

    Dictionary<Effect, Image> effects = new();

    public override void Start()
    {
        base.Start();

        if (owner != null)
        {
            // may be created with an entity that has received damage already, use existing values
            healthSlider.value = owner.GetHealthRatio();
        }
        else
        {
            Debug.LogWarning("floating health bar Start() was called without a set owner");
            healthSlider.value = 1.0F;
        }

        transform.localScale = CalculateScaleBasedOnDistance();

        healthSliderTargetValue = healthSlider.value;

        if (visible) // already supposed to be visible, prefab might not be visible though
        {
            healthSlider.gameObject.SetActive(healthSliderTargetValue > 0);
        }
    }

    public override void LateUpdate()
    {
        base.LateUpdate();


        transform.localScale = CalculateScaleBasedOnDistance();
        
        if (owner == null || owner.gameObject == null)
        {
            Destroy(gameObject);
            return;
        }
        
        SetWorldPosition(owner.GetFloatingHealthBarTransform().position);
        
        if (Time.time - lastChange >= timeVisibleAfterChange)
        {
            Hide();
        }

        if (!owner.gameObject.activeSelf)
        {
            Hide();
        }

        if (!IsShown())
        {
            return;
        }
        
        if (healthSlider.value != healthSliderTargetValue)
        {
            healthSlider.value = Mathf.MoveTowards(healthSlider.value, healthSliderTargetValue, valueBarChangeSpeed * Time.deltaTime);
            
            if (healthSlider.value <= 0)
            {
                healthSliderFill.SetActive(false);
            }

        }

        healthText.text = owner.GetCurrentHealth() + " / " +  owner.GetMaxHealth();
    }

    public override void Hide()
    {
        visible = false;

        if (mainPanel != null)
            mainPanel.SetActive(false);

    }

    public override void Show()
    {
        if (!InRange() || !OnScreen())
        {
            return;
        }

        visible = true;
        if (mainPanel != null)
            mainPanel.SetActive(healthSliderTargetValue > 0);
        
        if (entityNameTMP != null)
            entityNameTMP.text = owner.GetDisplayName();

        lastChange = Time.time;
    }

    public override bool IsShown()
    {
        return visible;
    }

    [Client]
    public void SetOwner(Entity owner)
    {
        if (owner != null)
        {
            owner.OnDeathAnimationComplete -= OnOwnerDeath;
            owner.OnStatChanged -= OnEntityStatChanged;
        }

        this.owner = owner;

        if (entityNameTMP != null)
            entityNameTMP.text = owner.GetDisplayName();

        owner.OnDeathAnimationComplete += OnOwnerDeath;
        owner.OnStatChanged += OnEntityStatChanged;

        // Establish friend or enemy color
        Entity localPlayerEntity = NetworkClient.localPlayer.GetComponent<Player>().GetControlledEntity();

        if (localPlayerEntity != null && localPlayerEntity.IsEnemy(owner))
        {
            healthSliderFill.GetComponentInChildren<Image>().color = enemyColor;
        }
        else
        {
            healthSliderFill.GetComponentInChildren<Image>().color = friendColor;
        }

        SetWorldPosition(owner.GetFloatingHealthBarTransform().position);
    }


    public void OnOwnerDeath()
    {
        Destroy(gameObject);
    }

    [ClientCallback]
    public void OnEntityStatChanged(string statName, float value)
    {
        if (owner == null || statName == null || statName == "")
        {
            return;
        }
        
        if (statName.Contains("Health"))
        {
            healthSliderTargetValue = owner.GetHealthRatio();
            healthText.text = owner.GetCurrentHealth() + " / " +  owner.GetMaxHealth();
            lastChange = Time.time;
        }
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

    // TODO standardize this with controlled entity resource widget
    public void AddEffect(EffectInstance instance)
    {
        Image img = Instantiate(iconImagePrefab, effectIconsGroup);
        img.sprite = instance.effect.GetIcon();
        img.color = instance.effect.GetIconColor();

        if (!effects.ContainsKey(instance.effect))
        {
            effects.Add(instance.effect, img);
        }
    }

    public void RemoveEffect(EffectInstance instance)
    {
        if (effects.ContainsKey(instance.effect))
        {
            Image img = effects[instance.effect];
            Destroy(img.gameObject);

            effects.Remove(instance.effect);
        }
    }

}
