using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Aetherdale
{
    public class LightController : MonoBehaviour
    {
        public float lifespan;
        public List<float> intensities = new();
        
        Light controlledLight;
        int currentIntensityIndex = 0;
        float durationBetweenIntensities = 0;
        float lifespanLeft;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            lifespanLeft = lifespan;
            controlledLight = GetComponent<Light>();
            controlledLight.intensity = intensities[0];

            if (intensities.Count > 1)
            {
                durationBetweenIntensities = lifespan / intensities.Count;

                StartCoroutine(StepIntensity(intensities[0], intensities[1], durationBetweenIntensities));
            }
        }

        IEnumerator StepIntensity(float startIntensity, float endIntensity, float duration)
        {
            float difference = endIntensity - startIntensity;
            float changePerSecond = difference / duration;

            float startTime = Time.time;

            while (Time.time - startTime < duration)
            {
                controlledLight.intensity += changePerSecond * Time.deltaTime;
                yield return null;
            }
            
            controlledLight.intensity = endIntensity;

            currentIntensityIndex++;
            if (currentIntensityIndex + 1 < intensities.Count)
            {
                StartCoroutine(StepIntensity(intensities[currentIntensityIndex], intensities[currentIntensityIndex + 1], duration));
            }

        }

        // Update is called once per frame
        void Update()
        {
            lifespanLeft -= Time.deltaTime;

            if (lifespanLeft <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
