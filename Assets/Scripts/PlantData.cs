using UnityEngine;
using UnityEngine.Serialization;

public class PlantData : ScriptableObject
{
    [Header("Display")]
    public Plant plantPrefab;
    public Sprite icon;
    [Tooltip("the skill's icon in the plant info panel's skill frame, drawn at a whole number of canvas units per pixel " +
        "in its 66 unit window (up to 66 px; Acorn Knight's is 54). empty: the element's icon")]
    public Sprite skillIcon;

    // keeps icon following whatever sprite is currently on the prefab's "Visual" child (the
    // convention every plant prefab uses for its actual rendered sprite), so updating a plant's
    // art on the prefab doesn't also require manually re-dragging it onto this asset every time.
    // runs whenever this asset's Inspector validates (editor-only, never called in a build)
    private void OnValidate()
    {
        if (plantPrefab == null) return;
        Transform visual = plantPrefab.transform.Find("Visual");
        SpriteRenderer sr = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
        if (sr != null && sr.sprite != null)
            icon = sr.sprite;
    }
    public string plantName;
    public string displayName;
    public PlantRole role;
    public ElementalType elementalType;
    public DamageType damageType;
    public int sunCost;
    // max number of this plant that can be alive on the field at once. 0 = unlimited
    public int placementLimit = 0;
    // "tickets" this plant holds in an insect's weighted-random target roll (see
    // Insect.FindTauntWeightedPlantInRange / Plant.tauntValue), default 3, set per plant here
    public float tauntValue = 3f;
    // insects that start attacking this plant keep attacking it while it stays above
    // IronbarkAggroEffect.HealthThreshold health (see Insect.TryApplyIronbarkAggro)
    public bool retainsAggro;
    // tags plants that produce sun, wasps prefer these as targets
    public bool generatesSun;

    // dev-only label, never surfaced to players anywhere - just a way to tag which plants are
    // "elder" tier (currently Begonia and Carrot) versus the normal roster, for our own reference
    public enum PlantCategory { Normal, Elder }

    [Header("Dev Only")]
    public PlantCategory category = PlantCategory.Normal;

    // the bar widget sits just above this plant's own sprite on its own (see PlantWidget's
    // FollowSpriteTop). this nudges it for this plant only, in the widget's pixels
    [Header("Health Bar")]
    [Tooltip("widget pixels to move this plant's health bar up (or down, negative) from just above its sprite")]
    public int widgetHeightOffset = 0;

    [Header("Skill Tree")]
    public SkillTreeData skillTree;

    // extra stats THIS plant specifically can offer through fertilizers, on top of whatever the
    // generic pool / elemental presence already grant automatically. the picker only
    // lists stats not already covered by those automatic rules (see FertilizerStatRules) so this
    // never ends up duplicating something the plant would already offer anyway
    [Header("Fertilizer")]
    [RestrictedStatType]
    public StatType[] fertilizerPossibleStats;

    [Header("Core")]
    public float baseMaxHealth = 200f;
    public float baseAttackDamage;
    public float baseMagicPower;
    public float baseAttackSpeed;
    public float baseAttackRange;
    // seconds of windup before an attack actually lands/fires, at this plant's base Attack Speed -
    // see Plant.AttackChargeTime and Plant.BeginAnimatedAttackCharge
    public float baseAttackChargeTime;
    public int baseArmor;
    public int baseMagicArmor;
    // public float baseElementalEffectChance = 0.1f; (disabled, see Entity.cs)
    public float baseOnHitEffectiveness = 1f;

    [Header("Healing")]
    public float baseHealingBonus;
    public float baseHealingReceived;

    [Header("Resistances")]
    public float startingShield;
    public float baseTenacity;
    public float baseFireResistance;
    public float baseWaterResistance;
    public float basePoisonResistance;
    public float baseIceResistance;
    public float baseGrassResistance;
    public float baseWindResistance;
    public float baseDotResistance;
    public float baseHeatResistance;   // slows temperature rise in Hot weather, does not reduce the resulting damage
    public float baseColdResistance;   // slows temperature drop in Cold weather, does not reduce the resulting damage
    public float baseRespiration;      // slows Air depletion while Submerged, same shape as heat/cold resistance

    [Header("Offensive")]
    public float basePhysicalDamage;
    public float baseMagicDamage;
    public int baseArmorPenFlat;
    public float baseArmorPenPercent;
    public int baseMagicPenFlat;
    public float baseMagicPenPercent;
    public float baseLifesteal;
    [Tooltip("the share of an insect's targeting range this plant can be seen in, 1 = 100%. 0.5: an insect with a 6 targeting range only sees it within 3")]
    public float baseVisibility = 1f;
    // disabled: every chance-based proc that used to read this was converted to a deterministic
    // equivalent, so this has no reader left - kept out, not deleted, so already-serialized
    // fertilizer rolls and the BonusEffectChance ordinal in StatType stay valid
    // public float baseBonusEffectChance;
    public float baseCriticalChance = 0.05f;
    public float baseCriticalDamage = 1.5f;
    public float baseDotDamage;
    public float baseelementalAffinity;
    public float baseCoordinatedDamage;
    public float baseCounterDamage;

    [Header("Elemental Damage")]
    public float baseFireDamage;
    public float baseWaterDamage;
    public float baseGrassDamage;
    public float baseWindDamage;
    public float basePoisonDamage;
    public float baseIceDamage;

