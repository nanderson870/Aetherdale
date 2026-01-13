using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    public float lifespan = 1.0F;


    float startTime;
    void Start()
    {
        startTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if ((Time.time - startTime) > lifespan)
        {
            Destroy(gameObject);
        }
    }
}
