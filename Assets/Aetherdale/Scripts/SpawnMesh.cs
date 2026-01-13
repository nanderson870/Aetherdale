using System.Collections.Generic;
using System.Linq;
using Geometry2D;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.AI;

[ExecuteInEditMode]
public class SpawnMesh : MonoBehaviour
{
    public List<Triangle3D> triangles = new();

    public static SpawnMesh Singleton;

    public bool showGizmos = false;

#region RUNTIME

    public void Start()
    {
        Singleton = this;
    }

    public static Vector3 GetSpawnPoint(Vector3 position = new(), float distance = 30.0F)
    {
        if (position != Vector3.zero)
        {
            Debug.Log("SPECIFIED");
            Triangle3D[] tris = Singleton.triangles.Where(tri => Vector3.Distance(tri.GetCenter(), position) < distance).ToArray();
            return tris[Random.Range(0, tris.Length)].GetCenter();
        }
        else
        {
            Debug.Log("NOT SPECIFIED");
            return Singleton.triangles[Random.Range(0, Singleton.triangles.Count)].GetCenter();
        }
    }

#endregion
    


#region GENERATION
    #if UNITY_EDITOR
    // Update is called once per frame
    void GenerateTriangles()
    {
        triangles = new();
        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        for (int i = 0; i < triangulation.indices.Length; i += 3)
        {
            int index1 = triangulation.indices[i];
            int index2 = triangulation.indices[i + 1];
            int index3 = triangulation.indices[i + 2];

            Vector3 p1 = triangulation.vertices[index1];
            Vector3 p2 = triangulation.vertices[index2];
            Vector3 p3 = triangulation.vertices[index3];

            Vector3 center = new(
                (p1.x + p2.x + p3.x) / 3,
                (p1.y + p2.y + p3.y) / 3,
                (p1.z + p2.z + p3.z) / 3
            );


            bool seen = false;
            foreach (Transform eye in transform)
            {
                if (!Physics.Raycast(eye.position, center - eye.position, out RaycastHit hitInfo, (eye.position - center).magnitude, LayerMask.GetMask("Default")) || (center - hitInfo.point).magnitude < 2F)
                {
                    seen = true;
                    break;
                }
            }

            if (seen)
            {
                triangles.Add(new Triangle3D(p1, p2, p3));
            }
        }
    }

    public void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.green;
        foreach (Triangle3D tri in triangles)
        {
            Gizmos.DrawLine(tri.v0, tri.v1);
            Gizmos.DrawLine(tri.v1, tri.v2);
            Gizmos.DrawLine(tri.v2, tri.v0);
        }
    }

    public void Bake()
    {
        string objectName = "SPAWN MESH";

        GenerateTriangles();
        EditorUtility.SetDirty(this);
    }
    #endif
#endregion
}