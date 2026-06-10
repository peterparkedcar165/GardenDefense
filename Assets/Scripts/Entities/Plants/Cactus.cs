using UnityEngine;
using System.Collections.Generic;

public class Cactus : Shooter
{
    private float _tauntTickTimer;
    private const float TauntTickInterval  = 0.25f;
    private const float TauntEffectDuration = 0.35f;

    private readonly HashSet<Insect> _volleyHit = new HashSet<Insect>();

    private CactusData CactData => data as CactusData;
    private float ShieldAmount  => (CactData?.baseShieldAmount ?? 100f) + (CactData?.path3ShieldPerLevel ?? 10f) * effectivePath3Level;
    private float SkillDuration => data.baseSkillDuration + (CactData?.path3SkillDurationPerLevel ?? 2f) * effectivePath3Level;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    protected override void Update()
    {
        base.Update();

        if (HasEffect<ShieldEffect>())
        {
            _tauntTickTimer += Time.deltaTime;
            if (_tauntTickTimer >= TauntTickInterval)
            {
                _tauntTickTimer -= TauntTickInterval;
                ApplyTauntInRange();
            }
        }
        else
        {
            _tauntTickTimer = 0f;
        }
    }

    protected override void Shoot(Vector3 target)
    {
        if (projectilePrefab == null) return;
        _volleyHit.Clear();
        int needleBase  = CactData?.path1NeedlesBase      ?? 8;
        int needleLevel = CactData?.path1NeedlesPerLevel  ?? 3;
        int needleCount = needleBase + needleLevel * effectivePath1Level;
        float angleStep = 360f / needleCount;

        for (int i = 0; i < needleCount; i++)
        {
            float rad = i * angleStep * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
            Vector3 targetPos = transform.position + dir * maxRange;

            GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            CactusNeedleProjectile needle = proj.GetComponent<CactusNeedleProjectile>();
            needle?.Initialize(targetPos, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
        }
    }

    protected override void OnHitByInsect(Insect attacker)
    {
        if (!IsAlive || !attacker.IsAlive) return;
        attacker.Damage(attacker.attackDamage * 1.5f, damageType, elementalType, this, false, new DamageTag[] { DamageTag.PassiveDamage, DamageTag.Counter });
        ApplyPunctured(attacker, 1 + effectivePath2Level);
    }

    public void OnNeedleHit(Insect insect, float baseDamage)
    {
        if (!insect.IsAlive) return;
        float dmg = _volleyHit.Contains(insect) ? baseDamage * 0.5f : baseDamage;
        _volleyHit.Add(insect);
        insect.Damage(dmg, damageType, elementalType, this, true, new DamageTag[] { DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget });
        ApplyPunctured(insect, 1 + effectivePath2Level);
    }

    private void ApplyPunctured(Insect insect, int stacks)
    {
        if (!insect.IsAlive) return;
        int current = insect.GetEffect<PuncturedEffect>()?.level ?? 0;
        int total = Mathf.Min(current + stacks, 100);
        if (total <= current) return;
        insect.ApplyEffect(new PuncturedEffect(insect, 8f, total, this));
    }

    private void ApplyTauntInRange()
    {
        foreach (Insect insect in Insect.allInsects)
        {
            if (!insect.IsAlive) continue;
            if (Vector3.Distance(transform.position, insect.transform.position) <= attackRange)
                insect.ApplyEffect(new TauntEffect(insect, TauntEffectDuration, 1, this, this));
        }
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        skillCooldownTimer = skillCooldown;
        ApplyEffect(new CactusShieldEffect(this, SkillDuration, 1, this, ShieldAmount));
    }

    public override void UpdateStats()
    {
        base.UpdateStats();
        temperatureMax = comfortMax;
    }

    public override void OnPath1Upgrade(int level)
    {
        piercingAdder = level >= 5 ? 1 : 0;
    }

    public override void OnPath2Upgrade(int level)
    {
        float hppl = CactData?.path2HealthPerLevel ?? 60f;
        baseMaxHealth = (data?.baseMaxHealth ?? 250f) + hppl * level;
        health += hppl;
        UpdateHealthBar();
    }

    public override string GetName() => "<b><color=green>Cactus</color></b>";

    public override string GetDescription() =>
        "A hardy desert plant that fires needles in all directions and retaliates against attackers.";

    public override string GetAttackDescription()
    {
        int needleBase  = CactData?.path1NeedlesBase     ?? 8;
        int needleLevel = CactData?.path1NeedlesPerLevel ?? 3;
        return $"Fires <color=green><b>{needleBase + needleLevel * effectivePath1Level}</b></color> needles in equal angles around itself, dealing " +
               $"<color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage per needle. " +
               $"Each hit applies <color=#A0522D><b>Punctured</b></color>.\n" +
               $"At Level <color=green><b>5</b></color>, <color=green><b>+1</b></color> Piercing.";
    }

    public override string GetPassiveDescription() =>
        $"Insects that attack the {GetName()} take damage equal to <color=green><b>150%</b></color> of their own Attack Damage as " +
        $"{PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage, and receive " +
        $"<color=green><b>{1 + effectivePath2Level}</b></color> <color=#A0522D>Punctured</color> stack(s).";

    public override string GetSkillDesription() =>
        $"Grants the {GetName()} a <color=grey><b>{ShieldAmount:F0}</b></color> shield for <color=green><b>{SkillDuration:F0}s</b></color>. " +
        $"While the shield holds, nearby insects are forced to target the {GetName()}.";

    public override string GetPath1Description(bool details = false)
    {
        int needleLevel = CactData?.path1NeedlesPerLevel ?? 3;
        string scaling = details
            ? $"Increase needle count by <color=green><b>{needleLevel}</b></color> per level. [<color=green><b>+{needleLevel * effectivePath1Level}</b></color>]"
            : $"Increase needle count by <color=green><b>{needleLevel}</b></color>.";
        return $"Attack:\n\n{GetAttackDescription()}\n\n" +
               $"{scaling}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float hppl = CactData?.path2HealthPerLevel ?? 60f;
        string scaling = details
            ? $"Increase <color=#A0522D>Punctured</color> stacks per hit by <color=green><b>1</b></color> per level. [<color=green><b>+{effectivePath2Level}</b></color>]\n\n" +
              $"Increase Max Health by <color=green><b>{hppl:F0}</b></color> per level. [<color=green><b>+{hppl * effectivePath2Level:F0}</b></color>]"
            : $"Increase <color=#A0522D>Punctured</color> stacks per hit by <color=green><b>1</b></color>.\n\n" +
              $"Increase Max Health by <color=green><b>{hppl:F0}</b></color>.";
        return $"Passive:\n\n{GetPassiveDescription()}\n\n" +
               $"<color=#A0522D>Punctured</color>: reduces Physical Resistance by <color=green><b>0.5%</b></color> per stack, lasts 8 seconds.\n\n" +
               $"{scaling}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float shieldpl  = CactData?.path3ShieldPerLevel        ?? 10f;
        float durpl     = CactData?.path3SkillDurationPerLevel ?? 2f;
        float healpl    = CactData?.path3HealBonusPerLevel     ?? 0.04f;
        string scaling = details
            ? $"Increase shield by <color=green><b>{shieldpl:F0}</b></color> per level. [<color=grey><b>+{shieldpl * effectivePath3Level:F0}</b></color>]\n\n" +
              $"Increase duration by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}s</b></color>]\n\n" +
              $"Increase healing received by <color=green><b>{healpl * 100f:F0}%</b></color> per level. [<color=green><b>+{healpl * effectivePath3Level * 100f:F0}%</b></color>]"
            : $"Increase shield by <color=green><b>{shieldpl:F0}</b></color>.\n\n" +
              $"Increase duration by <color=green><b>{durpl:F0}</b></color> seconds.\n\n" +
              $"Increase healing received by <color=green><b>{healpl * 100f:F0}%</b></color>.";
        return $"Skill:\n\n{GetSkillDesription()}\n\n" +
               $"{scaling}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
