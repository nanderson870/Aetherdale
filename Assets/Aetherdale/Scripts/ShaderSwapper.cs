using System.Linq;
using UnityEngine;

/// <summary>
/// Swaps the shaders of an entity temporarily. Attach this as a child
/// of an entity.
/// </summary>
public class ShaderSwapper : MonoBehaviour
{
    public Shader shader;

    [Tooltip("Duration of shader swap - set to 0 for indefinite duration")]
    public float duration = 0;

    float startTime;
    Entity entity;

    void Start()
    {
        startTime = Time.time;

        entity = GetComponentInParent<Entity>();
        if (entity == null)
        {
            Debug.LogError("ShaderSwapper was created but not parented to an entity");
            return;
        }

        foreach (Entity.RendererMaterialColor rmc in entity.defaultMaterialsColors)
        {
            Material[] newMats = new Material[rmc.materialsColors.Count()];
            for (int i = 0; i < rmc.renderer.materials.Count() && i < newMats.Count(); i++)
            {
                newMats[i] = new(rmc.renderer.materials[i]);
                newMats[i].shader = shader;
            }

            rmc.renderer.materials = newMats;
        }
    }

    void Update()
    {
        if ((Time.time - startTime) >= duration)
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        Entity entity = GetComponentInParent<Entity>();
        if (entity == null)
        {
            Debug.LogError("ShaderSwapper was destroyed but not parented to an entity");
            return;
        }

        entity.ResetMaterials();
    }

    public void SetFloat(string name, float value)
    {
        foreach (Entity.RendererMaterialColor rmc in entity.defaultMaterialsColors)
        {
            foreach (Material material in rmc.renderer.materials)
            {
                if (material.HasFloat(name))
                {
                    material.SetFloat(name, value);
                }
            }

        }
    }
}
