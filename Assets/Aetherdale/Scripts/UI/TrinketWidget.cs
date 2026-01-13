

using System.Collections;
using UnityEngine;

public class TrinketWidget : CooldownWidget
{
    void Start()
    {
        StartCoroutine(WaitAndInitialize());
    }

    IEnumerator WaitAndInitialize()
    {
        while (Player.GetLocalPlayer() == null)
        {
            yield return null;
        }

        Player localPlayer = Player.GetLocalPlayer();
        RegisterPlayerCallbacks(localPlayer);

        SetTrinket(localPlayer.GetTrinket());

    }

    void RegisterPlayerCallbacks(Player player)
    {
        player.OnTrinketEquippedOnClient += SetTrinket;
        Inventory inventory = player.GetInventory();
    }


    void UnregisterPlayerCallbacks(Player player)
    {
        player.OnTrinketEquippedOnClient -= SetTrinket;
    }

    public void SetTrinket(Trinket trinket)
    {
        Debug.Log("NEW TRINKET");
        if (trinket == null)
        {
            gameObject.SetActive(false);
            return;
        }
        
        trinket.OnTrinketUsed += (Trinket usedTrinket) => {StartCooldown(usedTrinket.GetCooldown());};
        gameObject.SetActive(true);
        SetIcon(trinket.GetIcon());
        SetInfo(trinket.GetName(), trinket.GetStatsDescription(), trinket.GetDescription());
    }


}