#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ModularFortressGenerator))]
public class ModularFortressGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ModularFortressGenerator generator = (ModularFortressGenerator)target;

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