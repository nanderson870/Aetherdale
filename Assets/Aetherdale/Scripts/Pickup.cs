
using UnityEngine;
using Mirror;
using System.Collections.Generic;
using FMODUnity;

[RequireComponent(typeof(Rigidbody))]
public abstract class Pickup : NetworkBehaviour
{
    const float PICKUP_RANGE = 1.0F;

    const float TIMEOUT_FLASH_FREQUENCY = 0.25F;

    [Header("Pickup Settings")]
    [SerializeField] protected float pickupTimeout = 0.0F;
    [SerializeField] protected bool magnetism = true;
    [SerializeField] protected float magnetismRange = 8.0F;
    [SerializeField] protected float magnetismBaseSpeed = 4.0F;
    [SerializeField] protected float magnetismAcceleration = 12.0F;
    [SerializeField] protected float lifespanBeforeMagnetism = 0.8F;
    [SerializeField] protected EventReference pickupSoundEvent;
    
    List<Player> playersThatCanReceive = null;
    List<Condition> visibilityConditions = new();

    public Rigidbody body;
    
    public float spawnTime;

    /// <summary>
    /// Whether one person picking this up picks it up for everyone
    /// </summary>
    bool shared = false;

    bool flashing = false;
    
    float currentAcceleratedVelocity = 0;


    public static T Create<T>(T pickupPrefab, Vector3 position, int quantity, bool shared=false, List<Player> playersThatCanReceive = null) where T : Pickup
    {
        T inst = Instantiate(pickupPrefab, position, Quaternion.identity);
        inst.playersThatCanReceive = playersThatCanReceive;
        inst.shared = shared;

        NetworkServer.Spawn(inst.gameObject);

        return inst;
    }

    public static Pickup Create(ItemData itemData, Vector3 position, int quantity, bool shared = false, List<Player> playersThatCanReceive = null)
    {
        Pickup pickup = itemData.CreatePickup(position, Quaternion.identity);
        pickup.playersThatCanReceive = playersThatCanReceive;
        pickup.shared = shared;

        return pickup;
    }

    public virtual void Start()
    {
        body = this.GetComponent<Rigidbody>();

        if (isServer)
        {
            foreach (Player player in Player.GetPlayers())
            {
                foreach (Condition condition in visibilityConditions)
                {
                    if (!condition.Check(player) && (playersThatCanReceive == null || playersThatCanReceive.Contains(player)))
                    {
                        NetworkIdentity interacterIdentity = player.GetComponent<NetworkIdentity>();
                        TargetSetInactive(interacterIdentity.connectionToClient);

                        break;
                    }
                }
            }

            RpcSetInitialVelocity(body.linearVelocity);

            if (TryGetComponent(out InteractionPrompt _))
            {
                // Almost never want magnetism
                magnetism = false;
            }
        }

        spawnTime = Time.time;
    }

    void RpcSetInitialVelocity(Vector3 initialVelocity)
    {
        body.linearVelocity = initialVelocity;
    }


