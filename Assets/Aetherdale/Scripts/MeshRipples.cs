using UnityEngine;


/// <summary>
/// Causes a ripply wavelike pattern to cascade over the attached mesh
/// 
/// Ideal meshes contain many vertices
/// </summary>

public class MeshRipples : MonoBehaviour
{
    [SerializeField] float speed = 0.5F;
    [SerializeField] float rippleAmplitude = 1.0F;
    [SerializeField] float minWidth = 1.0F;
    [SerializeField] float maxWidth = 1.5F;

    [SerializeField] bool useLocalPosition = true;
    public enum MeshRippleDirection
    {
        X=0,
        Y=1,
        Z=2
    }
    [SerializeField] MeshRippleDirection direction;

    Mesh mesh;
    Vector3[] originalVertices;
    Vector3[] currentVertices;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MeshFilter filter = GetComponent<MeshFilter>();

        mesh = filter.mesh;

        originalVertices = mesh.vertices;
        currentVertices = mesh.vertices;
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < currentVertices.Length; i++)
        {
            float sinMult;
            switch (direction)
            {
                case MeshRippleDirection.X:
                    sinMult = (Mathf.Sin((Time.time * speed + currentVertices[i].x) * rippleAmplitude) + 2).Remap(0, 2, minWidth, maxWidth);
                    currentVertices[i].y = originalVertices[i].y * sinMult;
                    currentVertices[i].z = originalVertices[i].z * sinMult;
                    break;

                case MeshRippleDirection.Y:
                    sinMult = (Mathf.Sin((Time.time * speed + currentVertices[i].y) * rippleAmplitude) + 2).Remap(0, 2, minWidth, maxWidth);
                    currentVertices[i].x = originalVertices[i].x * sinMult;
                    currentVertices[i].z = originalVertices[i].z * sinMult;
                    break;

                case MeshRippleDirection.Z:
                    sinMult = (Mathf.Sin((Time.time * speed + currentVertices[i].z) * rippleAmplitude) + 2).Remap(0, 2, minWidth, maxWidth);
                    currentVertices[i].x = originalVertices[i].x * sinMult;
                    currentVertices[i].y = originalVertices[i].y * sinMult;
                    break;
            }
        }

        mesh.vertices = currentVertices;

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }
}