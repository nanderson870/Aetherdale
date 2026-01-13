#if UNITY_EDITOR

using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class AetherdaleAssetPostprocessor : AssetPostprocessor
{
    void OnPostprocessModel(GameObject gameObject)
    {
        if (assetPath.Contains("Assets/Aetherdale/Meshes/Levels/"))
        {
            MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();

            foreach (MeshFilter mf in meshFilters)
            {
                if (mf.name.Contains("ReplaceWith"))
                {
                    string objName = Regex.Replace(mf.name, "\\.[0-9]*", "");

                    string[] comps = objName.Split("_");
                    string replacementType = comps[1];

                    if (replacementType == "Tree")
                    {
                        string treeType = comps[2];
                        string[] potentialTrees = AssetDatabase.FindAssets("", new string[] { $"Assets/Aetherdale/Prefabs/Tree Prefabs/Halloween" });
                        string path = AssetDatabase.GUIDToAssetPath(potentialTrees[Random.Range(0, potentialTrees.Length - 1)]);

                        GameObject tree = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                        GameObject newObj = GameObject.Instantiate(tree, gameObject.transform);
                        newObj.transform.localPosition = mf.gameObject.transform.localPosition + new Vector3(0, -3.0F, 0);

                        newObj.transform.rotation = Quaternion.Euler(
                            new(0, Random.Range(0, 360), 0)
                        );
                    }
                    GameObject.DestroyImmediate(mf.gameObject);
                }

            }


        }
    }
}


#endif