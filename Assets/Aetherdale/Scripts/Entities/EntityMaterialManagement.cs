using System;
using UnityEngine;

[System.Serializable]
public class RendererMaterialColor
{
    public Renderer renderer;
    public MaterialColor[] materialsColors;

    public RendererMaterialColor(Renderer renderer, MaterialColor[] materialsColors)
    {
        this.renderer = renderer;
        this.materialsColors = materialsColors;
    }
}

[System.Serializable]
public enum EliteOverrideMode
{
    NotOverridden=0,
    OverrideWithPrimary=1,
    OverrideWithSecondary=2,
}

[Serializable]
public class MaterialColor
{
    public Material material;
    public Color color;

    public EliteOverrideMode eliteOverride = EliteOverrideMode.NotOverridden;

    public MaterialColor(Material material, Color color)
    {
        this.material = material;
        this.color = color;
    }
}