using System;
using System.Collections;
using System.Collections.Generic;
using Aetherdale;
using FMOD.Studio;
using FMODUnity;
using Mirror;
using UnityEngine;
using UnityEngine.VFX;

public class AreaPortal : NetworkBehaviour, IInteractable
{
    public static Action OnAreaPortalDiscovered;
    public static Action<AreaPortal> OnPortalRebuildStart;

    public const float UPDATE_PERIODICS_INTERVAL = 0.25F;

    public const float PORTAL_REBUILD_PER_KILL = 0.05F;
    public const float PORTAL_REBUILD_PER_SECOND = 0.005F;

    public const float PORTAL_DISCOVERY_RANGE = 30.0F;

    [SerializeField] Renderer portalPlaneRenderer;
    [SerializeField] Light areaPointLight;

    [SerializeField] EventReference chargingLoop;
    [SerializeField] EventReference chargeFinishSound;
    [SerializeField] EventReference enemyDeathChargeSound;
    [SerializeField] EventReference chargedLoop;

    [SerializeField] VisualEffect portalChargeVFX;
    [SerializeField] PathFollowingParticleVFX entityDeathParticlePrefab;

    [SerializeField] EntityTrackingZone portalBuildZone;

    public Area area;

    [field: SerializeField] public bool AreaChosenByAreaManager { get; private set; }

    [SyncVar(hook = nameof(AreaIDChanged))] public string areaID = "";

    [SyncVar(hook = nameof(PortalActiveChanged))] public bool portalActive = false;

    [SyncVar(hook = nameof(PortalRebuildingChanged))] public bool rebuilding = false;
    [SyncVar(hook = nameof(CurrentRebuildChanged))] public float currentRebuild = 0;

    public Action<AreaPortal> OnFinishedRebuilding;
    public Action<AreaPortal, float> OnRebuildValueChanged;

    List<Objective> additionalObjectivesRequired = new();

    EventInstance rebuildLoopInstance;
    EventInstance rebuiltLoopInstance;

    [SyncVar(hook = nameof(OnDiscoveredChanged))] bool discovered = false;


    void Start()
    {

        portalBuildZone.gameObject.SetActive(false);
        portalPlaneRenderer.gameObject.SetActive(portalActive);

        portalChargeVFX.enabled = false;
        portalChargeVFX.enabled = true;
        portalChargeVFX.Stop();
        portalChargeVFX.Reinit();

        portalBuildZone.OnEntityDiedInZone += EntityDied;

        rebuildLoopInstance = RuntimeManager.CreateInstance(chargingLoop);
        RuntimeManager.AttachInstanceToGameObject(rebuildLoopInstance, transform);

        rebuiltLoopInstance = RuntimeManager.CreateInstance(chargedLoop);
        RuntimeManager.AttachInstanceToGameObject(rebuiltLoopInstance, transform);

        InvokeRepeating(nameof(UpdatePeriodics), 0, UPDATE_PERIODICS_INTERVAL);

    }

    void OnDestroy()
    {
        rebuildLoopInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        rebuiltLoopInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
    }

    void UpdatePeriodics()
    {
        if (isServer)
        {
            if (!discovered)
            {
                foreach (Entity entity in Player.GetPlayerEntities())
                {
                    float distanceToPortal = Vector3.Distance(entity.transform.position, transform.position);
                    if (distanceToPortal < PORTAL_DISCOVERY_RANGE)
                    {
                        Vector3 targetPos = transform.position + new Vector3(0, 8.0F, 0); // Target a bit in the air so ground doesnt interrupt portal LOS
                        if (Physics.Raycast(entity.transform.position, (targetPos - entity.transform.position), out RaycastHit hitInfo, distanceToPortal, LayerMask.GetMask("Default")))
                        {
                            if (Mathf.Abs(hitInfo.distance - distanceToPortal) < 0.5F)
                            {
                                // Most likely the portal that our raycast pinged
                                discovered = true;
                            }
                        }
                    }
                }  
            }

            if (rebuilding)
            {
                List<ControlledEntity> buildingEntities = portalBuildZone.GetEntitiesInZone<ControlledEntity>();

                if (buildingEntities.Count > 0)
                {
                    currentRebuild = currentRebuild + PORTAL_REBUILD_PER_SECOND * UPDATE_PERIODICS_INTERVAL;
                }

                if (currentRebuild >= 1)
                {
                    // Limit to 100% in case there are still requirements
                    currentRebuild = 1;

                    if (AllAdditionalObjectivesCompleted())
                    {
                        FinishRebuilding();
                    }
                }
            }
        }
    }

    public void SetArea(Area area)
    {
        if (!AreaChosenByAreaManager)
        {
            return;
        }

        this.area = area;
        this.areaID = area.GetAreaID();

        portalPlaneRenderer.material = this.area.GetPortalPlaneMaterial();
        areaPointLight.color = portalPlaneRenderer.material.GetColor("_Color_1");
    }


    void AreaIDChanged(string oldID, string newID)
    {
        area = Area.GetArea(areaID);

        portalPlaneRenderer.material = this.area.GetPortalPlaneMaterial();
    }