    [Header("Misc")]
    public float baseLightEmissionRange;
    public float baseMinionDamage;   // multiplier bonus on damage dealt by this plant's minions

    [Header("Shooter")]
    public float baseProjectileSpeed;
    public float baseMaxRange;
    public int basePiercing;
    public float baseProjectileHitboxRadius = 0.1f;
    public float baseProjectileVisualHitboxRadius = 0.1f;
    // this plant's own innate ability: its projectiles always emit light, regardless of Floral
    // Glow - e.g. Sunflower. own dedicated radius/color rather than reusing
    // baseLightEmissionRange, since a plant's own glow and its projectiles' glow may want to
    // look different
    public bool projectileEmitLight = false;
    public float projectileLightRadius = 0.5f;
    public float projectileLightIntensity = 0.4f;
    public Color projectileLightColor = Color.white;

    [Header("Lobber")]
    public float baseAoERadius;
    public float minFlightDuration = 0.5f;
    public float projectileHeight = 1.2f;

    [Header("Specific")]
    public float basePassiveCooldown;
    public float basePassiveDuration;
    public float baseSkillCooldown;
    public float baseSkillDuration;
    public float channelDuration;   // how long the plant cannot auto-attack while casting its skill
    public float baseSkillRadius;
    public float baseSkillDamageMultiplier;
    public float baseSkillHealth;
    public float basePassiveDamage;
    public float baseSkillDamage;

    [Header("Audio")]
    [Tooltip("played when this plant unleashes its attack (shot fired, melee swing, lob thrown)")]
    public SoundEffect attackSound;
    [Tooltip("played on the target when this plant's attack actually lands on it")]
    [FormerlySerializedAs("impactSound")]
    public SoundEffect hitSound;
    [Tooltip("played when this plant's skill is activated (every plant should have one)")]
    public SoundEffect skillSound;

    public static string ElementalTag(ElementalType t) => t switch
    {
        ElementalType.Fire    => "<color=orange>Fire</color>",
        ElementalType.Water   => "<color=#4FC3F7>Water</color>",
        ElementalType.Grass  => "<color=green>Grass</color>",
        ElementalType.Wind    => "<color=#B2EBF2>Wind</color>",
        ElementalType.Poison  => "<color=purple>Poison</color>",
        ElementalType.Ice     => "<color=#00FFFF>Ice</color>",
        _                     => t.ToString()
    };

    public static string ElementalColor(ElementalType t) => t switch
    {
        ElementalType.Fire    => "orange",
        ElementalType.Water   => "#4FC3F7",
        ElementalType.Grass  => "green",
        ElementalType.Wind    => "#B2EBF2",
        ElementalType.Poison  => "purple",
        ElementalType.Ice     => "#00FFFF",
        _                     => "white"
    };

    // the element colour above as a Color, for tinting greyscale art (the plant bar's bevel portrait
    // frame). Neutral is white, which leaves the art untinted
    public static Color ElementColor(ElementalType t) =>
        ColorUtility.TryParseHtmlString(ElementalColor(t), out Color color) ? color : Color.white;

    // the element colour mixed 60% toward white, for a portrait's backdrop: light enough that the frame
    // (the full element colour) still stands out against it
    public static Color ElementBackdropColor(ElementalType t)
    {
        Color c = ElementColor(t);
        return new Color(c.r + (1f - c.r) * 0.6f, c.g + (1f - c.g) * 0.6f, c.b + (1f - c.b) * 0.6f, 1f);
    }

    // the plant info panel's colour (Downloads\gd_plant_info_panel): its panel, insets and strips.
    // fire is tommy's orange, the other five came with the pack as proposals, neutral (insects) is a
    // proposal too
    public static Color ElementPanelColor(ElementalType t) => t switch
    {
        ElementalType.Fire    => new Color32(0xB8, 0x6E, 0x00, 0xFF),
        ElementalType.Water   => new Color32(0x2A, 0x6D, 0xB0, 0xFF),
        ElementalType.Ice     => new Color32(0x1F, 0x9F, 0xBF, 0xFF),
        ElementalType.Wind    => new Color32(0x4F, 0x94, 0x84, 0xFF),
        ElementalType.Grass   => new Color32(0x3A, 0x8A, 0x2E, 0xFF),
        ElementalType.Poison  => new Color32(0x7A, 0x35, 0x92, 0xFF),
        _                     => new Color32(0x6B, 0x6B, 0x6B, 0xFF),
    };

    // the 1 pixel shadow under the panel's text: the panel colour times 0.38, rounded down
    public static Color ElementTextShadowColor(ElementalType t)
    {
        Color32 c = ElementPanelColor(t);
        return new Color32((byte)(c.r * 38 / 100), (byte)(c.g * 38 / 100), (byte)(c.b * 38 / 100), 0xFF);
    }

    public static string DamageTypeLabel(DamageType t) => t switch
    {
        DamageType.Magic    => "<color=#FFB6C1><b>Magic Damage</b></color>",
        DamageType.Physical => "<color=#A0522D><b>Physical Damage</b></color>",
        DamageType.True     => "<color=white><b>True Damage</b></color>",
        _                   => $"<b>{t} Damage</b>"
    };

    // passive line shared by every plant with retainsAggro
    public static string RetainAggroLine() =>
        $"While over <color=green><b>{IronbarkAggroEffect.HealthThreshold * 100f:F0}%</b></color> Health, insects that initiate an Attack on this plant will retain aggro.";

}
