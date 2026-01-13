using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Mirror;

public class CurrencyDisplay : MonoBehaviour
{
    [SerializeField] GameObject aetherCounter;
    [SerializeField] GameObject goldCounter;


    [SerializeField] Color goldReceivedFlashColor;
    float lastGoldReceiveTime;
    const float GOLD_RECEIVED_FLASH_FADE_TIME = .25F;

    
    void Awake()
    {
        if (NetworkClient.active)
        {
            Player player = Player.GetLocalPlayer();

            if (player != null && player.GetPlayerData() != null)
            {
                goldCounter.SetActive(player.GetInventory().GetGold() > 0);
                
                aetherCounter.SetActive(player.GetPlayerData().AetherCount > 0);
            }
        }

        Inventory.OnLocalPlayerGoldCountChanged += UpdateGoldCount;
    }

    
    void Update()
    {
        if (!NetworkClient.active)
        {
            return;
        }


        Player player = Player.GetLocalPlayer();

        if (player != null && player.GetPlayerData() != null)
        {
            aetherCounter.SetActive(player.GetPlayerData().AetherCount > 0);
            
            aetherCounter.GetComponentInChildren<TextMeshProUGUI>().text = player.GetPlayerData().AetherCount.ToString();
        }
        
        goldCounter.GetComponentInChildren<TextMeshProUGUI>().color = Color.Lerp(goldCounter.GetComponentInChildren<TextMeshProUGUI>().color, Color.white, (1 / GOLD_RECEIVED_FLASH_FADE_TIME) * Time.deltaTime);
    }

    void UpdateGoldCount(int previousCount, int currentCount)
    {
        goldCounter.SetActive(currentCount > 0);
        goldCounter.GetComponentInChildren<TextMeshProUGUI>().text = currentCount.ToString();

        lastGoldReceiveTime = Time.time;
        goldCounter.GetComponentInChildren<TextMeshProUGUI>().color = goldReceivedFlashColor;

    }

}
