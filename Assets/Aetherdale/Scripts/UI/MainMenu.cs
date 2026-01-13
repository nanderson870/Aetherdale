using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using FMODUnity;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Mirror;

public class MainMenu : MonoBehaviour
{
    [SerializeField] EventReference mainMenuMusic;

    InputAction uiNavigationInputAction;

    void Start()
    {
        uiNavigationInputAction = InputSystem.actions.FindAction("Navigate");

        //AudioManager.Singleton.StartMusicTrack(mainMenuMusic);
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    void Update()
    {
        if (EventSystem.current.currentSelectedGameObject == null && uiNavigationInputAction.ReadValue<Vector2>() != Vector2.zero)
        {
            EventSystem.current.SetSelectedGameObject(EventSystem.current.firstSelectedGameObject);
        }
    }

    public void Play()
    {
        GameManager.StartGame();
    }

    public void Join()
    {
        GameManager.JoinGame();
    }

    public void Quit()
    {        
        // Stop game in editor if applicable
        #if UNITY_EDITOR
        if(EditorApplication.isPlaying)
        {
            UnityEditor.EditorApplication.isPlaying = false;
            return;
        }
        #endif

        Application.Quit();
    }

    bool RunningViaSteam()
    {
        SteamLobby[] lobbies = FindObjectsByType<SteamLobby>(FindObjectsSortMode.None);
        return lobbies.Length > 0;
    }
}
