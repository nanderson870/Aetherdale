using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerResourceWidget : MonoBehaviour
{
    [SerializeField] readonly float valueBarChangeSpeed = 0.5F; // percent of bar per second

    [Header("Materials")]
    [SerializeField] Material pulsingMaterial;

    [SerializeField] ControlledEntityResourceWidget wraithWidget;
    [SerializeField] ControlledEntityResourceWidget idolWidget;
    
    [SerializeField] Transform activeHolder;    
    [SerializeField] Transform inactiveHolder;    


    Player trackedPlayer;
    Entity trackedEntity;

    public void Start()
    {
        StartCoroutine(WaitAndInitialize());
        SceneManager.activeSceneChanged += SceneChanged;
    }

    private void SceneChanged(Scene oldScene, Scene newScene)
    {
        // TODO only needed while form is not preserved through areas
        GetComponent<Animator>().SetTrigger("Wraith");
    }

    IEnumerator WaitAndInitialize()
    {
        while (Player.GetLocalPlayer() == null)
        {
            yield return null;
        }

        Player localPlayer = Player.GetLocalPlayer();
        RegisterPlayerCallbacks(localPlayer);
    }

    private void SwapWidgets(ControlledEntity entity)
    {
        if (entity is PlayerWraith)
        {
            wraithWidget.transform.position = activeHolder.position;
            wraithWidget.transform.localScale = activeHolder.localScale;

            
            idolWidget.transform.position = inactiveHolder.position;
            idolWidget.transform.localScale = inactiveHolder.localScale;
        }
        else if (entity is IdolForm)
        {
            idolWidget.transform.position = activeHolder.position;
            idolWidget.transform.localScale = activeHolder.localScale;

            
            wraithWidget.transform.position = inactiveHolder.position;
            wraithWidget.transform.localScale = inactiveHolder.localScale;
        }
    }

    void RegisterPlayerCallbacks(Player player)
    {
        player.OnEntityChangedOnClient += SwapWidgets;
    }

    void UnregisterPlayerCallbacks(Player player)
    {
        player.OnEntityChangedOnClient -= SwapWidgets;
    }

    void OnDestroy()
    {
        if (trackedPlayer != null)
        {
            UnregisterPlayerCallbacks(trackedPlayer);
        }
        SceneManager.activeSceneChanged -= SceneChanged;
    }

}