    [Server]
    public void Interact(ControlledEntity interactingEntity)
    {
        if (area.GetAreaName() == "Secluded Grove")
        {
            AreaSequencer.GetAreaSequencer().StopAreaSequence();
        }
        else
        {
            if (!portalActive && !rebuilding)
            {
                StartRebuilding();
            }
            else if (portalActive)
            {
                AreaSequencer.GetAreaSequencer().LoadArea(area);
            }
        }
    }

    [Server]
    void StartRebuilding()
    {
        rebuilding = true;

        OnPortalRebuildStart?.Invoke(this);

        Player.SendEnvironmentChatMessage("Rebuilding the portal draws the attention of nearby enemies!");
    }

    IEnumerator ScaleUpPortalZone()
    {
        Vector3 portalZoneScale = portalBuildZone.transform.localScale;

        float timeRemaining = 1.0F;
        Vector3 scalePerSecond = portalZoneScale / timeRemaining;

        portalBuildZone.transform.localScale = new();

        while (timeRemaining > 0)
        {
            portalBuildZone.transform.localScale += scalePerSecond * Time.deltaTime;
            timeRemaining -= Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }

        // For good measure, set back to default
        portalBuildZone.transform.localScale = portalZoneScale;
    }

    void FinishRebuilding()
    {
        rebuilding = false;
        currentRebuild = 1.0F;

        SetPortalActive(true);

        OnFinishedRebuilding?.Invoke(this);
    }


    
    private void EntityDied(Entity entity, Entity killer)
    {
        if (!rebuilding)
        {
            return;
        }

        if (isServer)
        {
            currentRebuild = currentRebuild + PORTAL_REBUILD_PER_KILL;
            RpcOnEntityDied(entity.GetWorldPosCenter());
        }
    }

    [ClientRpc]
    void RpcOnEntityDied(Vector3 deathPos)
    {
        Debug.Log("DIED EeVENT!!");
        PathFollowingParticleVFX vfx = Instantiate(entityDeathParticlePrefab, deathPos, Quaternion.identity);
        vfx.SetPositions(deathPos, transform.position + Vector3.up * 6.0F);
        vfx.Play();

        EventInstance instance = RuntimeManager.CreateInstance(enemyDeathChargeSound);
        RuntimeManager.AttachInstanceToGameObject(instance, gameObject);
        instance.setParameterByName("Portal Charge State", Mathf.Clamp01(currentRebuild));
        instance.start();
        instance.release();
    }

    public void SetPortalActive(bool active)
    {
        portalActive = active;

        if (active)
        {
            rebuilding = false;
            currentRebuild = 1.0F;
        }
    }

    void PortalActiveChanged(bool previousState, bool newState)
    {
        portalPlaneRenderer.gameObject.SetActive(newState);

        if (newState)
        {
            rebuiltLoopInstance.start();
        }
    }

    void PortalRebuildingChanged(bool previousState, bool newState)
    {
        portalBuildZone.gameObject.SetActive(newState);

        if (newState)
        {
            portalBuildZone.gameObject.SetActive(true);
            StartCoroutine(ScaleUpPortalZone());

            portalChargeVFX.SendEvent("StartRebuild");

            rebuildLoopInstance.start();
        }
        else if (previousState && !newState)
        {
            rebuildLoopInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);

            AudioManager.Singleton.PlayOneShot(chargeFinishSound, transform.position);

            portalChargeVFX.SendEvent("FinishRebuild");
        }
    }

    /// <summary>
    /// Adds an additional objective required to finish rebuilding this portal
    /// </summary>
    /// <param name="objective"></param>
    public void AddAdditionalObjective(Objective objective)
    {
        additionalObjectivesRequired.Add(objective);
    }

    public bool AllAdditionalObjectivesCompleted()
    {
        foreach (Objective objective in additionalObjectivesRequired)
        {
            if (!objective.IsObjectiveComplete())
            {
                return false;
            }
        }

        return true;
    }

    void CurrentRebuildChanged(float previousRebuild, float newRebuild)
    {
        OnRebuildValueChanged?.Invoke(this, newRebuild);
    }


    public bool IsInteractable(ControlledEntity interactingEntity)
    {
        return area != null
            && (portalActive || !rebuilding);
    }

    public string GetInteractionPromptText(ControlledEntity interactingEntity)
    {
        if (!portalActive && !rebuilding)
        {
            return "Rebuild Portal";
        }

        if (area == null)
        {
            return "Error";
        }

        if (area.GetAreaName() == "Secluded Grove")
        {
            return "Return Home";
        }

        if (area.IsBossArea())
        {
            return "Challenge Boss";
        }

        return $"{area.GetAreaName()}";
    }

    public string GetTooltipText(ControlledEntity interactingEntity)
    {
        return "";
    }

    public string GetTooltipTitle(ControlledEntity interactingEntity)
    {
        return "";
    }

    public bool IsSelectable()
    {
        return !rebuilding;
    }
    
    
    void OnDiscoveredChanged(bool prevValue, bool newValue)
    {
        if (!prevValue && newValue)
        {
            OnAreaPortalDiscovered?.Invoke();
        }
    }
}
