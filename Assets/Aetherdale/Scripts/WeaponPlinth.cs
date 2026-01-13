using Mirror;
using UnityEngine;

public class WeaponPlinth : NetworkBehaviour, IInteractable
{
    [SerializeField] Transform weaponTransform;
    [SerializeField] int minLevel = 0;
    
    float rotationSpeed = 18.0F;
    float rotation = 0;
    const float rotationExtents = 40;
    bool positiveRotation = true;

    [SyncVar(hook = nameof(WeaponChanged))] ShopOfferingInfo weaponInfo;
    
    float height = 0;
    const float maxHeight = 0.3F;
    float heightPingPongInterval = 6.0F;
    float heightSeed = 0;

    
    Vector3 initialWeaponPosition;

    WeaponData weapon;

    void Start()
    {
        if (NetworkServer.active && weaponInfo == null)
        {
            // Let the table group assign our item if it exists
            if (GetComponentInParent<DerboTableGroup>() == null)
            {
                int level = AreaSequencer.GetAreaSequencer().GetAreaLevel();
                if (minLevel > level)
                {
                    level = minLevel;
                }

                SetWeapon(ShopOffering.CreateWeaponOffering(level));
            }
        }

        rotation = Random.Range(-rotationExtents, rotationExtents);

        weaponTransform.Rotate(Vector3.up, rotation);

        heightSeed = Random.Range(0F, 999F);

        initialWeaponPosition = weaponTransform.localPosition;
    }

    void Update()
    {
        float frameRotation = rotationSpeed * Time.deltaTime;
        if (!positiveRotation)
        {
            frameRotation *= -1;
        }

        rotation += frameRotation;


        weaponTransform.Rotate(Vector3.up, frameRotation);

        if (Mathf.Abs(rotation) >= rotationExtents)
        {
            positiveRotation = !positiveRotation;
        }


        // ---- Height ------
        height = Mathf.PingPong((Time.time + heightSeed) / heightPingPongInterval, maxHeight);
        weaponTransform.localPosition = initialWeaponPosition + new Vector3(0, height, 0);
    }
    

    void SetWeapon(WeaponData weaponData)
    {
        weaponInfo = weaponData.GetInfo();
    }
    
    void WeaponChanged(ShopOfferingInfo oldWeaponInfo, ShopOfferingInfo newWeaponInfo)
    {
        foreach (Transform child in weaponTransform)
        {
            Destroy(child.gameObject);
        }

        if (newWeaponInfo == null)
        {
            weapon = null;
            BroadcastMessage("MeshesChanged", SendMessageOptions.DontRequireReceiver);
            return;
        }

        weapon = (WeaponData) ShopOffering.ShopOfferingFromInfo(newWeaponInfo);

        GameObject inst = Instantiate(weapon.GetPreviewPrefab(), weaponTransform);
        inst.transform.localPosition = Vector3.zero;
        BroadcastMessage("MeshesChanged", SendMessageOptions.DontRequireReceiver);
    }

    [Server]
    public void Interact(ControlledEntity interactingEntity)
    {
        if (interactingEntity is IWeaponBehaviourWielder weaponBehaviourWielder)
        {
            WeaponData dispensedData = (WeaponData) ShopOffering.ShopOfferingFromInfo(weaponInfo);

            SetWeapon(weaponBehaviourWielder.GetEquippedWeaponData());

            weaponBehaviourWielder.gameObject.GetComponent<Entity>().GetOwningPlayer().SetSequenceWeapon(dispensedData);
        }
    }

    public bool IsInteractable(ControlledEntity interactingEntity)
    {
        return weaponInfo != null;
    }

    public bool IsSelectable()
    {
        return weaponInfo != null;
    }

    public string GetInteractionPromptText(ControlledEntity interactingEntity)
    {
        return $"Equip {weaponInfo.name}";
    }

    public string GetTooltipTitle(ControlledEntity interactingEntity)
    {
        return weaponInfo.name;
    }

    public string GetTooltipText(ControlledEntity interactingEntity)
    {
        return weaponInfo.statsDescription;
    }
}
