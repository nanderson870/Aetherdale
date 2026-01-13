using System.Collections.Generic;
using Aetherdale;
using FMODUnity;
using Mirror;
using UnityEngine;

/// <summary>
/// Spawns elites and rewards a trait on all of their deaths
/// </summary>
public class ChallengeTablet : NetworkBehaviour, IInteractable
{
    public Element element = Element.Physical;

    [SerializeField] EventReference startStinger;

    public float enemySelectionLevelMult = 1.5F; // multiplies current enemy level when selecting from regional spawnlist

    const int NUMBER_ENEMIES = 4;
    const float SPAWN_RADIUS = 15.0F;

    [SyncVar] bool activated = false;
    [SyncVar] bool completed = false;
    
    List<Entity> entities = new();

    public static string GetCompletionMessage(Element element)
    {
        return element switch
        {
            Element.Fire => "You are emboldened by the inferno.",
            Element.Water => "You are brought into balance by the seas.",
            Element.Nature => "You are made sturdier by the earth",
            Element.Storm => "You are invigorated by the maelstrom.",
            Element.Dark => "You descend deeper into shadow.",
            Element.Light => "You are made radiant by the stars.",
            _ => "You have prevailed against the elements.",
        };
    }

    public static Trait GetRewardTrait(Element element)
    {
        return element switch
        {
            Element.Fire => new Catalyze(),
            Element.Water => new Absorption(),
            Element.Nature => new Unyielding(),
            Element.Storm => new Voltage(),
            Element.Dark => new Shadowstep(),
            Element.Light => new SeekingSpirits(),
            _ => throw new System.NotImplementedException($"Invalid element given to GetRewardTrait - {element}")
        };
    }


    public void Start()
    {
        if (element == Element.Physical)
        {
            element = (Element) Random.Range((int) Element.Fire, (int) Element.Dark + 1);
        }

        foreach (AlphabetSymbol symbol in GetComponentsInChildren<AlphabetSymbol>())
        {
            symbol.SetColor(ColorPalette.GetPrimaryColorForElement(element));

            symbol.SetIndex(Random.Range(0, 25));
        }

        foreach (Light light in GetComponentsInChildren<Light>())
        {
            light.color = ColorPalette.GetPrimaryColorForElement(element);
        }
    }

    public void Update()
    {
        if (activated && !completed)
        {
            foreach (Entity entity in entities)
            {
                if (entity != null || !entity.IsDead())
                {
                    return;
                }
            }

            // If we get here, all entities are dead
            CompleteChallenge();
        }
    }

    public void BeginChallenge(Player initiator)
    {
        if (activated)
        {
            return;
        }

        activated = true;

        Player.SendEnvironmentChatMessage($"{initiator.GetDisplayName()} has initiated an elemental challenge!");

        SpawnList spawnList = AreaManager.CurrentAreaManager.GetArea().region.spawnList;

        int enemyLevel = AreaManager.CurrentAreaManager.GetEnemyLevel();

        int challengeLevel = (int)(enemyLevel * enemySelectionLevelMult);

        for (int i = 0; i < NUMBER_ENEMIES; i++)
        {
            Vector2 spawnOffset = Random.insideUnitCircle * SPAWN_RADIUS;
            Vector3 spawnPos = transform.position + new Vector3(spawnOffset.x, 1.0F, spawnOffset.y);

            Entity entity = Instantiate(spawnList.GetEntity(challengeLevel), spawnPos, Quaternion.Euler(0, Random.Range(0, 361), 0));
            entity.SetLevel(challengeLevel);
            entity.SetFaction(AreaManager.CurrentAreaManager.GetArea().region.defaultFaction);

            entities.Add(entity);

            NetworkServer.Spawn(entity.gameObject);

            Elite.CreateElite(entity, element);

        }

        RpcBeginChallenge();
        
    }


    [ClientRpc]
    void RpcBeginChallenge()
    {
        AudioManager.Singleton.PlayOneShot(startStinger);
    }

    void CompleteChallenge()
    {
        completed = true;
        Player.SendEnvironmentChatMessage(GetCompletionMessage(element));

        foreach (Player player in Player.GetPlayers())
        {
            // TODO trait selection when we have more we can use
            player.AddTrait(GetRewardTrait(element));
        }
    }


    #region IInteractable;
    public void Interact(ControlledEntity interactingEntity)
    {
        BeginChallenge(interactingEntity.GetOwningPlayer());
    }

    public string GetInteractionPromptText(ControlledEntity interactingEntity)
    {
        return "Activate Challenge Tablet";
    }

    public string GetTooltipText(ControlledEntity interactingEntity)
    {
        return "Summon elite enemies to fight. Receive a rare trait upon defeating them.";
    }

    public string GetTooltipTitle(ControlledEntity interactingEntity)
    {
        return "Begin Challenge";
    }

    public bool IsInteractable(ControlledEntity interactingEntity)
    {
        return !activated;
    }

    public bool IsSelectable()
    {
        return !activated;
    }

    #endregion

}
