using System.Collections;
using FMODUnity;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerCamera : NetworkBehaviour
{
    const float CAMERA_LOOK_EVENT_DISTANCE = 50.0f;
    const float AIM_ASSIST_DEAD_ZONE_DEGREES = 5F;

    const float UNRECOIL_PER_SECOND=2.0F;

    public float aimAssistBearingRange = 10;
    public float aimAssistPitchRange = 10;
    public float aimAssistStrength = .5F;

    [SerializeField] float cameraSensitivity = 3.0f;
    [SerializeField] float positionalLerp = 0.125f;
    [SerializeField] float zoomLerp = 8.0F;
    [SerializeField] float maxDistance = 4.0f;

    [SerializeField] [FormerlySerializedAs("aimAssistFalloff")] AnimationCurve aimAssistAngleFalloff;
    [SerializeField] AnimationCurve aimAssistDistanceFalloff;
    


    Vector3 localOffset;
    LayerMask cameraCollideIgnore;

    CameraContext context;
    public bool offsetXFlipped = false;

    float currentDistance = 4.0F;
    float targetDistance = 4.0F;
    float preferredDistance = 4.0F;

    float recoilDistance = 0;



    float screenShakeAngleChangeInterval = 0.04F;

    readonly float maxYaw = 4F; // around Y
    readonly float maxPitch = 3F; // around X
    readonly float maxRoll = 2F; // around Z

    
    Vector3 screenShakeAngle = new();
    float lastScreenShakeAngleChange = 0;
    float screenShakeIntensity = 0.5F;
    float screenshakeDurationRemaining = 0.0F;

    Vector3 preShakeEulers = new();

    Vector3 aimedPosition; // Gets synced client->server

    Vector2 offset = Vector2.zero;

    bool aimIncludeEntities = false;


    public Entity aimAssistedEntity;


    public delegate void LookAtEntityAction(Entity entity);
    public event LookAtEntityAction OnLookAtEntity;


    public static PlayerCamera GetLocalPlayerCamera()
    {
        foreach (PlayerCamera playerCamera in FindObjectsByType<PlayerCamera>(FindObjectsSortMode.None))
        {
            if (playerCamera.isOwned)
            {
                return playerCamera;
            }
        }

        return null;
    }
    public void EnableCamera()
    {
        GetComponent<Camera>().enabled = true;
    }

    void Start()
    {
        cameraCollideIgnore = LayerMask.GetMask("Entities", "Hitboxes", "Hurtboxes", "Loot");

        if (!isOwned)
        {
            gameObject.SetActive(false);
            GetComponent<Camera>().enabled = false;
            GetComponent<StudioListener>().enabled = false;
        }
        else
        {
            EnableCamera();
        }

        currentDistance = maxDistance;
        preferredDistance = maxDistance;

        DontDestroyOnLoad(gameObject);
    }

    void LateUpdate()
    {
        if (!isOwned || context == null || Time.timeScale == 0)
        {
            return;
        }

        if (recoilDistance > 0)
        {
            recoilDistance -= UNRECOIL_PER_SECOND * Time.deltaTime;
        }

        //if (Physics.Raycast(transform.position, -transform.forward, out RaycastHit hitInfo, maxDistance, LayerMask.GetMask("Default")))
        if (Physics.Raycast(context.transform.position, -transform.forward, out RaycastHit hitInfo, maxDistance, LayerMask.GetMask("Default")))
        {
            targetDistance = hitInfo.distance - 0.5F;
        }
        else
        {
            targetDistance = preferredDistance;
        }

        targetDistance += recoilDistance;

        currentDistance = Mathf.Lerp(currentDistance, targetDistance, zoomLerp * Time.deltaTime);

        
        Vector3 currentOffset = offsetXFlipped ? new Vector3 (-localOffset.x, localOffset.y, localOffset.z) : localOffset;
        Vector3 desiredPosition = context.transform.position + context.transform.TransformVector(new Vector3(0, 0, -currentDistance));
        transform.position = desiredPosition;

        CalculateAimedPosition();

        transform.eulerAngles = new();
        transform.LookAt(context.transform);

        preShakeEulers = transform.eulerAngles;

        if (Settings.settings.controlsSettings.aimAssist)
        { 
            ApplyAimAssist();
        }

        // Transform and rotation are always "zeroed" as of now, so screenshake is applied
        ProcessScreenShake();

    }
    

    void ApplyAimAssist()
    {
        if (aimAssistedEntity != null)
        {
            float x = context.gameObject.GetRelativeBearingAngle(aimAssistedEntity.GetWorldPosCenter());
            float y = context.gameObject.GetRelativePitchAngle(aimAssistedEntity.GetWorldPosCenter());
            Vector2 rotation = -new Vector2(-x, y);
            if (rotation.magnitude < AIM_ASSIST_DEAD_ZONE_DEGREES)
            {
                return;
            }

            Vector2 maximums = new Vector2(aimAssistBearingRange, aimAssistPitchRange);

            float normalizedDistanceFromCenter = rotation.magnitude / maximums.magnitude;
            float strength = aimAssistAngleFalloff.Evaluate(normalizedDistanceFromCenter) * aimAssistStrength;

            float distance = Vector3.Distance(transform.position, aimAssistedEntity.transform.position);
            if (distance > ControlledEntity.RANGED_AIM_ASSIST_MAX_DEFAULT)
                distance = ControlledEntity.RANGED_AIM_ASSIST_MAX_DEFAULT;

            strength *= aimAssistDistanceFalloff.Evaluate(distance / ControlledEntity.RANGED_AIM_ASSIST_MAX_DEFAULT);

            context.AddRotation(strength * Time.deltaTime * rotation.normalized);
        }
    }


    /// <summary>
    /// Recoil camera back <strength> meters
    /// </summary>
    [ClientRpc]
    public void RpcApplyZoomRecoil(float strength)
    {
        recoilDistance += strength;
    }

    public void OverridePreferredDistance(float preferredDistance)
    {
        this.preferredDistance = preferredDistance;
    }

    public void ClearPreferredDistanceOverrides()
    {
        preferredDistance = maxDistance;
    }

    public void SetAimIncludeEntities(bool include)
    {
        aimIncludeEntities = include;
    }

    public void CheckLookingAtEntities()
    {
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, CAMERA_LOOK_EVENT_DISTANCE, LayerMask.GetMask("Entities")))
        {
            if (hit.transform.gameObject.TryGetComponent(out Entity entity))
            {
                OnLookAtEntity?.Invoke(entity);
            }
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="strength"></param>
    /// <param name="duration"></param>
    /// <param name="originPosition"></param>
    public static void ApplyScreenShake(float strength, float duration, Vector3 originPosition = new(), float frequency = 25F)
    {
        foreach (PlayerCamera playerCamera in FindObjectsByType<PlayerCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            float distance = Vector3.Distance(playerCamera.transform.position, originPosition);

            float distanceAdjustedStrength = strength / Mathf.Sqrt(distance);

            playerCamera.TargetApplyScreenShake(distanceAdjustedStrength, duration, frequency);
        }
    }

    public static void ApplyLocalScreenShake(float strength, float duration, Vector3 originPosition = new(), float frequency = 25F)
    {
        PlayerCamera playerCamera = Player.GetLocalPlayer().GetCamera();

        float distance = Vector3.Distance(playerCamera.transform.position, originPosition);

        float distanceAdjustedStrength = strength / Mathf.Sqrt(distance);

        playerCamera.ApplyScreenShake(distanceAdjustedStrength, duration, frequency);
    }

    [TargetRpc]
    public void TargetApplyScreenShake(float strength, float duration, float frequency)
    {
        ApplyScreenShake(strength, duration, frequency);
    }

    void ApplyScreenShake(float strength, float duration, float frequency)
    {
        screenshakeDurationRemaining = duration;
        screenShakeIntensity = strength;
        screenShakeAngleChangeInterval = 1.0F / frequency;
    }


    /// <summary>
    /// Process screen shake, meant to be performed on update.
    /// Camera transform must be locally defaulted before each call
    /// </summary>
    public void ProcessScreenShake()
    {
        if (screenshakeDurationRemaining <= 0)
        {
            return;
        }

        screenshakeDurationRemaining -= Time.deltaTime;

        ChangeScreenShakeAngle();

        transform.eulerAngles = transform.eulerAngles + screenShakeAngle;
    }

    void ChangeScreenShakeAngle()
    {
        screenShakeAngle = new()
        {
            x = maxPitch * screenShakeIntensity * (Mathf.PerlinNoise(123456, Time.time / screenShakeAngleChangeInterval) * 2 - 1),
            y = maxYaw   * screenShakeIntensity * (Mathf.PerlinNoise(123457, Time.time / screenShakeAngleChangeInterval) * 2 - 1),
            z = maxRoll  * screenShakeIntensity * (Mathf.PerlinNoise(123458, Time.time / screenShakeAngleChangeInterval) * 2 - 1)
        };

        lastScreenShakeAngleChange = Time.time;
    } 

    [ClientRpc]
    public void RpcSetContextFromEntity(Entity entity)
    {
        Debug.Log("Setting camera context to entity: " + entity);
        SetContext(entity.GetCameraContext());
    }

    public void SetContext(CameraContext context)
    {
        this.context = context;
    
        if (this.context != null)
        {
            transform.position = context.transform.position + new Vector3(0, 0, -currentDistance);
        }

        transform.LookAt(context.transform);
        GetComponent<StudioListener>().attenuationObject = context.gameObject;
    }

    public Vector3 GetPreShakeEulers()
    {
        return preShakeEulers;
    }

    void CalculateAimedPosition()
    {
        if (context == null)
        {
            aimedPosition = Vector3.zero;
            return;
        }
        
        int layerMask = LayerMask.GetMask("Default");
        Vector3 startPos = transform.position;
        if (aimIncludeEntities)
        {
            layerMask = LayerMask.GetMask("Default", "Entities");
        }


        if (Physics.Raycast(startPos, transform.forward, out RaycastHit hit, Mathf.Infinity, layerMask))
        {
            if (Vector3.Distance(transform.position, context.transform.position)
                < Vector3.Distance(transform.position, hit.point))
            {
                aimedPosition = hit.point;
            }
        }
        else
        {
            aimedPosition = transform.position + transform.forward * 1000.0F;
        }
        
        CmdUpdateAimedPosition(aimedPosition);
    }

    [Command]
    void CmdUpdateAimedPosition(Vector3 aimedPosition)
    {
        this.aimedPosition = aimedPosition;
    }

    public Vector3 GetAimedPosition()
    {
        return aimedPosition;
    }

    public void AddOffset(Vector3 offset)
    {
        this.offset += new Vector2(offset.x, offset.y);
    }

    public void RemoveOffset(Vector3 offset)
    {
        this.offset -= new Vector2(offset.x, offset.y);
    }

    public void ClearOffset()
    {
        this.offset = Vector3.zero;
    }
}
