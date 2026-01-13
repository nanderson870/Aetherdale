using Mirror;
using UnityEngine;

namespace Aetherdale
{
    public class VoltarisBoulder : NetworkBehaviour
    {
        Voltaris voltaris;
        Vector3 originalHorizontalOffset;

        float maxVelocity = 4.0F;

        Rigidbody rb;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        // Update is called once per frame
        void Update()
        {
            if (voltaris != null)
            {
                Vector3 currentOffset = transform.position - voltaris.transform.position;
                currentOffset.y = 0;

                Vector3 desiredOffset = voltaris.transform.TransformVector(originalHorizontalOffset);

                Vector3 velocityToAdd = desiredOffset - currentOffset;
                if (velocityToAdd.magnitude > maxVelocity)
                {
                    velocityToAdd = velocityToAdd.normalized * maxVelocity;
                }

                rb.linearVelocity = new Vector3(velocityToAdd.x, rb.linearVelocity.y, velocityToAdd.z);
            }
        }

        public void SetVoltaris(Voltaris voltaris)
        {
            this.voltaris = voltaris;

            if (voltaris != null)
            {
                originalHorizontalOffset = voltaris.transform.InverseTransformVector(transform.position - voltaris.transform.position);
                originalHorizontalOffset.y = 0;
            }
        }
    }
}
