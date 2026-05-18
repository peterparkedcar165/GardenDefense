using UnityEngine;
using System.Collections;

public class BogIris : Shooter
{
    private float
    bAD = 70f,
    bAS = 0.8f,
    bAR = 3f,
    bPS = 3f,
    bMR = 20f;
    private int bP = 0;

    [SerializeField] private GameObject geyserPrefab;
    [SerializeField] private SpriteRenderer closedVisual;
    [SerializeField] private SpriteRenderer openVisual;

    private bool isOpen = false;
    private float cycleTimer = 0f;
    private float sunTickTimer = 0f;
    private float healTickTimer = 0f;
    private GameObject _indicatorPrefab;
    private const float totalHeal = 200f;
    private float SunInterval => 2f * (passiveCooldown / basePassiveCooldown);
    private float OpenDuration => 6f + 2f * effectivePath2Level;
    private int SunGenerated => 2 + effectivePath2Level;
    private float GeyserRadius => 1.25f + 0.15f * effectivePath3Level;
    private float KnockUpHeight => ScaleCC((3f + 1f * effectivePath3Level) * skillDuration);
    private float KnockUpForce => Mathf.Sqrt(2f * Insect.gravity * (KnockUpHeight)); // knock up height dictates force
    private float GeyserDamage => (75f + 15f * effectivePath3Level) + 1.33f * attackDamage;

    protected override void Awake()
    {
        elementalType = ElementalType.Water;
        damageType = DamageType.Magic;
        baseAttackDamage = bAD;
        baseAttackSpeed = bAS;
        baseAttackRange = bAR;
        baseProjectileSpeed = bPS;
        baseMaxRange = bMR;
        basePiercing = bP;
        basePassiveCooldown = 12f;
        baseSkillCooldown = 30f;
        baseSkillDuration = 1f;
        base.Awake();
        _indicatorPrefab = Resources.Load<GameObject>("DamageIndicator");
        SetVisualState(false);
    }

    protected override void Update()
    {
        base.Update();
        UpdatePassive();
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
                Heal(totalHeal / passiveCooldown);
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
                GameManager.instance.AddSun(SunGenerated);
                GameObject indicator = Object.Instantiate(
                    _indicatorPrefab,
                    transform.position + new Vector3(0.25f, 0.5f, 0f),
                    Quaternion.identity);
                indicator.GetComponent<DamageIndicator>().Initialize($"+{SunGenerated} Sun", new Color(1f, 1f, 0f));
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

    private void SetVisualState(bool open)
    {
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
        bool isRaining = WeatherManager.instance != null && WeatherManager.instance.weather == WeatherType.Rain;
        yield return new WaitForSeconds(isRaining ? 1f : 2f);
        if (geyserPrefab == null) yield break;
        GameObject obj = Instantiate(geyserPrefab, position, Quaternion.identity);
        obj.GetComponent<Geyser>()?.Initialize(position, GeyserRadius, skillDuration, GeyserDamage, KnockUpForce, this);
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = bAD + 8f * level;
    }

    public override void OnPath2Upgrade(int level) { }
    public override void OnPath3Upgrade(int level) { }

    public override PlantBaseStats GetBaseStats() => new PlantBaseStats
    {
        attackDamage = bAD, attackSpeed = bAS, attackRange = bAR,
        skillCooldown = 30f, passiveCooldown = 12f, skillDuration = 1f,
        sunGenerated = 2f, sunInterval = 2f, openDuration = 6f,
        geyserDamage = 75f + 1.33f * bAD, knockUpHeight = 3f,
    };

    public override string GetName() => "<b><color=#4FC3F7>Bog Iris</color></b>";

    public override string GetDescription()
        => $"The {GetName()} is self-sufficient, providing herself with regeneration as well as generating sun for the garden.";

    public override string GetAttackDescription()
        => $"Fires a water bolt at a single target dealing <color=green><b>{attackDamage:F0}</b></color> <color=#4FC3F7>Water</color> <color=#FFB6C1>Magic</color> damage.";

    public override string GetPassiveDescription()
        => $"The {GetName()} cycles between an <b><color=#4FC3F7>open</color></b> (<color=green><b>{OpenDuration:F0}s</b></color>) and <b><color=#4FC3F7>closed</color></b> (<color=green><b>{passiveCooldown:F1}s</b></color>) state.\n\n" +
           $"In <b><color=#4FC3F7>open</color></b> form, she generates <color=green><b>{SunGenerated}</b></color> Sun every <color=green><b>{SunInterval:F1}</b></color> seconds.\n\n" +
           $"In <b><color=#4FC3F7>closed</color></b> form, she regenerates <color=green><b>{totalHeal}</b></color> HP over <color=green><b>{passiveCooldown:F1}</b></color> seconds.";

    public override string GetSkillDesription()
        => $"Target a location. After a brief delay, a geyser erupts, dealing <color=green><b>{GeyserDamage:F0}</b></color> <color=#4FC3F7>Water</color> <color=#FFB6C1>Magic</color> damage and knocking all insects airborne by <color=green><b>{KnockUpHeight:F0}</b></color> units.";

    public override string GetPath1Description()
        => $"Attack:\n\n{GetAttackDescription()}\n\nIncrease Attack Damage by <color=green><b>8</b></color> per level. [<color=green><b>+{8 * effectivePath1Level}</b></color>]\n\n" +
           $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>";

    public override string GetPath2Description()
        => $"Passive:\n\n{GetPassiveDescription()}\n\nIncrease the duration of the <b><color=#4FC3F7>open</color></b> state by <color=green><b>2</b></color> seconds per level. [<color=green><b>+{2 * effectivePath2Level}s</b></color>]\n\n" +
           $"Increase Sun generated per tick by <color=green><b>1</b></color> per level. [<color=green><b>+{1 * effectivePath2Level}</b></color>]\n\n" +
           $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>";

    public override string GetPath3Description()
        => $"Skill:\n\n{GetSkillDesription()}\n\nIncrease the flat component of geyser damage by <color=green><b>15</b></color> per level. [<color=green><b>+{15 * effectivePath3Level}</b></color>]\n\n" +
           $"Increase the knock-up height by <color=green><b>1</b></color> unit per level. [<color=green><b>+{effectivePath3Level}</b></color>]\n\n" +
           $"Increase the radius of the geyser by <color=green><b>0.15</b></color> per level. [<color=green><b>+{0.15f * effectivePath3Level:F2}</b></color>]\n\n" +
           $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>";
}
