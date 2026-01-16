using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ThreatCompass : MonoBehaviour, IOnLocalPlayerReadyTarget
{
    public const float THREAT_COMPASS_PASSIVE_RADIUS = 30; // Enemies in this radius are picked up "passively", whether they intend to attack or not
    public const float THREAT_COMPASS_INTENT_RADIUS = 60; // Enemies in this radius are picked up only when they intend to attack us
    public const float THREAT_COMPASS_BEARING_LIMIT = 90;

    [SerializeField] Image threatIndicatorPrefab;

    PlayerUI parentUI;
    ControlledEntity owningEntity;
    Player owningPlayer;

    Dictionary<Entity, Image> currentThreats = new();

    public void OnLocalPlayerReady(Player player)
    {
        parentUI = GetComponentInParent<PlayerUI>();
        owningEntity = parentUI.GetOwningPlayer().GetControlledEntity();
        owningPlayer = parentUI.GetOwningPlayer();
        parentUI.GetOwningPlayer().OnEntityChangedOnClient += SetOwningEntity;
    }

    void FixedUpdate()
    {
        if (!Player.IsLocalPlayerReady)
        {
            return;
        }

        CheckForNewThreats();

        ProcessExistingThreats();
    }

    void CheckForNewThreats()
    {
        if (owningEntity == null)
        {
            return;
        }
        
        Collider[] colls = Physics.OverlapSphere(owningPlayer.GetCamera().gameObject.transform.position, THREAT_COMPASS_INTENT_RADIUS);
        foreach (Collider collider in colls)
        {
            Entity entity = collider.gameObject.GetComponentInParent<Entity>();
            if (entity != null)
            {
                if (entity.IsEnemy(owningEntity)
                    && (InRadius(entity) || entity.currentTarget == owningEntity)
                    && !currentThreats.ContainsKey(entity)
                    && (Mathf.Abs(owningEntity.GetCamera().gameObject.GetRelativeBearingAngle(entity.gameObject)) >   Camera.main.fieldOfView * 0.8F
                        || Mathf.Abs(owningEntity.GetCamera().gameObject.GetRelativePitchAngle(entity.gameObject)) > Camera.main.fieldOfView * 0.8F))
                {
                    AddThreat(entity);
                }
            }
        }
    }

    void ProcessExistingThreats()
    {
        List<Entity> keysForRemoval = new();
        foreach (var threat in currentThreats)
        {
            if (threat.Key == null || threat.Value == null
                || !InRadius(threat.Key)
                || (Mathf.Abs(owningEntity.GetCamera().gameObject.GetRelativeBearingAngle(threat.Key.gameObject)) <= Camera.main.fieldOfView * 0.8F
                    && Mathf.Abs(owningEntity.GetCamera().gameObject.GetRelativePitchAngle(threat.Key.gameObject)) <= Camera.main.fieldOfView * 0.8F))
            {
                keysForRemoval.Add(threat.Key);
                continue;
            }


            // Else still a valid threat, process position of threat indicator

            Vector3 direction = threat.Key.GetWorldPosCenter() - owningEntity.GetCamera().transform.position;
            Vector3 projected = Vector3.ProjectOnPlane(direction, owningEntity.GetCamera().transform.forward);

            Vector3 transformed = owningEntity.GetCamera().transform.InverseTransformDirection(projected).normalized;


            // x and y of transformed go from -1 to 1, need to map this onto viewport coords which is 0 to 1
            // transformed += Vector3.one * 0.5F;


            threat.Value.transform.localPosition = new(
                transformed.x * 0.45F * Screen.width,
                transformed.y * 0.45F * Screen.height
            );
        }


        foreach (Entity entity in keysForRemoval)
        {
            RemoveThreat(entity);
        }
    }

    bool InRadius(Entity potentialThreat)
    {
        if (owningEntity == null || potentialThreat == null)
        {
            return false;
        }
        
        float distance = Vector3.Distance(owningEntity.GetCamera().gameObject.transform.position, potentialThreat.transform.position);
        if (potentialThreat.currentTarget == owningEntity)
        {
            return distance < THREAT_COMPASS_INTENT_RADIUS;
        }
        else
        {
            return distance < THREAT_COMPASS_PASSIVE_RADIUS;
        }
    }

    void AddThreat(Entity entity)
    {
        currentThreats.Add(entity, Instantiate(threatIndicatorPrefab, transform));
    }

    void RemoveThreat(Entity entity)
    {
        Destroy(currentThreats[entity].gameObject);
        currentThreats.Remove(entity);
    }


    void SetOwningEntity(ControlledEntity newEntity)
    {
        owningEntity = newEntity;
    }

}
