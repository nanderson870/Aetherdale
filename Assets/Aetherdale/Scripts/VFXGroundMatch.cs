#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// Sets the "Color" attribute on the attached VFX to whatever color the object below this is
/// </summary>
[ExecuteInEditMode]
public class VFXGroundMatch : MonoBehaviour
{
    public Color color;

    // Update is called once per frame
    void Update()
    {
        if(Physics.Raycast(transform.position, -transform.up, out RaycastHit hitInfo, 0.3F, LayerMask.GetMask("Default")))
        {
            GetComponent<VisualEffect>().SetBool("Enabled", true);
            GameObject hitObject = hitInfo.collider.gameObject;
            if (hitObject == null)
            {
                return;
            }
            
            Renderer renderer = hitObject.GetComponent<Renderer>();

            if (renderer == null)
            {
                // TODO probably a terrain
                GetComponent<VisualEffect>().SetVector4("Color", Color.grey * 0.3F);

                return;
            }

            Texture2D tex = renderer.material.mainTexture as Texture2D;

            Color materialColor;
            if (renderer.material.HasColor("_Color"))
            {
                materialColor = renderer.material.color;
            }
            else if (renderer.material.HasColor("_BaseColor"))
            {
                materialColor = renderer.material.GetColor("_BaseColor");

            }
            else
            {
                materialColor = Color.grey;
            }

            if (tex == null)
            {
                // Flat color object
                color = materialColor;
            }
            else
            {
                // Sample texture
                Vector2 coords = hitInfo.textureCoord;
                coords.x *= tex.width;
                coords.y *= tex.height;

                color = tex.GetPixel((int)coords.x, (int)coords.y);
                color *= materialColor;
            }
            GetComponent<VisualEffect>().SetVector4("Color", color);

        }
        else
        {
            GetComponent<VisualEffect>().SetBool("Enabled", false);
        }

    }
}
