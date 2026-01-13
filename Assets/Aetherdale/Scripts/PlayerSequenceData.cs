
using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

// public class PlayerSequenceData
// {

//     // The following should be put into player-specific sequence data eventually. Keys are player ids.
//     public readonly TraitList traits = new();

//     [NonSerialized] public readonly List<List<Trait>> pendingTraitOptions = new();

//     public void AddTrait(Trait trait)
//     {
//         traits.AddTrait(trait);
//     }

//     public bool HasTraitOptionsAvailable()
//     {
//         return pendingTraitOptions.Count > 0;
//     }

//     public List<Trait> GetNextTraitOptions()
//     {
//         List<Trait> ret = pendingTraitOptions[0];
//         pendingTraitOptions.RemoveAt(0);
//         return ret;
//     }

// }