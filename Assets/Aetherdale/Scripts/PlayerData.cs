using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;


[Serializable]
public class PlayerData
{
    public string username = "";

    public int aetherCount = 0;

    public PlayerUnlockData unlockData = new();

    public string equippedIdolId = "";
    public string equippedWeaponId = ""; 
    public string equippedTrinketId = "";

    public string[] ownedIdols = new string [0];
    public string[] ownedWeapons = new string[0]; // serialized in format <itemId>|<rarity> -- quantity determined by entries in list, weapons are unique by nature

    public string[] ownedTrinkets = new string[0]; 

    public string[] heldQuestItems = new string[0];


    public string trackedQuestId = "";
    public string[] runningQuests = new string[0];
    public string[] completedQuests =  new string[0];


    public string[] heardTopicIds = new string[0];
    public string[] completedTutorials = new string[0];

    public PlayerStatistics statistics = new();

    [Serializable]
    public class PlayerStatistics
    {
        public int runsCompleted = 0;
        public int runsVictorious = 0;
        public int entitiesKilled = 0;
        public int bossesKilled = 0;
    }


    public static PlayerData LoadPlayerData()
    {
        string playerDataFile = Application.persistentDataPath + "/player_data.json";
        PlayerData playerData;

        // Find or create player data
        if (!File.Exists(playerDataFile))
        {
            Debug.Log("InitializePlayerData: No player data found, creating it");

            // No data, create it now
            playerData = new();
        }
        else
        {
            TextReader tr = new StreamReader(playerDataFile);
            string playerDataString = tr.ReadToEnd();
            tr.Close();

            playerData = FromString(playerDataString);
        }

        return playerData;
    }

    public static void SavePlayerData(PlayerData playerData)
    {
        string playerDataFile = Application.persistentDataPath + "/player_data.json";

        string playerDataString = playerData.ToString();
        
        TextWriter tw = new StreamWriter(playerDataFile);
        tw.Write(playerDataString);
        tw.Close();
    }

    public override string ToString()
    {
        return JsonUtility.ToJson(this, prettyPrint:true);
    }

    public static PlayerData FromString(string jsonData)
    {
        return JsonUtility.FromJson<PlayerData>(jsonData);
    }
}
