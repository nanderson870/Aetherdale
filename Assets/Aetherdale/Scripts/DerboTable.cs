using FMODUnity;
using Mirror;
using TMPro;
using UnityEngine;

namespace Aetherdale
{
    public class DerboTable : NetworkBehaviour, IInteractable
    {
        [SerializeField] ShopOfferingType offeringType = ShopOfferingType.None;
        [SerializeField] TextMeshPro priceTMP;

        [SerializeField] Transform offeringTransform;

        [SerializeField] ShopOfferingEffects effects;

        [SyncVar(hook = nameof(ShopOfferingChanged))] ShopOfferingInfo offeringInfo;
        [SerializeField] EventReference buySound;


        float rotationSpeed = 18.0F;
        float rotation = 0;
        const float rotationExtents = 40;
        bool positiveRotation = true;

        float height = 0;
        const float maxHeight = 0.3F;
        float heightPingPongInterval = 6.0F;
        float heightSeed = 0;

        Vector3 initialOfferingPosition;
        

        IShopOffering offering;


        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            if (NetworkServer.active && offeringInfo == null)
            {
                // Let the table group assign our item if it exists
                if (GetComponentInParent<DerboTableGroup>() == null)
                {
                    SetOffering(ShopOffering.GetRandomLevelledShopOffering(AreaSequencer.GetAreaSequencer().GetAreaLevel()));
                }
            }

            rotation = Random.Range(-rotationExtents, rotationExtents);

            offeringTransform.Rotate(Vector3.up, rotation);

            heightSeed = Random.Range(0F, 999F);

            initialOfferingPosition = offeringTransform.localPosition;
        }

        void Update()
        {
            float frameRotation = rotationSpeed * Time.deltaTime;
            if (!positiveRotation)
            {
                frameRotation *= -1;
            }

            rotation += frameRotation;

            offeringTransform.Rotate(Vector3.up, frameRotation);

            if (Mathf.Abs(rotation) >= rotationExtents)
            {
                positiveRotation = !positiveRotation;
            }


            // ---- Height ------
            height = Mathf.PingPong((Time.time + heightSeed) / heightPingPongInterval, maxHeight);
            offeringTransform.localPosition = initialOfferingPosition + new Vector3(0, height, 0);
        }

        [Server]
        public void SetOffering(IShopOffering shopOffering)
        {
            offeringInfo = shopOffering.GetInfo();
        }

        void ShopOfferingChanged(ShopOfferingInfo previous, ShopOfferingInfo current)
        {
            foreach (Transform child in offeringTransform)
            {
                Destroy(child.gameObject);
            }

            if (current == null)
            {
                offering = null;
                priceTMP.text = "Sold!";
                BroadcastMessage("MeshesChanged", SendMessageOptions.DontRequireReceiver);
                return;
            }

            offering = ShopOffering.ShopOfferingFromInfo(current);
            priceTMP.text = offeringInfo.goldCost.ToString();

            GameObject inst = Instantiate(offering.GetPreviewPrefab(), offeringTransform);
            inst.transform.localPosition = Vector3.zero;

            effects.SetRarity(offering.GetRarity());
            BroadcastMessage("MeshesChanged", SendMessageOptions.DontRequireReceiver);
        }

        [Server]
        public void Interact(ControlledEntity interactingEntity)
        {
            Player player = interactingEntity.GetOwningPlayer();

            if (player.GetInventory().GetGold() < offeringInfo.goldCost)
            {
                Debug.Log($"NOT ENOUGH GOLD - {player.GetInventory().GetGold()}/{offeringInfo.goldCost}");
                return;
            }

            if (!offering.PlayerMeetsRequirements(player))
            {
                return;
            }

            player.GetInventory().RemoveGold(offeringInfo.goldCost);
            offering.GiveToPlayer(player);

            offeringInfo = null;

            RpcBoughtItem();
        }

        [ClientRpc]
        void RpcBoughtItem()
        {
            AudioManager.Singleton.PlayOneShot(buySound);
        }

        public bool IsInteractable(ControlledEntity interactingEntity)
        {
            return offeringInfo != null && offering.PlayerMeetsRequirements(interactingEntity.GetOwningPlayer()) && interactingEntity.GetOwningPlayer().GetInventory().GetGold() >= offeringInfo.goldCost;
        }

        public string GetInteractionPromptText(ControlledEntity interactingEntity)
        {
            if (offeringInfo != null)
            {
                return $"Buy {offering.GetName()} ({offeringInfo.goldCost} gold)";
            }
            else
            {
                return "";
            }
        }

        public string GetTooltipTitle(ControlledEntity interactingEntity)
        {
            if (offeringInfo != null)
            {
                return offering.GetName();
            }
            else
            {
                return "";
            }
        }

        public string GetTooltipText(ControlledEntity interactingEntity)
        {
            return offering.GetStatsDescription();
        }

        public bool IsSelectable()
        {
            return offering != null;
        }
    }
}
