using UnityEngine;

namespace Aetherdale
{
    public class BubbleShieldObject : MonoBehaviour
    {
        [SerializeField] float scalePerRadius = 0.67F;


        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Entity entity = GetComponentInParent<Entity>();
            if (entity == null)
            {
                return;
            }

            if (entity.ephemeraParentTransform != null)
            {
                transform.SetParent(entity.ephemeraParentTransform);
            }

            float colliderRadius = entity.GetSize();
            float scale = colliderRadius * scalePerRadius;

            transform.localScale = Vector3.one * scalePerRadius;
            transform.position = entity.GetWorldPosCenter();
        }
    }
}
