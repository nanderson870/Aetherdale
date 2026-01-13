using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Mirror;
using System;

public class BossCutscene : NetworkBehaviour, IOnLocalPlayerReadyTarget
{

    bool initialized = false;

    public Action OnExited;

    public void Start()
    {
        if (isServer)
        {
            StatefulCombatEntity.SetStatefulCombatEntityGlobalAI(false);
        }

        if (Player.IsLocalPlayerReady && !initialized)
        {
            // Player was ready before we spawned, safe to set up
            InitializeCutscene();
        }
    }

    public void OnLocalPlayerReady(Player player)
    {
        if (!initialized)
        {
            // Player ready after start, safe to set up
            InitializeCutscene();
        }
    }

    public void Update()
    {
        PlayableDirector playableDirector = GetComponent<PlayableDirector>();
        if (playableDirector.state != PlayState.Playing)
        {
            StopCutscene();
        }
    }

    void InitializeCutscene()
    {
        initialized = true;

        PlayableDirector playableDirector = GetComponent<PlayableDirector>();

        Boss boss = FindAnyObjectByType<Boss>();
        PlayerCamera localCam =  PlayerCamera.GetLocalPlayerCamera();

        // Set bindings
        TimelineAsset timelineAsset = playableDirector.playableAsset as TimelineAsset;
        foreach (PlayableBinding output in timelineAsset.outputs)
        {
            if (output.streamName == "Boss Animation Track")
            {
                playableDirector.SetGenericBinding(output.sourceObject, boss.GetComponent<Animator>());
            }
            else if (output.streamName == "Camera Animation Track")
            {
                playableDirector.SetGenericBinding(output.sourceObject, localCam.gameObject.GetComponent<Animator>());
            }
        }

        // Set camera position
        Vector3 offset = boss.transform.TransformDirection(new Vector3(3.0F, 2.0F, 8F));
        localCam.transform.position = boss.GetWorldPosCenter() + offset;
        localCam.transform.LookAt(boss.GetWorldPosCenter());

        
        Player.GetLocalPlayer().CmdSetInCutscene(true);
        

    }

    void StopCutscene()
    {
        if (isServer)
        {
            StatefulCombatEntity.SetStatefulCombatEntityGlobalAI(true);
        }

        OnExited?.Invoke();
        Player.GetLocalPlayer().CmdSetInCutscene(false);
        Destroy(gameObject);
    }
}
