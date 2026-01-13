using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthBar : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI tmpro;
    [SerializeField] ResourceBar resourceBar;
    [SerializeField] float valueBarChangeSpeed = 0.5F; // percent of bar per second

    Boss trackedBoss;
    float healthSliderTargetValue;

    public void Update()
    {
        if (trackedBoss == null)
        {
            Debug.LogWarning("Tracked boss is null for boss health bar");
            return;
        }


    }


    public void SetBoss(Boss boss)
    { 
        trackedBoss = boss;
        boss.OnStatChanged += UpdateStats;

        tmpro.text = trackedBoss.GetDisplayName();
    }

    
    void UpdateStats(string statName, float value)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (statName.Contains("Health"))
        {
            resourceBar.SetValues(trackedBoss.GetCurrentHealth(), trackedBoss.GetMaxHealth());
        }
    }
}
