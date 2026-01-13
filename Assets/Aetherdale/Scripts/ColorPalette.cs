using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Palette", menuName = "Aetherdale/Libraries/Color Palette", order = 0)]
public class ColorPalette : ScriptableObject
{
    public Color entityHealthDamaged;

    [Header("Items")]
    public Color rarityCommon;
    public Color rarityUncommon;
    public Color rarityRare;
    public Color rarityEpic;
    public Color rarityLegendary;
    public Color rarityCursed;

    [Header("Physical Theme")]
    public Color elementPhysical;
    public Color elementPhysicalSecondary;
    public FontStyle elementPhysicalFontStyle = FontStyle.Normal;

    [Header("Fire Theme")]
    public Color elementFire;
    public Color elementFireSecondary;
    public FontStyle elementFireFontStyle = FontStyle.Normal;

    [Header("Nature Theme")]
    public Color elementNature;
    public Color elementNatureSecondary;
    public FontStyle elementNatureFontStyle = FontStyle.Normal;

    [Header("Water Theme")]
    public Color elementWater;
    public Color elementWaterSecondary;
    public FontStyle elementWaterFontStyle = FontStyle.Normal;

    [Header("Storm Theme")]
    public Color elementStorm;
    public Color elementStormSecondary;
    public FontStyle elementStormFontStyle = FontStyle.Normal;

    [Header("Light Theme")]
    public Color elementLight;
    public Color elementLightSecondary;
    public FontStyle elementLightFontStyle = FontStyle.Normal;

    [Header("Dark Theme")]
    public Color elementDark;
    public Color elementDarkSecondary;
    public FontStyle elementDarkFontStyle = FontStyle.Normal;

    [Header("Healing Theme")]
    public Color elementHealing;
    public Color elementHealingSecondary;
    public FontStyle elementHealingFontStyle = FontStyle.Normal;

    [Header("True Damage Theme")]
    public Color elementTrueDamage;
    public Color elementTrueDamageSecondary;
    public FontStyle elementTrueDamageFontStyle = FontStyle.Italic;
    
    public static ColorPalette GetDefaultPalette()
    {
        return Resources.Load<ColorPalette>("Color Palette");
    }

    public static Color GetColorForRarity(Rarity rarity)
    {
        ColorPalette palette = GetDefaultPalette();
        return rarity switch
        {
            Rarity.Common => palette.rarityCommon,
            Rarity.Uncommon => palette.rarityUncommon,
            Rarity.Rare => palette.rarityRare,
            Rarity.Epic => palette.rarityEpic,
            Rarity.Legendary => palette.rarityLegendary,
            Rarity.Cursed => palette.rarityCursed,
            _ => palette.rarityCommon,
        };
    }

    public static Color GetPrimaryColorForElement(Element element)
    {
        ColorPalette palette = GetDefaultPalette();
        return element switch
        {
            Element.Fire => palette.elementFire,
            Element.Water => palette.elementWater,
            Element.Nature => palette.elementNature,
            Element.Storm => palette.elementStorm,
            Element.Light => palette.elementLight,
            Element.Dark => palette.elementDark,
            Element.Healing => palette.elementHealing,
            Element.TrueDamage => palette.elementTrueDamage,
            _ => palette.elementPhysical,
        };
    }

    public static Color GetSecondaryColorForElement(Element element)
    {
        ColorPalette palette = GetDefaultPalette();
        return element switch
        {
            Element.Fire => palette.elementFireSecondary,
            Element.Water => palette.elementWaterSecondary,
            Element.Nature => palette.elementNatureSecondary,
            Element.Storm => palette.elementStormSecondary,
            Element.Light => palette.elementLightSecondary,
            Element.Dark => palette.elementDarkSecondary,
            Element.Healing => palette.elementHealingSecondary,
            Element.TrueDamage => palette.elementTrueDamageSecondary,
            _ => palette.elementPhysicalSecondary,
        };
    }
}

