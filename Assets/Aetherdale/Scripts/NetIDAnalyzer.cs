#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Linq;
using System;
using System.Collections.Generic;

public class NetIDAnalyzer : EditorWindow
{
    [MenuItem("Tools/Net ID Analyzer")]
    static void ShowWindow()
    {
        GetWindow<NetIDAnalyzer>("Net ID Analyzer");
    }


    private void OnGUI()
    {
        if (GUILayout.Button("List Net IDs"))
        {
            ListNetIDs();
        }
    }

    private void ListNetIDs()
    {
        Debug.Log("listing...");
        foreach (var asset in AssetDatabase.FindAssets("t:NetworkIdentity"))
        {
            Debug.Log(asset);
        }

    }
}

#endif
