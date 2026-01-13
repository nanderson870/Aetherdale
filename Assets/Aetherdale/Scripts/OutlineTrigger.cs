using System.Collections.Generic;
using UnityEngine;

public class OutlineTrigger : MonoBehaviour
{
    public Color triggerZoneColor = Color.white;
    public Color outlineColor = Color.red;

    List<Entity> outlinedEntities = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetZoneOutlineColor(triggerZoneColor);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out Entity entity))
        {
            Outliner.Outline(entity.gameObject, outlineColor);
            outlinedEntities.Add(entity);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.TryGetComponent(out Entity entity))
        {
            Outliner.ClearOutline(entity.gameObject);
            outlinedEntities.Remove(entity);
        }
    }

    void OnDestroy()
    {
        foreach (Entity entity in outlinedEntities)
        {
            Outliner.ClearOutline(entity.gameObject);
        }
    }

    public void SetZoneOutlineColor(Color color)
    {
        foreach (MeshRenderer meshRenderer in GetComponentsInChildren<MeshRenderer>())
        {
            Material[] mats = meshRenderer.materials;
            foreach (Material material in mats)
            {
                material.color = color;
            }

            meshRenderer.materials = mats;
        }
    }

    public void SetScale(Vector3 scale)
    {
        transform.localScale = scale;
    }
}
