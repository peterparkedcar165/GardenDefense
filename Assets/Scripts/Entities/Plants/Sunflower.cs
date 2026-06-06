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

        int sunpl = SFData?.path2SunPerLevel ?? 2;
        sunGenerated = (SFData?.baseSunGenerated ?? 6) + sunpl * effectivePath2Level;

        if (passiveCooldownTimer <= 0)
        {
            int granted = GenerateSun(sunGenerated);
            passiveCooldownTimer += passiveCooldown;
            SunIndicator.Spawn(transform.position + new Vector3(0.25f, 0.5f, 0f), granted);
        }
    }

    public override void UpdateStats()
    {
        base.UpdateStats();
        float dpspl = SFData?.path3SunrayDPSPerLevel ?? 15f;
        sunrayDamagePerSecond = (SFData?.baseSunrayDPS ?? 0f) + dpspl * effectivePath3Level + skillDamageMultiplier * magicPower;
        channelDuration = HasEffect<SunlightExposedEffect>() ? 1f : 2f;
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
        baseAttackDamage = data.baseAttackDamage + level * (SFData?.path1AttackDamagePerLevel ?? 5f);
        baseAttackSpeed  = data.baseAttackSpeed  + level * (SFData?.path1AttackSpeedPerLevel  ?? 0.05f);
    }

    public override void OnPath2Upgrade(int level)
    {
        passiveCooldownAdder = -(float)level;
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + (SFData?.path3SkillDurationPerLevel ?? 0.5f) * level;
    }

    public override void ActivateSkill()
    {
        SkillTargetingManager.instance.BeginTargeting(skillAoERadius, OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        if (sunrayPrefab == null) return;
        skillCooldownTimer = skillCooldown;
        BeginChannel();   // can't attack until the sunray appears (set channelDuration in data)
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
        $"Briefly charges up a solar-powered energy orb then shoots it towards her target, dealing <color=green><b>{attackDamage}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage.";

    public override string GetSkillDesription()
    {
        float dpspl = SFData?.path3SunrayDPSPerLevel ?? 15f;
        return $"Gathers a large burst of energy from the sun, calling down a scorching beam from above that deals <color=green><b>{(SFData?.baseSunrayDPS ?? 0f) + dpspl * effectivePath3Level:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage per second to insects within the designated area for <color=green><b>{skillDuration}</b></color> seconds.";
    }

    public override string GetPassiveDescription() =>
        $"Passively generates <color=green><b>{sunGenerated}</b></color> <color=yellow>Sun</color> for the garden every <color=green><b>{passiveCooldown}</b></color> seconds. Attacks reduce the cooldown by <color=green><b>1</b></color> second.";

    public override string GetPath1Description()
    {
        float adpl = SFData?.path1AttackDamagePerLevel ?? 5f;
        float aspl = SFData?.path1AttackSpeedPerLevel  ?? 0.05f;
        return $"Attack:\n\n{GetAttackDescription()}\n\n" +
               $"Increase Attack Damage by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase Attack Speed by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>";
    }

    public override string GetPath2Description()
    {
        int sunpl = SFData?.path2SunPerLevel ?? 2;
        return $"Passive:\n\n{GetPassiveDescription()}\n\n" +
               $"Increase Sun Generated by <color=green><b>{sunpl}</b></color> per level. [<color=green><b>+{sunpl * effectivePath2Level}</b></color>]\n\n" +
               $"Reduce Sun Generation Cooldown by <color=green><b>1</b></color> second per level. [<color=green><b>-{effectivePath2Level}</b></color>]\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>";
    }

    public override string GetPath3Description()
    {
        float dpspl = SFData?.path3SunrayDPSPerLevel    ?? 15f;
        float durpl = SFData?.path3SkillDurationPerLevel ?? 0.5f;
        return $"Skill:\n\n{GetSkillDesription()}\n\n" +
               $"Scaling: <color=#FFB6C1><b>{skillDamageMultiplier * 100f:F0}%</b></color> <color=#FFB6C1>Magic Power</color>\n\n" +
               $"Increase Sunray Damage by <color=green><b>{dpspl:F0}</b></color> per level. [<color=green><b>+{dpspl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase Sunray duration by <color=green><b>{durpl:F1}</b></color> second per level. [<color=green><b>+{durpl * effectivePath3Level:F1}s</b></color>]\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>";
    }
}
