

using Mirror;
using UnityEngine;

public abstract class Progressable : NetworkBehaviour, IOnLocalPlayerReadyTarget
{
    [Server]
    public void SetProgress(float progress) => this.progress = progress;
    public float GetProgress() => progress;
    [SyncVar] float progress;


    public virtual void Start()
    {
        if (isClient)
        {
            PlayerUI.AddProgressBar(this);
        }
    }

    public void OnLocalPlayerReady(Player player)
    {
        PlayerUI.AddProgressBar(this);
    }
}