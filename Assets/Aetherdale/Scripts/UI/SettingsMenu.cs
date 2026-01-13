using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Aetherdale;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SettingsMenu : Menu
{
    [SerializeField] List<MenuTab> submenuTabs;

    #region General Settings UI
    [SerializeField] Toggle tutorialsToggle;
    [SerializeField] Toggle damageVignetteToggle;
    [SerializeField] Toggle damagePulseToggle;
    #endregion


    #region Controls UI
    [SerializeField] Slider cameraSensitivtySlider;
    [SerializeField] Slider aimedSensitivitySlider;
    [SerializeField] Toggle aimAssistToggle;
    [SerializeField] Slider aimAssistStrengthSlider;
    [SerializeField] Toggle toggleSprintToggle;
    #endregion


    #region Graphics Settings UI
    [SerializeField] TMP_Dropdown resolutionsDropdown;
    [SerializeField] Toggle fullscreenToggle;
    #endregion


    #region Audio Settings UI
    [SerializeField] Slider masterVolumeSlider;
    [SerializeField] Slider musicVolumeSlider;
    [SerializeField] Slider sfxVolumeSlider;
    [SerializeField] Slider ambientVolumeSlider;
    #endregion


    int submenuIndex = 0;


    bool menuInitialized = false;

    bool changeMade = false;

    public override void Open()
    {
        base.Open();

        changeMade = false;

        resolutionsDropdown.ClearOptions();
        for (int i = 0; i < Screen.resolutions.Length; i++)
        {
            
            if (Screen.resolutions[i].refreshRateRatio.value == Screen.currentResolution.refreshRateRatio.value && Screen.resolutions[i].width / Screen.resolutions[i].height == 16 / 9)
            {
                resolutionsDropdown.options.Add(new TMP_Dropdown.OptionData($"{Screen.resolutions[i].width}x{Screen.resolutions[i].height}"));
            }

            if (Screen.resolutions[i].width == Settings.settings.graphicsSettings.resolutionWidth
                && Screen.resolutions[i].height == Settings.settings.graphicsSettings.resolutionHeight)
            {
                resolutionsDropdown.SetValueWithoutNotify(i + 1);
            }
        }


        tutorialsToggle.SetIsOnWithoutNotify(Settings.settings.generalSettings.tutorialsEnabled);
        damageVignetteToggle.SetIsOnWithoutNotify(Settings.settings.generalSettings.damageVignetteEnabled);
        damagePulseToggle.SetIsOnWithoutNotify(Settings.settings.generalSettings.damagePulseEnabled);

        cameraSensitivtySlider.value = Settings.settings.controlsSettings.cameraSensitivity;
        aimedSensitivitySlider.value = Settings.settings.controlsSettings.aimedSensitivity;
        toggleSprintToggle.SetIsOnWithoutNotify(Settings.settings.controlsSettings.toggleSprint);
        aimAssistToggle.SetIsOnWithoutNotify(Settings.settings.controlsSettings.aimAssist);
        aimAssistStrengthSlider.value = Settings.settings.controlsSettings.aimAssistStrength;

        fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);

        masterVolumeSlider.value = Settings.settings.audioSettings.masterVolume;
        musicVolumeSlider.value = Settings.settings.audioSettings.musicVolume;
        sfxVolumeSlider.value = Settings.settings.audioSettings.sfxVolume;
        ambientVolumeSlider.value = Settings.settings.audioSettings.ambientVolume;
        
        menuInitialized = true;

        SelectTab(submenuTabs[0]);
        submenuIndex = 0;
    }

    public override void Close()
    {
        menuInitialized = false;

        if (changeMade)
        {   
            Settings.SaveSettings();
        }

        base.Close();
    }

    public override void ProcessInput()
    {
        if (InputSystem.actions.FindAction("TabRight").WasPressedThisFrame())
        {
            submenuIndex++;

            if (submenuIndex >= submenuTabs.Count)
            {
                submenuIndex = 0;
            }

            SelectTab(submenuTabs[submenuIndex]);
        }
        else if (InputSystem.actions.FindAction("TabLeft").WasPressedThisFrame())
        {
            submenuIndex--;

            if (submenuIndex < 0)
            {
                submenuIndex = submenuTabs.Count - 1;
            }

            SelectTab(submenuTabs[submenuIndex]);
        }
    }
    

    public void SelectTab(MenuTab selectedTab)
    {
        foreach (MenuTab tab in submenuTabs)
        {
            tab.Unselect();
        }

        selectedTab.Select();
        submenuIndex = submenuTabs.IndexOf(selectedTab);
    }

    #region General Settings
    public void DamageVignetteToggled()
    {
        Settings.settings.generalSettings.damageVignetteEnabled = damageVignetteToggle.isOn;
    }

    public void DamagePulseToggled()
    {
        Settings.settings.generalSettings.damagePulseEnabled = damagePulseToggle.isOn;
    }
    #endregion

    #region Controls Settings
    public void CameraSensitivityChanged()
    {
        if (menuInitialized)
        {
            Settings.settings.controlsSettings.cameraSensitivity = cameraSensitivtySlider.value;
        }

        changeMade = true;
    }

    public void AimedSensitivityChanged()
    {
        if (menuInitialized)
        {
            Settings.settings.controlsSettings.aimedSensitivity = aimedSensitivitySlider.value;
        }

        changeMade = true;
    }

    public void ToggleSprintToggled()
    {
        Settings.settings.controlsSettings.toggleSprint = toggleSprintToggle.isOn;
    }

    public void AimAssistToggled()
    {
        Settings.settings.controlsSettings.aimAssist = aimAssistToggle.isOn;
    }

    public void AimAssistStrengthChanged()
    {
        Settings.settings.controlsSettings.aimAssistStrength = aimAssistStrengthSlider.value;
    }

    #endregion

    #region Graphics Settings
    public void ResolutionChanged()
    {
        string resolutionString = resolutionsDropdown.options[resolutionsDropdown.value].text;

        string[] split = resolutionString.Split("x");

        int width = int.Parse(split[0]);
        int height = int.Parse(split[1]);

        Settings.settings.graphicsSettings.SetResolution(width, height);

        changeMade = true;
    }

    public void FullscreenToggled()
    {
        Screen.fullScreen = fullscreenToggle.isOn;
        Settings.settings.graphicsSettings.fullscreen = fullscreenToggle.isOn;
        if (Screen.fullScreen)
        {
            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
        }
        
        changeMade = true;
    }
    #endregion

    #region Audio Settings
    public void VolumeSelectionChanged()
    {
        if (menuInitialized)
        {
            Settings.settings.audioSettings.masterVolume = masterVolumeSlider.value;
            Settings.settings.audioSettings.musicVolume = musicVolumeSlider.value;
            Settings.settings.audioSettings.sfxVolume = sfxVolumeSlider.value;
            Settings.settings.audioSettings.ambientVolume = ambientVolumeSlider.value;
        }

        changeMade = true;
    }
    #endregion

}
