using System.IO;
using UnityEngine;

[System.Serializable]
public class Settings
{
    public static Settings settings;
    public delegate void SettingsLoadedAction(Settings settings);
    public static event SettingsLoadedAction OnSettingsLoaded;

    public GeneralSettings generalSettings = new();
    public ControlsSettings controlsSettings = new();
    public GraphicsSettings graphicsSettings = new();
    public AudioSettings audioSettings = new();

    public static void InitSettings()
    {
        string settingsFile = Application.persistentDataPath + "/settings.json";

        if (!File.Exists(settingsFile))
        {
            // Create settings for first time
            settings = new();

            SaveSettings();
        }
        else
        {
            // Load existing settings
            TextReader tr = new StreamReader(settingsFile);
            string settingsString = tr.ReadToEnd();

            settings = JsonUtility.FromJson<Settings>(settingsString);
            tr.Close();
        }

        Screen.fullScreen = settings.graphicsSettings.fullscreen;
        settings.graphicsSettings.SetResolution(settings.graphicsSettings.resolutionWidth, settings.graphicsSettings.resolutionHeight);

        OnSettingsLoaded?.Invoke(settings);
    }


    public static void SaveSettings()
    {
        string settingsFile = Application.persistentDataPath + "/settings.json";
        string settingsString = JsonUtility.ToJson(settings, prettyPrint:true);
        
        TextWriter tw = new StreamWriter(settingsFile);
        tw.Write(settingsString);
        tw.Close();

        OnSettingsLoaded?.Invoke(settings);
    }


    [System.Serializable]
    public class GeneralSettings
    {
        public bool tutorialsEnabled = true;
        public bool damageVignetteEnabled = true;
        public bool damagePulseEnabled = true;
    }

    [System.Serializable]
    public class ControlsSettings
    {
        public float cameraSensitivity = 3.0F;
        public float aimedSensitivity = 1.2F;
        public bool aimAssist = true;
        public float aimAssistStrength = 0.5F;
        public bool toggleSprint = true;
    }

    [System.Serializable]
    public class GraphicsSettings
    {
        public bool fullscreen = true;
        public int resolutionWidth = 1920;
        public int resolutionHeight = 1080;

        public void SetResolution(int width, int height)
        {
            resolutionWidth = width;
            resolutionHeight = height;

            Screen.SetResolution(width, height, fullscreen);
        }
    }
    
    [System.Serializable]
    public class AudioSettings
    {
        public float masterVolume = 1.0F;
        public float musicVolume = 0F;
        public float sfxVolume = 0.8F;
        public float ambientVolume = 0.8F;
    }

}
