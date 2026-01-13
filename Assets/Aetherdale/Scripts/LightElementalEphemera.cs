using UnityEngine;

namespace Aetherdale
{
    public class LightElementalEphemera : MonoBehaviour
    {
        [SerializeField] GameObject topRing;
        [SerializeField] GameObject middleRing;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Entity entity = GetComponentInParent<Entity>();
            
            Collider collider = entity.gameObject.GetComponent<Collider>();

            Vector3 top = collider.bounds.center + new Vector3(0, collider.bounds.extents.y + 2.0F * entity.GetSize(), 0);
            Vector3 center = collider.bounds.center;

            topRing.transform.position = top;
            middleRing.transform.position = center;

            topRing.transform.localScale *= entity.GetSize();
            middleRing.transform.localScale *= entity.GetSize();
        }
    }
}
