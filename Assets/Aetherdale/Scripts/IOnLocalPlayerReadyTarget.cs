
using UnityEngine;
using UnityEngine.EventSystems;

public interface IOnLocalPlayerReadyTarget : IEventSystemHandler
{
    GameObject gameObject { get ; } 
    public void OnLocalPlayerReady(Player player);

}