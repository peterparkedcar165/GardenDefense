using UnityEngine;
using System.Collections;

public class BogIris : Shooter
{
    [SerializeField] private GameObject geyserPrefab;
    [SerializeField] private SpriteRenderer closedVisual;
    [SerializeField] private SpriteRenderer openVisual;

    private SpriteRenderer _rootRenderer;
    private bool isOpen = false;
    private float cycleTimer = 0f;
    private float sunTickTimer = 0f;
    private float healTickTimer = 0f;
    private float TotalHeal => (BogData?.baseClosedHeal ?? 200f) + (BogData?.path2ClosedHealPerLevel ?? 20f) * effectivePath2Level;

    private BogIrisData BogData => data as BogIrisData;

    private float SunInterval => 2f * (passiveCooldown / basePassiveCooldown);
    private float OpenDuration => ((BogData?.baseOpenDuration ?? 0f) + (BogData?.path2OpenDurationPerLevel ?? 2f) * effectivePath2Level) * (1 + passiveDuration);
    private int SunGenerated => (BogData?.baseSunGenerated ?? 0) + (BogData?.path2SunPerLevel ?? 1) * effectivePath2Level;
    private float GeyserRadius => skillRadius + (BogData?.path3GeyserRadiusPerLevel ?? 0.15f) * effectivePath3Level;
    private float KnockUpHeight => ScaleCC(((BogData?.baseKnockUpHeight ?? 0f) + (BogData?.path3KnockUpPerLevel ?? 1f) * effectivePath3Level) * skillDuration);
    private float KnockUpForce => Mathf.Sqrt(2f * Insect.gravity * KnockUpHeight);
    private float GeyserDamage => (BogData?.baseGeyserDamage ?? 0f) + (BogData?.path3GeyserDamagePerLevel ?? 15f) * effectivePath3Level + skillDamageMultiplier * magicPower;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        _rootRenderer = GetComponent<SpriteRenderer>();
        SetVisualState(false);
    }

    protected override void Update()
    {
        base.Update();
        UpdatePassive();
    }

    protected override bool GetPassiveBarVisible() => true;
    protected override float GetPassiveBarFill()
    {
        if (isOpen)
            return SunInterval > 0f ? Mathf.Clamp01(sunTickTimer / SunInterval) : 0f;
        return passiveCooldown > 0f ? Mathf.Clamp01(cycleTimer / passiveCooldown) : 0f;
    }

    private void UpdatePassive()
    {
        cycleTimer += Time.deltaTime;

        if (!isOpen)
        {
            healTickTimer += Time.deltaTime;
            if (healTickTimer >= 1f)
            {
                healTickTimer -= 1f;
                Heal(TotalHeal / passiveCooldown);
            }
            if (cycleTimer >= passiveCooldown)
            {
                isOpen = true;
                cycleTimer = 0f;
                sunTickTimer = 0f;
                SetVisualState(true);
            }
        }
        else
        {
            sunTickTimer += Time.deltaTime;
            if (sunTickTimer >= SunInterval)
            {
                sunTickTimer -= SunInterval;
                int granted = GenerateSun(SunGenerated);
                SunIndicator.Spawn(transform.position + new Vector3(0.25f, 0.5f, 0f), granted);
            }
            if (cycleTimer >= OpenDuration)
            {
                isOpen = false;
                cycleTimer = 0f;
                healTickTimer = 0f;
                SetVisualState(false);
            }
        }
    }

    protected override SpriteRenderer GetMainRenderer()
    {
        return isOpen ? openVisual : closedVisual;
    }

    private void SetVisualState(bool open)
    {
        ResetOutlineRenderers();
        if (_rootRenderer != null) _rootRenderer.enabled = !open;
        if (closedVisual != null) closedVisual.gameObject.SetActive(!open);
        if (openVisual != null) openVisual.gameObject.SetActive(open);
    }

    protected override void Shoot(Vector3 target)
    {
        if (projectilePrefab == null) return;
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        BogIrisProjectile bogProj = proj.GetComponent<BogIrisProjectile>();
        if (bogProj != null)
        {
            bogProj.SetTarget(FindTarget());
            bogProj.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
        }
    }

    public override void ActivateSkill()
    {
        SkillTargetingManager.instance.BeginTargeting(GeyserRadius, OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        skillCooldownTimer = skillCooldown;
        StartCoroutine(SpawnGeyser(position));
    }

    private IEnumerator SpawnGeyser(Vector3 position)
    {
        bool isRaining = WeatherManager.instance != null && WeatherManager.instance.HasWeather(WeatherType.Rain);
        yield return new WaitForSeconds(isRaining ? 0.5f : 1f);
        if (geyserPrefab == null) yield break;
        GameObject obj = Instantiate(geyserPrefab, position, Quaternion.identity);
        obj.GetComponent<Geyser>()?.Initialize(position, GeyserRadius, skillDuration, GeyserDamage, KnockUpForce, this);
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (BogData?.path1AttackDamagePerLevel ?? 8f) * level;
    }

    public override void OnPath2Upgrade(int level) { }
    public override void OnPath3Upgrade(int level) { }

    public override string GetDescription() =>
        $"The {GetName()} is self-sufficient, providing herself with regeneration as well as generating sun for the garden.";

    public override string GetPath1Description()
    {
        float adpl = BogData?.path1AttackDamagePerLevel ?? 8f;
        return $"Attack:\n\n" +
               $"Fires a water bolt at a single target dealing <color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage.\n\n" +
               $"Increase Attack Damage by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>";
    }

    public override string GetPath2Description()
    {
        float opendurpl = BogData?.path2OpenDurationPerLevel ?? 2f;
        int sunpl       = BogData?.path2SunPerLevel          ?? 1;
        return $"Passive:\n\n" +
               $"Cycles between an <b><color=#4FC3F7>open</color></b> (<color=green><b>{OpenDuration:F0}s</b></color>) and <b><color=#4FC3F7>closed</color></b> (<color=green><b>{passiveCooldown:F1}s</b></color>) state.\n\n" +
               $"In <b><color=#4FC3F7>open</color></b> form, generates <color=green><b>{SunGenerated}</b></color> Sun every <color=green><b>{SunInterval:F1}</b></color> seconds.\n\n" +
               $"In <b><color=#4FC3F7>closed</color></b> form, regenerates <color=green><b>{TotalHeal}</b></color> HP over <color=green><b>{passiveCooldown:F1}</b></color> seconds.\n\n" +
               $"Increase the duration of the <b><color=#4FC3F7>open</color></b> state by <color=green><b>{opendurpl:F0}</b></color> seconds per level. [<color=green><b>+{opendurpl * effectivePath2Level:F0}s</b></color>]\n\n" +
               $"Increase Sun generated per tick by <color=green><b>{sunpl}</b></color> per level. [<color=green><b>+{sunpl * effectivePath2Level}</b></color>]\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>";
    }

    public override string GetPath3Description()
    {
        float dmgpl    = BogData?.path3GeyserDamagePerLevel  ?? 15f;
        float knockpl  = BogData?.path3KnockUpPerLevel        ?? 1f;
        float radiuspl = BogData?.path3GeyserRadiusPerLevel   ?? 0.15f;
        return $"Skill:\n\n" +
               $"Target a location. After a brief delay, a geyser erupts, dealing <color=green><b>{(BogData?.baseGeyserDamage ?? 0f) + dmgpl * effectivePath3Level:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage and knocking all insects airborne by <color=green><b>{KnockUpHeight:F0}</b></color> units.\n\n" +
               $"Scaling: <color=#FFB6C1><b>{skillDamageMultiplier * 100f:F0}%</b></color> Magic Power\n\n" +
               $"Increase the flat component of geyser damage by <color=green><b>{dmgpl:F0}</b></color> per level. [<color=green><b>+{dmgpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase the knock-up height by <color=green><b>{knockpl:F0}</b></color> unit per level. [<color=green><b>+{knockpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase the radius of the geyser by <color=green><b>{radiuspl:F2}</b></color> per level. [<color=green><b>+{radiuspl * effectivePath3Level:F2}</b></color>]\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>";
    }
}
