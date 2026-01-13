#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ModularFortressBuilder))]
public class ModularFortressBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ModularFortressBuilder generator = (ModularFortressBuilder)target;

        if (GUILayout.Button("Generate"))
        {
            generator.Generate();
        }

        if (GUILayout.Button("Clear"))
        {
            generator.Clear();
        }
    }   
}
#endif