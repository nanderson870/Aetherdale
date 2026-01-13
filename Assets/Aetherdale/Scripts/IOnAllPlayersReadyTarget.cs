
using UnityEngine;
using UnityEngine.EventSystems;

public interface IOnAllPlayersReadyTarget : IEventSystemHandler
{
    GameObject gameObject { get ; } 
    public void OnAllPlayersReady();

}