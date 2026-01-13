using UnityEngine;

[ExecuteInEditMode]
public class SpinningObject : MonoBehaviour
{
    public bool local = false;
    public Vector3 rotationAxis = Vector3.up;
    public float anglePerSecond = 90.0F;

    public bool paused = false;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!paused)
        {
            if (local)
            {
                transform.Rotate(rotationAxis, anglePerSecond * Time.deltaTime);
            }
            else
            {
                transform.Rotate(transform.InverseTransformVector(rotationAxis), anglePerSecond * Time.deltaTime);
            }
        }
    }
}
