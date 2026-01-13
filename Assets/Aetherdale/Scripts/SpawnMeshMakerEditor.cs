
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SpawnMesh))]
public class SpawnMeshMakerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SpawnMesh maker = (SpawnMesh)target;

        if (GUILayout.Button("Generate"))
        {
            maker.Bake();
        }
    }  
}
#endif