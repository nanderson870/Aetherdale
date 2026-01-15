using UnityEngine;

public class ShopOfferingEffects : MonoBehaviour
{
    [SerializeField] MeshRenderer glowSphere;
    [SerializeField] Light light;

    public void SetRarity(Rarity rarity)
    {
        SetColor(ColorPalette.GetEmissiveColorForRarity(rarity));
    }

    public void SetColor(Color color)
    {
        glowSphere.material.SetColor("_Color", color);
        light.color = color;
    }

}
