using System;
using System.Collections;
using System.Collections.Generic;
using Aetherdale;
using FMODUnity;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;


[CreateAssetMenu(fileName = "Aetherdale Data", menuName = "Aetherdale/Data", order = 0)]
public class AetherdaleData : ScriptableObject
{
    public static AetherdaleData GetAetherdaleData()
    {
        return Resources.Load<AetherdaleData>("Aetherdale Data");
    }

    public PlayerWraith wraithPrefab;
    public Entity[] entities;

    public Faction defaultEnemyFaction;

    public QuestData starterQuest;

    public Pickup healingOrbPickup;

    public ItemData aetherItem;
    public ItemData goldCoinsItem;
    public WeaponBehaviourPickupWrapper weaponBehaviourPickupWrapperPrefab;

    
    public AetherdaleNetworkManager steamNetworkManagerPrefab;
    public AetherdaleNetworkManager nonsteamNetworkManagerPrefab;


    [Header("Trait Sprites")]
    public Sprite acrobatSprite;
    public Sprite agilitySprite;
    public Sprite armsRaceSprite;
    public Sprite artificerSprite;
    public Sprite attunementSprite;
    public Sprite berserkSprite;
    public Sprite bloodlustSprite;
    public Sprite absorptionSprite;
    public Sprite catalyzeSprite;
    public Sprite celeritySprite;
    public Sprite cursedCoinSprite;
    public Sprite cutpurseSprite;
    public Sprite deathbringerSprite;
    public Sprite elementalStrikeSprite;
    public Sprite expertiseSprite;
    public Sprite explosiveSprite;
    public Sprite featherweightSprite;
    public Sprite ferocitySprite;
    public Sprite fortitudeSprite;
    public Sprite harvestingSprite;
    public Sprite hasteSprite;
    public Sprite lethalAccuracySprite;
    public Sprite luckySprite;
    public Sprite masterySprite;
    public Sprite narrowMindedSprite;
    public Sprite obliterateSprite;
    public Sprite persistenceSprite;
    public Sprite potentialSprite;
    public Sprite primordialSpiralSprite;
    public Sprite rampageSprite;
    public Sprite recoverySprite;
    public Sprite recklessSprite;
    public Sprite seekingSpiritsSprite;
    public Sprite shadowstepSprite;
    public Sprite shapeshifterSprite;
    public Sprite sharpenedSprite;
    public Sprite splitshotSprite;
    public Sprite transcendentSprite;
    public Sprite unyieldingSprite;
    public Sprite vampirismSprite;
    public Sprite voltageSprite;
    public Sprite wisdomSprite;


    [Header("Misc Sprites")]
    public Sprite noSelectionIcon;
    public Sprite defaultItemIcon;
    public Sprite defaultSwordIcon;
    public Sprite defaultAxeIcon;
    public Sprite defaultSpearIcon;
    public Sprite defaultCrossbowIcon;

    public Sprite brindleberryMuffinIcon;
    public Sprite blazeBombIcon;


    [Header("Projectiles")]
    public Projectile blazeBombProjectile;
    public Projectile thrownWeaponHolderProjectile;
    public Projectile wraithBoltProjectile; // TODO probably temporary location
    public GuaranteedSeekingProjectile seekingSpiritsProjectile;


    [Header("Misc Prefabs")]
    public Chest rustyChestPrefab;
    public Chest silverChestPrefab;
    public Chest goldChestPrefab;
    public Chest diamondChestPrefab;
    public DerboTable derboTablePrefab;
    public ChallengeTablet challengeTabletPrefab;
    public CursedShrine cursedShrinePrefab;
    public TraitTome traitTomePrefab;
    public TraitTomeMesh traitTomePreviewPrefab;
    public GameObject blazeBombHeldPrefab;
    public GameObject brindeberryMuffinHeldPrefab;
    public AreaOfEffect lightningStrikeAOE;
    public AreaOfEffect catalyzeAOE;
    public ItemData cursedCoinItemData;
    public GameObject levelUpVFXPrefab;
    public ChainLightningBolt voltageTraitBolt;
    public Effect natureEliteRegenEffect;
    public Effect unyieldingTraitEffect;
    public Effect fireEliteBurnEffect;
    public ResurrectionEffect transcendentEffect;
    public AreaOfEffect fireEliteDeathAOE;
    public LightEliteProjectileCharger lightEliteProjectileCharger;
    public AreaOfEffect spinningCurseScytheAOE;
    public Projectile sword1HWindSlashProjectile;
    public OutlineTrigger outlineTriggerSphere;
    public Revivable revivablePrefab;



    [Header("Sounds")]
    public AetherdaleSoundData soundData;

    [Header("VFX")]
    public AetherdaleVFXData vfxData;
}

[Serializable]
public class AetherdaleSoundData
{
    public EventReference transformSound;

    public EventReference defaultAttackSound1HSword;
    public EventReference defaultAttackSound2HSword;
    public EventReference defaultAttackSoundSpear;
    public EventReference defaultAttackSoundCrossbow;

    public EventReference defaultJumpAttackSound1HSword;

    public EventReference spiralProcSound;

    public EventReference defaultMeleeEquipSound;
    public EventReference defaultCrossbowEquipSound;

    public EventReference fireEliteIdleSound;

    public EventReference obliterateSound;

    public EventReference resurrectedSound;

    public FootstepsData footsteps;
    public TraitsData traits;
    [Serializable]
    public class FootstepsData
    {
        public EventReference dirtLight;
        public EventReference dirtMedium;
        public EventReference dirtHeavy;
    }

    [Serializable]
    public class TraitsData
    {
        public EventReference traitAcquiredCommon;
        public EventReference traitAcquiredUncommon;
        public EventReference traitAcquiredRare;
        public EventReference traitAcquiredEpic;
        public EventReference traitAcquiredLegendary;
        
        public EventReference transcendentAcquired;
    }
}

[Serializable]
public class AetherdaleVFXData
{
    public VisualEffect footstepsVFXPrefab;
}