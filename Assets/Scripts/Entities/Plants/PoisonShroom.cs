using UnityEngine;
using System.Collections.Generic;

public class PoisonShroom : Shooter
{
    private PoisonShroomData PSData => data as PoisonShroomData;
    public float PoisonBaseDPS => PSData?.basePoisonDPS ?? 0f;

    public int poisonLevel = 1;
    public float poisonDuration;
    public float activeRadius;
    [SerializeField] private GameObject poisonBlobPrefab;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        activeRadius = data.baseSkillRadius;
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void Shoot(Vector3 target)
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        PoisonShroomProjectile puff = projectile.GetComponent<PoisonShroomProjectile>();
        if (puff != null)
        {
            puff.SetTarget(FindTarget());
            puff.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
        }
    }

    protected override GameObject FindTarget()
    {
        List<Insect> unpoisoned = new List<Insect>();
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive) continue;
            float distance = Vector3.Distance(transform.position, insect.transform.position);
            if (distance <= attackRange && !insect.HasEffect<PoisonEffect>() && IsValidNightTarget(insect, distance))
                unpoisoned.Add(insect);
        }
        if (unpoisoned.Count > 0)
            return FindFirst(unpoisoned);
        return base.FindTarget();
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackSpeed = data.baseAttackSpeed + level * (PSData?.path1AttackSpeedPerLevel ?? 0.08f);
        baseAttackRange = data.baseAttackRange + level * (PSData?.path1AttackRangePerLevel ?? 0.1f);
    }

    public override void OnPath2Upgrade(int level) { }

    public override void UpdateStats()
    {
        base.UpdateStats();
        float durpl = PSData?.path2PoisonDurationPerLevel ?? 1f;
        poisonLevel    = 1 + path2Level;
        poisonDuration = ((PSData?.basePoisonDuration ?? 0f) + durpl * path2Level) * (1 + passiveDuration);
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + (PSData?.path3SkillDurationPerLevel ?? 1f) * level;
        activeRadius      = data.baseSkillRadius   + (PSData?.path3RadiusPerLevel        ?? 0.2f) * level;
    }

    public override void ActivateSkill()
    {
        SkillTargetingManager.instance.BeginTargeting(activeRadius, OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        if (poisonBlobPrefab == null) return;
        skillCooldownTimer = skillCooldown;
        float fieldDPS = PoisonBaseDPS + skillDamageMultiplier * magicPower;
        GameObject obj = Instantiate(poisonBlobPrefab, transform.position, Quaternion.identity);
        PoisonBlob blob = obj.GetComponent<PoisonBlob>();
        if (blob != null)
            blob.Initialize(position, activeRadius, skillDuration, this, fieldDPS);
    }

    public override string GetName() => $"<b><color=purple>{(data != null ? data.displayName : "Poison Shroom")}</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} shoots toxic spores dealing damage and applying <color=purple>Poison</color>.";

    public override string GetPath1Description(bool details = false)
    {
        float aspl    = PSData?.path1AttackSpeedPerLevel ?? 0.08f;
        float rangepl = PSData?.path1AttackRangePerLevel ?? 0.1f;
        string scaling = details
            ? $"Increase Attack Speed by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F2}</b></color>]\n\n" +
              $"Increase Attack Range by <color=green><b>{rangepl:F1}</b></color> per level. [<color=green><b>+{rangepl * effectivePath1Level:F1}</b></color>]"
            : $"Increase Attack Speed by <color=green><b>{aspl:F2}</b></color>.\n\n" +
              $"Increase Attack Range by <color=green><b>{rangepl:F1}</b></color>.";
        return $"Attack:\n\n" +
               $"Blows poisonous bubbles at his target, dealing <color=green><b>{attackDamage}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage.\n\n" +
               $"{scaling}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float dpspl = PSData?.path2PoisonDPSPerLevel        ?? 6f;
        float durpl = PSData?.path2PoisonDurationPerLevel   ?? 1f;
        string scaling = details
            ? $"Scaling: <color=#FFB6C1><b>{skillDamageMultiplier * 100f:F0}%</b></color> Magic Power\n\n" +
              $"Increase <color=purple>Poison</color> damage by <color=green><b>{dpspl:F0}</b></color> per level. [<color=green><b>+{dpspl * effectivePath2Level:F0}</b></color>]\n\n" +
              $"Increase <color=purple>Poison</color> duration by <color=green><b>{durpl:F0}</b></color> second per level. [<color=green><b>+{durpl * effectivePath2Level:F0}s</b></color>]"
            : $"Increase <color=purple>Poison</color> damage by <color=green><b>{dpspl:F0}</b></color>.\n\n" +
              $"Increase <color=purple>Poison</color> duration by <color=green><b>{durpl:F0}</b></color> second.";
        return $"Passive:\n\n" +
               $"Attacks apply a <color=purple>Poison</color> effect on hit for <color=green><b>{poisonDuration}</b></color> seconds, dealing <color=green><b>{PoisonBaseDPS + dpspl * effectivePath2Level:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] {PlantData.ElementalTag(elementalType)} damage per second.\n\n" +
               $"{scaling}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float durpl    = PSData?.path3SkillDurationPerLevel ?? 1f;
        float radiuspl = PSData?.path3RadiusPerLevel        ?? 0.2f;
        string scaling = details
            ? $"Scaling: <color=#FFB6C1><b>{skillDamageMultiplier * 100f:F0}%</b></color> Magic Power\n\n" +
              $"Increase field duration by <color=green><b>{durpl:F0}</b></color> second per level. [<color=green><b>+{durpl * effectivePath3Level:F0}s</b></color>]\n\n" +
              $"Increase field radius by <color=green><b>{radiuspl:F1}</b></color> per level. [<color=green><b>+{radiuspl * effectivePath3Level:F1}</b></color>]"
            : $"Increase field duration by <color=green><b>{durpl:F0}</b></color> second.\n\n" +
              $"Increase field radius by <color=green><b>{radiuspl:F1}</b></color>.";
        return $"Skill:\n\n" +
               $"Hurls a toxic blob towards a targeted area, creating a poison field with a <color=green><b>{activeRadius}</b></color> radius that lasts <color=green><b>{skillDuration}</b></color> seconds. Insects standing in the field take <color=green><b>{PoisonBaseDPS}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage per second, and any debuffs on them are frozen in time.\n\n" +
               $"{scaling}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
