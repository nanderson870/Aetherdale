using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Mirror;
using UnityEngine.Serialization;

public class PlayerEnterAreaTrigger : NetworkBehaviour
{
    [Tooltip("The number of players that must simultaneously be in this area to cause it to trigger")]
    [SerializeField] int requiredPlayers = 1;
    [Tooltip("Once all other conditions are met, wait this long before actually triggering")]
    [SerializeField] float triggerDelay = 1.0F;


    [FormerlySerializedAs("serverEvent")] public UnityEvent triggeredEvent;

    bool triggered = false;
    float timeUntilTrigger;


    // Runtime
    List<Player> playersInArea = new();

    void FixedUpdate()
    {
        if (!triggered && playersInArea.Count >= requiredPlayers)
        {
            timeUntilTrigger -= Time.deltaTime;

            if (timeUntilTrigger <= 0.0F)
            {
                Debug.Log("TRIGGER");
                triggered = true;
                triggeredEvent?.Invoke();
            }
        }
    }
    
    void OnTriggerEnter(Collider collider)
    {
        ControlledEntity entity = collider.gameObject.GetComponent<ControlledEntity>();
        if (entity != null)
        {
            Player player = entity.GetOwningPlayer();
            if (player == null)
            {
                return;
            }


            AddPlayerInArea(player);
        }
    }

    void OnTriggerExit(Collider collider)
    {
        ControlledEntity entity = collider.gameObject.GetComponent<ControlledEntity>();
        if (entity != null)
        {
            Player player = entity.GetOwningPlayer();
            if (player == null)
            {
                return;
            }

            RemovePlayerInArea(player);
        }
    }

    void AddPlayerInArea(Player player)
    {
        if (!playersInArea.Contains(player))
        {
            playersInArea.Add(player);
        }

        // Update time until trigger whenever player list changes
        timeUntilTrigger = triggerDelay;
    }

    void RemovePlayerInArea(Player player)
    {
        if (playersInArea.Contains(player))
        {
            playersInArea.Remove(player);
        }

        // Update time until trigger whenever player list changes
        timeUntilTrigger = triggerDelay;
    }
}
