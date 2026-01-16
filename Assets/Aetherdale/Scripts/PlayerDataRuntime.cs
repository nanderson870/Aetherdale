
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class PlayerDataRuntime
{
    public string Username {get; private set;} = "";

    public int AetherCount {get; private set;} = 0;

    public PlayerUnlockData UnlockData {get; private set;} = new();

    public IdolItem EquippedIdol {get; private set;}
    public WeaponData EquippedWeapon {get; private set;}
    public Trinket EquippedTrinket {get; private set;}

    public List<IdolItem> OwnedIdols {get; private set;} = new();
    public List<WeaponData> OwnedWeapons {get; private set;} = new();
    public List<Trinket> OwnedTrinkets{get; private set;} = new();

    public List<Item> HeldQuestItems{get; private set;} = new();

    public Quest TrackedQuest {get; private set;}
    public List<Quest> RunningQuests {get; private set;} = new();
    public List<Quest> CompletedQuests {get; private set;} = new();

    public List<DialogueTopic> HeardTopics { get; private set; } = new();

    public List<string> CompletedTutorialNames { get; private set; } = new();

    public PlayerData.PlayerStatistics statistics = new();


    // Events
    public static Action<Quest> OnTrackedQuestChanged;
    public static Action<Quest> OnTrackedQuestCleared;
    public static Action<Quest> OnQuestCompleted;


    #region SETUP TEARDOWN
    PlayerDataRuntime()
    {

    }

    PlayerDataRuntime(PlayerData playerData)
    {
        Username = playerData.username;
        AetherCount = playerData.aetherCount;

        UnlockData = playerData.unlockData;

        // Initialize Idols -------------------------------
        foreach (string serializedIdol in playerData.ownedIdols)
        {
            IdolItem idolItem = IdolItem.Deserialize(serializedIdol);
            OwnedIdols.Add(idolItem);

            if (idolItem.GetItemID() == playerData.equippedIdolId)
            {
                EquippedIdol = idolItem;
            }
        }
        // -------------------------------------------------


        // Initialize Weapons -----------------------------
        foreach (string weaponID in playerData.ownedWeapons)
        {
            WeaponData weapon = (WeaponData) ItemManager.LookupItemData(weaponID.Split("|")[0]); // prevent old weapon "serializations" spilling in
            OwnedWeapons.Add(weapon);
        }

        EquippedWeapon = (WeaponData)ItemManager.LookupItemData(playerData.equippedWeaponId.Split("|")[0]);
        // -------------------------------------------------


        // Initialize Trinkets -----------------------------
        foreach (string serializedTrinket in playerData.ownedTrinkets)
        {
            Trinket trinket = Trinket.Deserialize(serializedTrinket);
            OwnedTrinkets.Add(trinket);

            if (trinket.GetItemID() == playerData.equippedTrinketId)
            {
                EquippedTrinket = trinket;
            }
        }
        // -------------------------------------------------

        
        // Initialize Quest Items --------------------------
        foreach (string heldQuestItem in playerData.heldQuestItems)
        {
            Item item = Item.Deserialize(heldQuestItem);
            HeldQuestItems.Add(item);
        }
        // -------------------------------------------------


        // Initialize Quests -------------------------------

        foreach (string serializedQuest in playerData.runningQuests)
        {
            Quest runningQuest = Quest.Deserialize(serializedQuest);

            if (runningQuest.GetID() == playerData.trackedQuestId)
            {
                TrackedQuest = runningQuest;
            }
            
            AddQuest(runningQuest, allowImmediateTrack:false);
        }

        foreach (string serializedQuest in playerData.completedQuests)
        {
            Quest completedQuest = Quest.Deserialize(serializedQuest);

            CompletedQuests.Add(completedQuest);
        }
        // -------------------------------------------------


        // Initialize Heard Dialogue -----------------------
        foreach (string heardTopicID in playerData.heardTopicIds)
        {
            DialogueTopic topic = DialogueManager.LookupTopic(heardTopicID);

            HeardTopics.Add(topic);
        }
        // -------------------------------------------------


        // Statistics --------------------------------------
        statistics = playerData.statistics;
        // -------------------------------------------------
    }

    public static PlayerDataRuntime Load()
    {
        PlayerData diskPlayerData = PlayerData.LoadPlayerData();

        PlayerDataRuntime newPlayerDataRuntime = new(diskPlayerData);

        return newPlayerDataRuntime;
    }

    public static void Save(PlayerDataRuntime playerDataRuntime)
    {
        PlayerData playerData = playerDataRuntime.ToPlayerData();
        PlayerData.SavePlayerData(playerData);
    }

    public PlayerData ToPlayerData()
    {
        PlayerData playerData = new()
        {
            username = Username,
            aetherCount = AetherCount,

            unlockData = UnlockData,

            equippedIdolId = EquippedIdol != null ? EquippedIdol.GetItemID() : "",
            equippedWeaponId = EquippedWeapon != null ? EquippedWeapon.GetItemID() : "",
            equippedTrinketId = EquippedTrinket != null ? EquippedTrinket.GetItemID() : "",

            ownedIdols = OwnedIdols.Select(idol => IdolItem.Serialize(idol)).ToArray(),
            ownedWeapons = OwnedWeapons.Select(weapon => weapon.GetItemID()).ToArray(),
            ownedTrinkets = OwnedTrinkets.Select(trinket => Trinket.Serialize(trinket)).ToArray(),

            heldQuestItems = HeldQuestItems.Select(questItem => Item.Serialize(questItem)).ToArray(),

            trackedQuestId = TrackedQuest != null ? TrackedQuest.GetID() : "",

            runningQuests = RunningQuests.Select(quest => Quest.Serialize(quest)).ToArray(),
            completedQuests = CompletedQuests.Select(quest => Quest.Serialize(quest)).ToArray(),

            heardTopicIds = HeardTopics.Select(topic => topic.GetID()).ToArray(),
            statistics = statistics
        };

        return playerData;
    }

    public void RegisterCallbacks(Player player)
    {
        player.OnStoryEvent += StoryEventReceived;

        player.OnEntityKilled += EntityKilled;

        AreaSequencer.OnRunEnded += RunEnded;
    }

    public void UnregisterCallbacks(Player player)
    {
        player.OnStoryEvent -= StoryEventReceived;

        player.OnEntityKilled -= EntityKilled;

        AreaSequencer.OnRunEnded -= RunEnded;
    }

    public void RunEnded(bool victory)
    {
        statistics.runsCompleted++;

        if (victory)
        {
            statistics.runsVictorious++;
        }
    }

    public void EntityKilled(HitInfo killHit)
    {
        statistics.entitiesKilled++;

        if (killHit.entityHit is Boss)
        {
            statistics.bossesKilled++;
        }
    }

    void StoryEventReceived(string storyEvent)
    {
        if (storyEvent == "IdolsUnlocked")
        {
            Debug.Log("IDOLS UNLOCKED");
            UnlockData.idolsUnlocked = true;
        }

        if (storyEvent == "TrinketsUnlocked")
        {
            Debug.Log("TRINKETS UNLOCKED");
            UnlockData.trinketsUnlocked = true;
        }

        if (storyEvent  == "WraithVisionUnlocked")
        {
            Debug.Log("WRAITH VISION UNLOCKED");
            UnlockData.wraithVisionUnlocked = true;
        }
    }
    #endregion


    #region ACCOUNT
    public void SetUsername(string username)
    {
        Username = username;
    }

    public void AddAether(int quantity)
    {
        AetherCount += quantity;
    }



    /// <summary>
    /// Remove <paramref name="quantity"/> Aether from account. 
    /// </summary>
    /// <param name="quantity"></param>
    /// <returns>false if the player does not have that much</returns>
    public bool TryRemoveAether(int quantity)
    {
        if (AetherCount < quantity)
        {
            return false;
        }

        AetherCount -= quantity;

        return true;
    }

    #endregion


    #region ITEMS
    public void AddAccountItem(string itemId, int quantity)
    {
        ItemData itemData = ItemManager.LookupItemData(itemId);
        if (itemData == null)
        {
            Debug.LogError("Could not add item " + itemId + " to account");
            return;
        }

        if (itemData is WeaponData weaponData)
        {
            foreach (WeaponData ownedWeapon in OwnedWeapons)
            {
                if (ownedWeapon.GetItemID() == itemId)
                {
                    return;
                }
            }

            OwnedWeapons.Add(weaponData);
        }
        else if (itemData is TrinketData trinketData)
        {
            foreach (Trinket ownedTrinket in OwnedTrinkets)
            {
                if (ownedTrinket.GetItemID() == itemId)
                {
                    return;
                }
            }

            OwnedTrinkets.Add(new(trinketData));

        }
        else if (itemData is IdolItemData idolData)
        {
            foreach (IdolItem ownedIdol in OwnedIdols)
            {
                if (ownedIdol.GetItemID() == itemId)
                {
                    return;
                }
            }

            OwnedIdols.Add(new(idolData));
        }
        else if(itemData.IsQuestItem())
        {
            if (HasItem(itemData.GetItemID()))
            {
                if (!itemData.IsUnique())
                {
                    HeldQuestItems.Find(x => x.GetItemID() == itemData.GetItemID()).AddQuantity(quantity);
                }
            }
            else
            {
                HeldQuestItems.Add(new(itemData));
            }
        }

    }

    public bool HasItem(string itemId)
    {
        ItemData itemData = ItemManager.LookupItemData(itemId);

        if (itemData.GetName() == "Aether")
        {
            return AetherCount > 0;
        }
        else if (itemData is WeaponData weaponData)
        {
            return GetWeapon(weaponData.GetItemID()) != null;
        }
        else if (itemData is TrinketData trinketData)
        {
            foreach (Trinket trinket in OwnedTrinkets)
            {
                if (trinket.GetItemID() == itemId)
                {
                    return true;
                }
            }
        }
        else  if (itemData is IdolItemData idolData)
        {
            foreach (IdolItem idolItem in OwnedIdols)
            {
                if (idolItem.GetItemID() == itemId)
                {
                    return true;
                }
            }    
        }
        else if (itemData.IsQuestItem())
        {
            foreach (Item item in HeldQuestItems)
            {
                return item.GetItemID() == itemData.GetItemID();
            }
        }

        return false;
    }

    public List<Item> GetIdolsAsItems()
    {
        List<Item> ret = new();
        
        ret = OwnedIdols.Select(serialized => (Item) serialized).ToList();

        return ret;
    }

    public List<WeaponData> GetUnlockedWeaponData()
    {
        List<WeaponData> ret = new();

        ret = OwnedWeapons.Select(serialized => serialized).ToList();
        
        return ret;
    }

    public List<Item> GetTrinketsAsItems()
    {
        List<Item> ret = new();

        ret = OwnedTrinkets.Select(serialized => (Item) serialized).ToList();

        return ret;
    }
    
    public List<Trinket> GetTrinkets()
    {
        List<Trinket> ret = new();

        ret = OwnedTrinkets.Select(serialized => (Trinket)serialized).ToList();

        return ret;
    }
    #endregion


    #region IDOLS
    public void SetEquippedIdol(IdolItem equipped)
    {
        EquippedIdol = equipped;
    }
    #endregion


    #region WEAPONS
    
    public void SetEquippedWeapon(WeaponData equipped)
    {
        Debug.Log("SetEquippedWeapon " + equipped);
        EquippedWeapon = equipped;
    }

    public WeaponData GetWeapon(string weaponId)
    {
        foreach (WeaponData weapon in OwnedWeapons)
        {
            if (weapon.GetItemID() == weaponId)
            {
                return weapon;
            }
        }

        return null;
    }

    #endregion


    #region TRINKET
    public void SetEquippedTrinket(Trinket equipped)
    {
        EquippedTrinket = equipped;
    }

    #endregion


    #region QUESTS
    
    public void AddQuest(Quest quest, bool allowImmediateTrack = true)
    {
        RunningQuests.Add(quest);

        // Make this the tracked quest if none else exist
        if (RunningQuests.Count() == 1)
        {
            SetTrackedQuest(quest);
        }
            
        quest.OnQuestCompleted += CompleteQuest;

        quest.Start();
    }

    public void SetTrackedQuest(Quest quest)
    {
        TrackedQuest = quest;

        OnTrackedQuestChanged?.Invoke(TrackedQuest);
    }

    public bool HasQuest(Quest quest)
    {
        return HasQuest(quest.GetID());
    }

    public bool HasQuest(QuestData questData)
    {
        return HasQuest(questData.GetID());
    }

    public bool HasQuest(string questID)
    {
        if (CompletedQuest(questID))
        {
            return true;
        }

        foreach (Quest runningQuest in RunningQuests)
        {
            if (runningQuest.GetID() == questID)
            {
                return true;
            }
        }

        return false;
    }


    void CompleteQuest(Quest completedQuest)
    {
        for (int i = RunningQuests.Count - 1; i >= 0; i--)
        {
            Quest runningQuest = RunningQuests[i];
            if(runningQuest.GetID() == completedQuest.GetID())
            {
                RunningQuests.Remove(runningQuest);
                CompletedQuests.Add(runningQuest);

                OnQuestCompleted?.Invoke(completedQuest);

                // TODO only clear if no other quests remain
                OnTrackedQuestCleared?.Invoke(completedQuest);
                break;
            }
        }

    }

    public bool CompletedQuest(Quest quest)
    {
        return CompletedQuest(quest.GetID());
    }

    public bool CompletedQuest(string questID)
    {
        foreach (Quest completedQuest in CompletedQuests)
        {
            if (completedQuest.GetID() == questID)
            {
                return true;
            }
        }

        return false;
    }

    public bool HasObjective(ObjectiveData objectiveData)
    {
        foreach (Quest quest in RunningQuests)
        {
            if (quest.GetCurrentObjective().objectiveDataName == objectiveData.name)
            {
                return true;
            }
        }

        return false;
    }
    #endregion

    public void CompletedTutorial(string name)
    {
        CompletedTutorialNames.Add(name);
    }
}