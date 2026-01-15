using UnityEngine;

public class TraitTomeMesh : MonoBehaviour
{
    [SerializeField] MeshRenderer iconRenderer;
    [SerializeField] MeshRenderer backIconRenderer;

    public void SetTrait(Trait trait)
    {
        Sprite sprite = trait.GetIcon();

        iconRenderer.material.SetTexture("_Texture", sprite.texture);
        backIconRenderer.material.SetTexture("_Texture", sprite.texture);
        
        Color emissiveRarityColor = ColorPalette.GetEmissiveColorForRarity(trait.GetRarity());

        iconRenderer.material.SetColor("_EmissiveColor", emissiveRarityColor * 1.2F);
        backIconRenderer.material.SetColor("_EmissiveColor", emissiveRarityColor * 1.2F);
    }
}
