using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using System.IO;
using Mirror;
using System;

/// <summary>
/// Run a DebugCommand by invoking RunCommand, or by entering
/// a command preceded by "/" ingame
/// </summary>
public class DebugCommands
{
    public static void RunCommand(string command)
    {
        string[] components = command.Split(" ");

        if (components.Length == 0)
        {
            return;
        }

        if (components[0] == "load" && components.Length > 1)
        {
            LoadCommand(components[1]);
        }

        if (components[0] == "startnormal")
        {
            AreaSequencer.GetAreaSequencer().StartAreaSequence(AreaSequencer.SequenceMode.Normal);
        }

        if (components[0] == "startguantlet")
        {
            AreaSequencer.GetAreaSequencer().StartAreaSequence(AreaSequencer.SequenceMode.Gauntlet);
        }

        if (components[0] == "setusername" && components.Length > 1)
        {
            NetworkClient.localPlayer.GetComponent<Player>().SetUsername(components[1]);
        }

        if (components[0] == "giveall" && components.Length > 2)
        {
            ItemData itemData = ItemManager.LookupItemDataByName(components[1]);

            if (components[1].ToLower() == "gold")
            {
                foreach (Player player in Player.GetPlayers())
                {
                    player.GetInventory().AddGold(int.Parse(components[2]));
                }
            }
            else if (itemData != null)
            {
                foreach (Player player in Player.GetPlayers())
                {
                    player.GetPlayerData().AddAccountItem(itemData.GetItemID(), int.Parse(components[2]));
                }
            }
            Debug.Log("Gave player " + int.Parse(components[2]) + " of " + itemData);
        }

        if (components[0] == "giveme" && components.Length > 2)
        {
            ItemData itemData = ItemManager.LookupItemDataByName(components[1]);

            if (components[1].ToLower() == "gold")
            {
                Player.GetLocalPlayer().GetInventory().AddGold(int.Parse(components[2]));
            }
            else if (itemData != null)
            {
                Player.GetLocalPlayer().GetPlayerData().AddAccountItem(itemData.GetItemID(), int.Parse(components[2]));
            }
            Debug.Log("Gave player " + int.Parse(components[2]) + " of " + itemData);
        }

        if (components[0] == "settimescale" && components.Length > 1)
        {
            Time.timeScale = float.Parse(components[1]);
        }

        if (components[0] == "spawnobjects")
        {
            NetworkServer.SpawnObjects();
        }

        if (components[0] == "killme")
        {
            ControlledEntity playerEntity = Player.GetLocalPlayer().GetControlledEntity();
            playerEntity.Damage(playerEntity.GetMaxHealth(), Element.TrueDamage, damageDealer:playerEntity, hitType: HitType.Attack);
        }

        if (components[0] == "givexp" && components.Length > 1)
        {
            int exp = int.Parse(components[1]);
            Player.GetLocalPlayer().AddExperience(exp);
        }

        if (components[0] == "spawnatme" && components.Length > 1)
        {
            Entity spawnedEntityPrefab = EntityLookup.GetEntityByName(components[1]);
            if (spawnedEntityPrefab != null)
            {
                int spawns = 1;
                if (components.Length > 2) spawns = int.Parse(components[2]);

                for (int i = 0; i < spawns; i++)
                {
                    Entity spawnedEntity = GameObject.Instantiate(spawnedEntityPrefab, Player.GetLocalPlayer().GetControlledEntity().transform.position, Quaternion.identity);
                    NetworkServer.Spawn(spawnedEntity.gameObject);
                }
            }
        }

        if (components[0] == "godmode")
        {
            Player.GetLocalPlayer().GetControlledEntity().SetGodMode(!Player.GetLocalPlayer().GetControlledEntity().godmode);
        }

        if (components[0] == "setstat" && components.Length > 2)
        {
            string stat = components[1];
            int value = int.Parse(components[2]);

            Player.GetLocalPlayer().GetControlledEntity().SetStat(stat, value);
        }

        if (components[0] == "targetframerate" && components.Length > 1)
        {
            Application.targetFrameRate = int.Parse(components[1]);
        }

        if (components[0] == "givetrait" && components.Length > 1)
        {
            Trait trait = (Trait)Activator.CreateInstance(Type.GetType(components[1]));

            int quantity = 1;
            if (components.Length > 2)
            {
                quantity = int.Parse(components[2]);
            }

            for (int i = 0; i < quantity; i++)
            {
                Player.GetLocalPlayer().AddTrait(trait);
            }
        }

        if (components[0] == "zawarudo")
        {
            StatefulCombatEntity.SetStatefulCombatEntityGlobalAI(!StatefulCombatEntity.GetStatefulCombatEntityGlobalAIEnabled());
        }

        if (components[0] == "wraithvisionon")
        {
            AetherdalePostProcessing.TurnOnWraithVision();
        }
        if (components[0] == "wraithvisionoff")
        {
            AetherdalePostProcessing.TurnOffWraithVision();
        }

        if (components[0] == "stalactites")
        {
            int quantity = 0;
            if (components.Length > 1)
            {
                quantity = int.Parse(components[1]);
            }

            Stalactite.LooseStalactites(quantity);
        }

        if (components[0] == "setregionscompleted" && components.Length > 1)
        {
            int num = int.Parse(components[1]);
            AreaManager.regionsCompleted = num;
        }

        if (components[0] == "setmusicintensity" && components.Length > 1)
        {
            AudioManager.SetMusicIntensity(int.Parse(components[1]));
        }

        if (components[0] == "enablespawns")
        {
            SpawnZone.spawnsEnabled = true;
            Player.SendEnvironmentChatMessage("Spawns Enabled");
        }

        if (components[0] == "disablespawns")
        {
            SpawnZone.spawnsEnabled = false;
            Player.SendEnvironmentChatMessage("Spawns Disabled");
        }


    }


    static void LoadCommand(string sceneName)
    {
        int sceneCount = SceneManager.sceneCountInBuildSettings;

        string scrubbedSceneName = sceneName.Replace(" ", "").ToLower();
        for (int i = 0; i < sceneCount; i++)
        {
            string buildScenePath = SceneUtility.GetScenePathByBuildIndex(i);

            string scrubbedBuildSceneName = Path.GetFileNameWithoutExtension(buildScenePath).Replace(" ", "").ToLower();

            if (scrubbedSceneName == scrubbedBuildSceneName)
            {
                Debug.Log("commanded to load scene " + buildScenePath);
                AetherdaleNetworkManager.singleton.ServerChangeScene(buildScenePath);
            }
        }
    }

}