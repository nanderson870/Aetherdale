using UnityEngine;

public class TraitTomeMesh : MonoBehaviour
{
    [SerializeField] MeshRenderer iconRenderer;
    [SerializeField] MeshRenderer backIconRenderer;

    public void SetTrait(Trait trait)
    {
        Sprite sprite = trait.GetIcon();

        Color rarityColor = ColorPalette.GetColorForRarity(trait.GetRarity());

        iconRenderer.material.SetTexture("_Texture", sprite.texture);
        backIconRenderer.material.SetTexture("_Texture", sprite.texture);
        
        Color.RGBToHSV(rarityColor, out float h, out float s, out float v);

        if (s == 0)
        {
            v = 0.6F; // Pure white is a bit too bright to have 1.0 value and emission
        }
        else
        {
            v = 1.0F; // Crank that puppy up
        }

        Color emissiveRarityColor = Color.HSVToRGB(h, s, v);
        iconRenderer.material.SetColor("_EmissiveColor", emissiveRarityColor * 1.2F);
        backIconRenderer.material.SetColor("_EmissiveColor", emissiveRarityColor * 1.2F);
    }
}
