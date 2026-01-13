using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Outliner : MonoBehaviour
{
    public Color color = Color.white;

    Dictionary<Renderer, Renderer> renderersAndOutlines = new();

    Material outlineMaterial;

    bool outlineFades = false;
    float outlineFadeTime = 0;

    public static void Outline(GameObject target, Color color, float duration = 0)
    {
        Outliner outliner;
        if (!target.TryGetComponent(out outliner))
        {
            outliner = target.AddComponent<Outliner>();
        }

        outliner.color = color;
        outliner.Enable(duration);
    }

    public static void ClearOutline(GameObject target)
    {
        if (target.TryGetComponent(out Outliner outliner))
        {
            outliner.Disable();
        }
    }

    public void Awake()
    {
        outlineMaterial = Resources.Load<Material>("Materials/Outline");
    }

    public void Start()
    {
        CreateOutlineRenderers();
    }

    public void Update()
    {
        if (outlineFades && Time.time > outlineFadeTime)
        {
            Disable();
        }
    }

    public void Enable(float duration = 0)
    {
        enabled = true;
        
        foreach (var kvp in renderersAndOutlines)
        {
            kvp.Value.enabled = true;
        }

        if (duration == 0)
        {
            outlineFades = false;
        }
        else
        {
            outlineFades = true;
            outlineFadeTime = Time.time + duration;
        }
    }

    public void Disable()
    {
        enabled = false;

        foreach (var kvp in renderersAndOutlines)
        {
            if (kvp.Key == null || kvp.Value == null)
            {
                continue;
            }

            kvp.Value.enabled = false;
        }

    }

    void CreateOutlineRenderers()
    {
        foreach (MeshRenderer meshRenderer in GetComponentsInChildren<MeshRenderer>())
        {
            if (meshRenderer.gameObject.layer == LayerMask.NameToLayer("Outlines")) continue;

            MeshRenderer outline = AddMeshRendererOutlineDuplicate(meshRenderer);

            foreach (Material material in outline.materials)
            {
                if (meshRenderer.gameObject.transform.localScale.x != meshRenderer.gameObject.transform.localScale.y || meshRenderer.gameObject.transform.localScale.x != meshRenderer.gameObject.transform.localScale.z)
                {
                    Debug.LogWarning("Scale of object " + meshRenderer.gameObject + " is not uniform with itself, outliner will have issues");
                }

                material.SetFloat("_Size", material.GetFloat("_Size") / meshRenderer.gameObject.transform.localScale.x);
            }

            // For some reason outline looks much thinner on meshrenderers than smrs
            renderersAndOutlines.Add(meshRenderer, outline);
        }

        foreach (SkinnedMeshRenderer skinnedMeshRenderer in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (skinnedMeshRenderer.gameObject.layer == LayerMask.NameToLayer("Outlines")) continue;

            SkinnedMeshRenderer outline = AddMeshRendererOutlineDuplicate(skinnedMeshRenderer);

            foreach (Material material in outline.materials)
            {
                if (skinnedMeshRenderer.gameObject.transform.localScale.x != skinnedMeshRenderer.gameObject.transform.localScale.y || skinnedMeshRenderer.gameObject.transform.localScale.x != skinnedMeshRenderer.gameObject.transform.localScale.z)
                {
                    Debug.LogWarning("Scale of object " + skinnedMeshRenderer.gameObject + " is not uniform with itself, outliner will have issues");
                }

                material.SetFloat("_Size", material.GetFloat("_Size") / skinnedMeshRenderer.gameObject.transform.localScale.x);
            }

            renderersAndOutlines.Add(skinnedMeshRenderer, outline);
        }
    }

    private T AddMeshRendererOutlineDuplicate<T>(T renderer) where T : Renderer
    {
        GameObject outlineGameObject = new(); //Instantiate(renderer.gameObject, renderer.gameObject.transform.parent);
        {
            outlineGameObject.transform.position = renderer.transform.position;
            outlineGameObject.transform.rotation = renderer.transform.rotation;
            outlineGameObject.transform.parent = renderer.transform.parent;
            outlineGameObject.transform.localScale = renderer.transform.localScale;
            outlineGameObject.name = "Outline - " + renderer.gameObject.name;
            outlineGameObject.layer = LayerMask.NameToLayer("Outlines");
        }

        MeshFilter oldMeshFilter = renderer.gameObject.GetComponent<MeshFilter>();
        if (oldMeshFilter != null)
        {
            outlineGameObject.AddComponent<MeshFilter>().GetCopyOf(oldMeshFilter);
        }

        T originalRenderer = renderer.gameObject.GetComponent<T>();
        T outlineRenderer = outlineGameObject.AddComponent<T>().GetCopyOf(originalRenderer);
        Material[] mats = new Material[originalRenderer.materials.Length];
        
        for (int i = 0; i < mats.Length; i++)
        {
            mats[i] = outlineMaterial;
            mats[i].color = color;
            mats[i].SetColor("_Color", color);
        }

        outlineRenderer.materials = mats;

        return outlineRenderer;
    }

    void Clear()
    {
        foreach (var kvp in renderersAndOutlines)
        {
            if (kvp.Key != null && kvp.Value != null)
            {
                Destroy(kvp.Value.gameObject);
            }
        }

        renderersAndOutlines.Clear();
    }

    public void MeshesChanged()
    {
        Clear();

        CreateOutlineRenderers();
    }
}
