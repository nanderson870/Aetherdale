#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Linq;
using System;
using System.Collections.Generic;

public class TraitAnalyzer : EditorWindow
{
    [MenuItem("Tools/Trait Analyzer")]
    static void ShowWindow()
    {
        GetWindow<TraitAnalyzer>("Trait Analyzer");
    }


    private void OnGUI()
    {
        if (GUILayout.Button("Get Trait Rarities"))
        {
            GetTraitRarities();
        }

        if (GUILayout.Button("List Common Traits"))  ListTraits(Rarity.Common);
        if (GUILayout.Button("List Uncommon Traits"))  ListTraits(Rarity.Uncommon);
        if (GUILayout.Button("List Rare Traits"))  ListTraits(Rarity.Rare);
        if (GUILayout.Button("List Epic Traits"))  ListTraits(Rarity.Epic);
        if (GUILayout.Button("List Legendary Traits"))  ListTraits(Rarity.Legendary);
    }

    private void GetTraitRarities()
    {
        Type[] traits = System.AppDomain.CurrentDomain.GetAssemblies()
                // alternative: .GetExportedTypes()
                .SelectMany(domainAssembly => domainAssembly.GetTypes())
                .Where(type => typeof(Trait).IsAssignableFrom(type)
                // alternative: => type.IsSubclassOf(typeof(B))
                && type != typeof(Trait)
                && ! type.IsAbstract
                ).ToArray();

        Dictionary<Rarity, int> rarityCounts = new();

        foreach (Type type in traits)
        {
            Trait trait = (Trait) Activator.CreateInstance(type);
            if (trait != null)
            {
                if (rarityCounts.ContainsKey(trait.GetRarity()))
                {
                    rarityCounts[trait.GetRarity()]++;
                }
                else
                {
                    rarityCounts[trait.GetRarity()] = 1;
                }
            }
        }
        
        
        for (int i = (int) Rarity.Common; i <= (int) Rarity.Cursed; i++)
        {
            Rarity rarity = (Rarity) i;
            rarityCounts.TryGetValue(rarity, out int count);

            Debug.Log($"{count} {rarity} traits");
        }
    }

    
    void ListTraits(Rarity rarity)
    {
        Type[] traits = System.AppDomain.CurrentDomain.GetAssemblies()
                // alternative: .GetExportedTypes()
                .SelectMany(domainAssembly => domainAssembly.GetTypes())
                .Where(type => typeof(Trait).IsAssignableFrom(type)
                // alternative: => type.IsSubclassOf(typeof(B))
                && type != typeof(Trait)
                && ! type.IsAbstract
                && ((Trait) Activator.CreateInstance(type)).GetRarity() == rarity
                ).ToArray();

        foreach (Type trait in traits)
        {
            Debug.Log(((Trait) Activator.CreateInstance(trait)).GetName());
        }
    }
}

#endif