    public virtual void Teardown()
    {
        NetworkServer.UnSpawn(gameObject);

        if (this is LootItem lootItem && lootItem.data == AetherdaleData.GetAetherdaleData().goldCoinsItem)
        {
            ItemSpawnPooler.singleton.Return(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void FixedUpdate()
    {
        if (isServer && pickupTimeout != 0)
        {
            if ((Time.time - spawnTime) >= pickupTimeout)
            {
                // timed out
                Teardown();
                return;
            }
        }

        if (!gameObject.activeSelf)
        {
            return;
        }

        if (pickupTimeout != 0)
        {
            if ((Time.time - spawnTime) >= pickupTimeout * 0.75F && !flashing)
            {
                flashing = true;        
                InvokeRepeating(nameof(TimeoutFlash), 0, TIMEOUT_FLASH_FREQUENCY);
            }
        }

        if (magnetism)
        {
            if ((Time.time - spawnTime) >= lifespanBeforeMagnetism)
            {
                // Check if local player is in range, and initiate process of pulling into player if so
                Player localPlayer = Player.GetLocalPlayer();
                if (localPlayer != null)
                {
                    ControlledEntity localPlayerEntity = localPlayer.GetControlledEntity();
                    if (localPlayerEntity != null && localPlayerEntity.IsEntityReady())
                    {
                        Vector3 playerEntityCenter = localPlayerEntity.GetWorldPosCenter();
                        if (Vector3.Distance(playerEntityCenter, transform.position) < magnetismRange)
                        {
                            GetComponent<Collider>().excludeLayers = ~0;
                            GetComponent<Rigidbody>().useGravity = false;

                            if (Vector3.Distance(playerEntityCenter, transform.position) < PICKUP_RANGE)
                            {
                                gameObject.SetActive(false); // set inactive now so that we don't update again and command a second pickup

                                CmdPickMeUp(localPlayerEntity);
                            }
                            else
                            {
                                currentAcceleratedVelocity += magnetismAcceleration * Time.deltaTime;
                                Vector3 velocity  = (magnetismBaseSpeed + currentAcceleratedVelocity) * (playerEntityCenter - transform.position).normalized;
                                body.linearVelocity = velocity;
                            }
                        }
                        else
                        {
                            GetComponent<Collider>().excludeLayers = 0;
                            GetComponent<Rigidbody>().useGravity = true;
                            currentAcceleratedVelocity = 0;
                        }
                    }
                }
            }
        }
    }

    void TimeoutFlash()
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = !renderer.enabled;
        }
    }

    public void SetShared(bool shared)
    {
        this.shared = shared;
    }
    

    [Server]
    public void SetVisibilityConditions(List<Condition> visibilityConditions)
    {
        this.visibilityConditions = visibilityConditions;
    }

    [TargetRpc]
    public void TargetSetInactive(NetworkConnectionToClient target)
    {
        gameObject.SetActive(false);
    }

    
    [Command(requiresAuthority = false)]
    public void CmdPickMeUp(ControlledEntity interactingEntity)
    {
        PickMeUp(interactingEntity);

    }

    [Server]
    void PickMeUp(ControlledEntity interactingEntity)
    {
        Player interactingPlayer = interactingEntity.GetOwningPlayer();

        NetworkIdentity interacterIdentity = interactingEntity.GetComponent<NetworkIdentity>();

        // Set object as inactive on clientside no matter what
        TargetSetInactive(interacterIdentity.connectionToClient);

        foreach (Condition condition in visibilityConditions)
        {
            if (!condition.Check(interactingPlayer))
            {
                // A condition is not met, do not allow this item to be picked up
                Debug.LogWarning(interactingEntity + " tried to pick up an item it did not meet the conditions for");
                return;
            }
        }

        if (!pickupSoundEvent.IsNull)
        {
            AudioManager.Singleton.PlayOneShot(pickupSoundEvent, transform.position);
        }

        if (shared)
        { 
            Teardown();
        }

        OnPickup(interactingEntity);
    }

    protected abstract void OnPickup(Entity entity);

    public void Interact(ControlledEntity interactingEntity)
    {
        PickMeUp(interactingEntity);
    }

    public bool IsSelectable()
    {
        return true;
    }


    [Server]
    public Pickup Drop(Vector3 position, Vector3 velocity = new(), List<Condition> requirements = null, bool shared = false)
    {
        Pickup droppedPickup;

        if (this == AetherdaleData.GetAetherdaleData().goldCoinsItem.GetLootItem())
        {
            droppedPickup = ItemSpawnPooler.singleton.GetGold(position, Quaternion.identity).GetComponent<Pickup>();
        }
        else
        {
            droppedPickup = Instantiate(this, position, this.transform.rotation);
        }
        
        NetworkServer.Spawn(droppedPickup.gameObject);

        droppedPickup.gameObject.GetComponent<Rigidbody>().linearVelocity = velocity;

        if (requirements != null)
        {
            droppedPickup.SetVisibilityConditions(requirements);
        }

        droppedPickup.SetShared(shared);

        return droppedPickup;
    }

    public float GetTimeout()
    {
        return pickupTimeout;
    }

    public float GetSpawnTime()
    {
        return spawnTime;
    }

}