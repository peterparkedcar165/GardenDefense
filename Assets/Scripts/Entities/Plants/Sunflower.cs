using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class Sunflower : Shooter
{
    public float channelDuration = 2f;
    public int sunGenerated;
    public float skillAoERadius, sunrayDamagePerSecond;
    [SerializeField] private GameObject sunrayPrefab;

    private SunflowerData SFData => data as SunflowerData;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        skillAoERadius       = data.baseSkillRadius;
        passiveCooldownTimer = data.basePassiveCooldown;
    }

    protected override void Update()
    {
        base.Update();

        sunGenerated = (SFData?.baseSunGenerated ?? 6) + 2 * effectivePath2Level;

        if (passiveCooldownTimer <= 0)
        {
            GameManager.instance.AddSun(sunGenerated);
            passiveCooldownTimer += passiveCooldown;
            GameObject indicator = Object.Instantiate(Resources.Load<GameObject>("DamageIndicator"), transform.position + new Vector3(0.25f, 0.5f, 0f), Quaternion.identity);
            indicator.GetComponent<DamageIndicator>().Initialize($"+{sunGenerated} Sun", new Color(1f, 1f, 0f));
        }
    }

    public override void UpdateStats()
    {
        base.UpdateStats();
        sunrayDamagePerSecond = (SFData?.baseSunrayDPS ?? 0f) + 15f * effectivePath3Level + skillDamageMultiplier * magicPower;
        channelDuration = (WeatherManager.instance != null && WeatherManager.instance.weather == WeatherType.Sunny) ? 1f : 2f;
    }

    protected override void Shoot(Vector3 target)
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        SunflowerProjectile petal = projectile.GetComponent<SunflowerProjectile>();
        if (petal != null)
        {
            petal.SetTarget(FindTarget());
            petal.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
        }
    }

    public void ReduceSunTimer()
    {
        passiveCooldownTimer -= 1f;
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + level * 5f;
        baseAttackSpeed  = data.baseAttackSpeed  + level * 0.05f;
    }

    public override void OnPath2Upgrade(int level)
    {
        passiveCooldownAdder = -(float)level;
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + 0.5f * level;
    }

    public override void ActivateSkill()
    {
        SkillTargetingManager.instance.BeginTargeting(skillAoERadius, OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        if (sunrayPrefab == null) return;
        skillCooldownTimer = skillCooldown;
        StartCoroutine(ChannelAndSpawn(position));
    }

    private IEnumerator ChannelAndSpawn(Vector3 position)
    {
        yield return new WaitForSeconds(channelDuration);
        GameObject obj = Instantiate(sunrayPrefab, position, Quaternion.identity);
        Sunray sunray = obj.GetComponent<Sunray>();
        if (sunray != null)
            sunray.Initialize(sunrayDamagePerSecond, skillAoERadius, skillDuration, this);
    }

    public override string GetName() => "<b><color=orange>Sunflower</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} shoots her targets with sun bolts and generate precious <color=yellow>Sun</color> for the garden.";

    public override string GetAttackDescription() =>
        $"Briefly charges up a solar-powered energy orb then shoots it towards her target, dealing <color=green><b>{attackDamage}</b></color> <color=orange>Fire</color> <color=#FFB6C1>Magic </color>damage.";

    public override string GetSkillDesription() =>
        $"Gathers a large burst of energy from the sun, calling down a scorching beam from above that deals <color=green><b>{(SFData?.baseSunrayDPS ?? 0f) + 15f * effectivePath3Level:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] <color=orange>Fire</color> <color=#FFB6C1>Magic</color> damage per second to insects within the designated area for <color=green><b>{skillDuration}</b></color> seconds.";

    public override string GetPassiveDescription() =>
        $"Passively generates <color=green><b>{sunGenerated}</b></color> <color=yellow>Sun</color> for the garden every <color=green><b>{passiveCooldown}</b></color> seconds. Attacks reduce the cooldown by <color=green><b>1</b></color> second.";

    public override string GetPath1Description() =>
        $"Attack:\n\n{GetAttackDescription()}\n\n" +
        $"Increase Attack Damage by <color=green><b>5</b></color> per level. [<color=green><b>+{5 * effectivePath1Level}</b></color>]\n\n" +
        $"Increase Attack Speed by <color=green><b>0.05</b></color> per level. [<color=green><b>+{0.05 * effectivePath1Level}</b></color>]\n\n" +
        $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>";

    public override string GetPath2Description() =>
        $"Passive:\n\n{GetPassiveDescription()}\n\n" +
        $"Increase Sun Generated by <color=green><b>2</b></color> per level. [<color=green><b>+{2 * effectivePath2Level}</b></color>]\n\n" +
        $"Reduce Sun Generation Cooldown by <color=green><b>1</b></color> second per level. [<color=green><b>-{effectivePath2Level}</b></color>]\n\n" +
        $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>";

    public override string GetPath3Description() =>
        $"Skill:\n\n{GetSkillDesription()}\n\n" +
        $"Scaling: <color=#FFB6C1><b>{skillDamageMultiplier * 100f:F0}%</b></color> <color=#FFB6C1>Magic Power</color>\n\n" +
        $"Increase Sunray Damage by <color=green><b>15</b></color> per level. [<color=green><b>+{15 * effectivePath3Level}</b></color>]\n\n" +
        $"Increase Sunray duration by <color=green><b>0.5</b></color> second per level. [<color=green><b>+{0.5 * effectivePath3Level}s</b></color>]\n\n" +
        $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>";
}
