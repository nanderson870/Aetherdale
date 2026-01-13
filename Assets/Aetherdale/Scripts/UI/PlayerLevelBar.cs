using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLevelBar : MonoBehaviour
{
    public static void SetLevelAndExperience(int level, ulong experienceThisLevel)
    {
        PlayerLevelBar[] levelBars = FindObjectsByType<PlayerLevelBar>(FindObjectsSortMode.None);

        foreach (PlayerLevelBar levelBar in levelBars)
        {
            levelBar.UpdateLevelAndExperience(level, experienceThisLevel);
        }
    }

    [SerializeField] TextMeshProUGUI currentLevelTMP;
    [SerializeField] TextMeshProUGUI nextLevelTMP;

    [SerializeField] TextMeshProUGUI experienceTMP;

    [SerializeField] Slider expSlider;

    void Start()
    {
        expSlider = GetComponentInChildren<Slider>();
    }

    void UpdateLevelAndExperience(int level, ulong experienceThisLevel)
    {
        currentLevelTMP.text = $"Lvl {level}";
        nextLevelTMP.text = $"Lvl {level + 1}";

        int experienceNeeded = (int) Equation.PLAYER_EXP_PER_LEVEL.Calculate(level);

        expSlider.value = (float) experienceThisLevel / experienceNeeded;

        experienceTMP.text = $"{experienceThisLevel} / {experienceNeeded}";
    }
}
