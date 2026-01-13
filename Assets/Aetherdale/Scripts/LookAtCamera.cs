using UnityEngine;

namespace Aetherdale
{
    public class LookAtCamera : MonoBehaviour
    {
        public Vector3 up;

        // Update is called once per frame
        void LateUpdate()
        {
            if (Camera.main != null)
            {

                //float originalZ = transform.rotation.eulerAngles.z;

                Vector3 towardsCamera = Camera.main.transform.position - transform.position;
                
                Vector3 eulers = Quaternion.LookRotation(towardsCamera).eulerAngles + up;
                //eulers.z = originalZ;
                
                transform.rotation = Quaternion.Euler(eulers);
            }
        }
    }
}
