using UnityEngine;

public class Cactus : Shooter
{
    private float _tauntTickTimer;
    private const float TauntTickInterval  = 0.25f;
    private const float TauntEffectDuration = 0.35f;

    private float ShieldAmount   => 100f + 20f * effectivePath3Level;
    private float SkillDuration  => 12f  +  2f * effectivePath3Level;
    private float SkillHealBonus => 0.16f + 0.04f * effectivePath3Level;

    private int PassivePunctureStacks => 1 + effectivePath2Level;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    protected override void Update()
    {
        base.Update();

        if (HasEffect<CactusTauntingEffect>())
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
        int needleCount = 8 + 3 * effectivePath1Level;
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
        attacker.Damage(attacker.attackDamage * 1.5f, DamageType.Physical, ElementalType.Nature, this, false, new DamageTag[] { DamageTag.PassiveDamage, DamageTag.Counter });
        ApplyPunctured(attacker, PassivePunctureStacks);
    }

    public void OnNeedleHit(Insect insect)
    {
        ApplyPunctured(insect, 1);
    }

    private void ApplyPunctured(Insect insect, int stacks)
    {
        if (!insect.IsAlive) return;
        int current = insect.GetEffect<PuncturedEffect>()?.level ?? 0;
        insect.ApplyEffect(new PuncturedEffect(insect, 8f, current + stacks, this));
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
        ApplyEffect(new CactusTauntingEffect(this, SkillDuration, 1, this, SkillHealBonus));
    }

    public override void OnPath1Upgrade(int level)
    {
        piercingAdder = level >= 5 ? 1 : 0;
    }

    public override void OnPath2Upgrade(int level)
    {
        baseMaxHealth = (data?.baseMaxHealth ?? 250f) + 60f * level;
        health += 60f;
        UpdateHealthBar();
    }

    public override string GetName() => "<b><color=green>Cactus</color></b>";

    public override string GetDescription() =>
        "A hardy desert plant that fires needles in all directions and retaliates against attackers.";

    public override string GetAttackDescription() =>
        $"Fires <color=green><b>{8 + 3 * effectivePath1Level}</b></color> needles in equal angles around itself, dealing " +
        $"<color=green><b>{attackDamage:F0}</b></color> <color=green>Nature</color> <color=#A0522D>Physical</color> damage per needle. " +
        $"Each hit applies <color=#A0522D><b>Punctured</b></color>.\n" +
        $"At Effective Level 5, <color=green><b>+1</b></color> Piercing.";

    public override string GetPassiveDescription() =>
        $"Insects that attack the {GetName()} take damage equal to <color=green><b>150%</b></color> of their own Attack Damage as " +
        $"<color=green>Nature</color> <color=#A0522D>Physical</color> damage, and receive " +
        $"<color=green><b>{PassivePunctureStacks}</b></color> <color=#A0522D>Punctured</color> stack(s).";

    public override string GetSkillDesription() =>
        $"Gains a <color=grey><b>{ShieldAmount:F0}</b></color> shield and taunts insects within range for " +
        $"<color=green><b>{SkillDuration:F0}s</b></color>. Healing received increased by " +
        $"<color=green><b>{SkillHealBonus * 100f:F0}%</b></color> during the effect.";

    public override string GetPath1Description() =>
        $"Attack:\n\n{GetAttackDescription()}\n\n" +
        $"Increase needle count by <color=green><b>3</b></color> per level. [<color=green><b>{8 + 3 * effectivePath1Level}</b></color> needles]\n\n" +
        $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>";

    public override string GetPath2Description() =>
        $"Passive:\n\n{GetPassiveDescription()}\n\n" +
        $"<color=#A0522D>Punctured</color>: reduces Physical Resistance by <color=green><b>2%</b></color> per stack, lasts 8 seconds.\n\n" +
        $"Increase <color=#A0522D>Punctured</color> stacks per passive hit by <color=green><b>1</b></color> per level. [<color=green><b>+{effectivePath2Level}</b></color>]\n\n" +
        $"Increase Max Health by <color=green><b>60</b></color> per level. [<color=green><b>+{60 * effectivePath2Level}</b></color>]\n\n" +
        $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>";

    public override string GetPath3Description() =>
        $"Skill:\n\n{GetSkillDesription()}\n\n" +
        $"Increase shield by <color=green><b>20</b></color> per level. [<color=grey><b>+{20 * effectivePath3Level}</b></color>]\n\n" +
        $"Increase duration by <color=green><b>2</b></color> seconds per level. [<color=green><b>+{2 * effectivePath3Level}s</b></color>]\n\n" +
        $"Increase healing received by <color=green><b>4%</b></color> per level. [<color=green><b>+{4 * effectivePath3Level}%</b></color>]\n\n" +
        $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>";
}
