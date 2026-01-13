using UnityEngine;
using UnityEngine.VFX;

namespace Aetherdale
{
    public class VisualEffectTester : MonoBehaviour
    {
        VisualEffect effect;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            effect = GetComponent<VisualEffect>();
        }

        // Update is called once per frame
        void Update()
        {
            Debug.Log(effect.aliveParticleCount);    
            
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                effect.Play();
            }
            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                effect.Stop();
            }
        }
    }
}
