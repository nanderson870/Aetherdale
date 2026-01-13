using System;
using System.Collections;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Singleton { get; private set; }

    Bus masterBus;
    Bus musicBus;
    Bus soundEffectsBus;
    Bus ambientBus;

    EventInstance musicEventInstance;

    static float musicTargetIntensity = 0;
    static float musicActualIntensity = 0;


    const float COMBAT_INTENSITY_CHANGERATE = 0.8F;
    const float COMBAT_MUSIC_INTENSITY_TIMEOUT = 10.0F;
    float lastCombatTime = -900;

    int musicTrackIndex = 0;
    const int TRACKS_PER_REGION = 3;


    void Awake()
    {
        if (Singleton != null)
        {
            Destroy(this);
            return;
        }
        Singleton = this;

        masterBus = RuntimeManager.GetBus("bus:/");
        musicBus = RuntimeManager.GetBus("bus:/Music");
        soundEffectsBus = RuntimeManager.GetBus("bus:/Sound Effects");
        ambientBus = RuntimeManager.GetBus("bus:/Ambient");

        SceneManager.activeSceneChanged += OnSceneChanged;

        DontDestroyOnLoad(this);
    }

    void OnDestroy()
    {
        StopMusicTrack();
    }

    void Update()
    {
        masterBus.setVolume(Settings.settings.audioSettings.masterVolume);
        musicBus.setVolume(Settings.settings.audioSettings.musicVolume);
        soundEffectsBus.setVolume(Settings.settings.audioSettings.sfxVolume);
        ambientBus.setVolume(Settings.settings.audioSettings.ambientVolume);
        
        if (Time.time - lastCombatTime <= COMBAT_MUSIC_INTENSITY_TIMEOUT)
        {
            musicTargetIntensity = 1.0F;
        }
        else
        {
            musicTargetIntensity = 0;
        }

        musicActualIntensity = Mathf.MoveTowards(musicActualIntensity, musicTargetIntensity, COMBAT_INTENSITY_CHANGERATE * Time.deltaTime);
        musicEventInstance.setParameterByName("Intensity", musicActualIntensity);
    }

    public void PlayOneShot(EventReference sound, Vector3 worldPos = default)
    {
        if (sound.IsNull)
        {
            //Debug.LogWarning("Sound event reference is null");
            return;
        }

        RuntimeManager.PlayOneShot(sound, worldPos);
    }

    public void StopMusicTrack()
    {
        musicEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    }

    public void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        MusicEntrypoint();
    }

    public void MusicEntrypoint()
    {
        StopMusicTrack();

        // Lookup via FindAnyObjectByType - this only happens once per scene and we can't afford to wait for AreaManager's singleton
        AreaManager areaManager = FindAnyObjectByType<AreaManager>(FindObjectsInactive.Include);

        if (areaManager == null)
        {
            // Debug.LogError("Current scene has no areamanager");
            return;
        }

        try
        {
            StartMusicTrack(areaManager.GetArea().GetMusicTrack(musicTrackIndex));
        }
        catch
        {
            
        }

        musicTrackIndex++;
        if (musicTrackIndex >= TRACKS_PER_REGION)
        {
            musicTrackIndex = 0;
        }
    }

    public void StartMusicTrack(EventReference track)
    {
        StartCoroutine(PlayMusicTrack(track));
    }

    IEnumerator PlayMusicTrack(EventReference track)
    {
        musicEventInstance = RuntimeManager.CreateInstance(track);
        musicEventInstance.start();
        musicEventInstance.release();

        PLAYBACK_STATE playbackState;
        musicEventInstance.getPlaybackState(out playbackState);
        while (playbackState != PLAYBACK_STATE.STOPPED)
        {
            musicEventInstance.getPlaybackState(out playbackState);
            yield return null;
        }

        MusicEntrypoint();
    }

    public static void SetMusicIntensity(float intensity)
    {
        musicTargetIntensity = intensity;
    }

    public static void SetPortalChargeState(float chargeState)
    {
        chargeState = Mathf.Clamp(chargeState, 0.0F, 1.0F);

    }

    public static void UpdateCombatTime()
    {
        AudioManager.Singleton.lastCombatTime = Time.time;
    }
}
