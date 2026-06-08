using UnityEngine;
using System.Collections.Generic;

public class Hellebore : Shooter
{
    private HelleboreData HData => data as HelleboreData;

    private float _auraTick;
    private const float AuraTickInterval = 0.25f;

    private float AuraResist => (HData?.passivePhysResist ?? 0.1f)  + effectivePath2Level * (HData?.path2PhysResistPerLevel ?? 0.02f);
    private float CDRPerHit  => (HData?.passiveCDRPerHit  ?? 0.5f)  + effectivePath2Level * (HData?.path2CDRPerLevel       ?? 0.1f);

    private float SkillShieldBase => (HData?.shieldAmount ?? 120f) + effectivePath3Level * (HData?.path3ShieldPerLevel ?? 30f);
    private float SkillShieldMP   => (HData?.shieldMP ?? 0.5f) * magicPower;
    private float SkillShield     => SkillShieldBase + SkillShieldMP;
    private float SkillDur    => (HData?.shieldDuration    ?? 12f)  + effectivePath3Level * (HData?.path3DurationPerLevel ?? 2f);
    private float ReflectBase => (HData?.reflectPoisonBase ?? 15f)  + effectivePath3Level * (HData?.path3ReflectPerLevel  ?? 5f);
    private float ReflectMP   => HData?.reflectPoisonMP ?? 0.2f;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    protected override void Update()
    {
        base.Update();
        TickAura();
    }

    private void TickAura()
    {
        _auraTick += Time.deltaTime;
        if (_auraTick < AuraTickInterval) return;
        _auraTick -= AuraTickInterval;

        float resist = AuraResist;
        float auraExpire = AuraTickInterval * 1.6f; // slightly longer than tick interval so it never gaps
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive) continue;
            if (Vector3.Distance(transform.position, plant.transform.position) > attackRange) continue;
            plant.ApplyEffect(new HelleboreAuraEffect(plant, auraExpire, 1, this, resist));
        }
    }

    protected override void Shoot(Vector3 target)
    {
        if (projectilePrefab == null) return;
        GameObject obj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        HelleboreProjectile proj = obj.GetComponent<HelleboreProjectile>();
        if (proj == null) return;
        proj.SetTarget(FindTarget());
        proj.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
    }

    public void OnProjectileHit()
    {
        skillCooldownTimer = Mathf.Max(0f, skillCooldownTimer - CDRPerHit);
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        SkillTargetingManager.instance.BeginPlantTargeting(OnTargetConfirmed, this);
    }

    private void OnTargetConfirmed(Plant targetPlant)
    {
        if (targetPlant == null) return;
        skillCooldownTimer = skillCooldown;
        targetPlant.ApplyEffect(new HelleboreProtectionEffect(
            targetPlant, SkillDur, effectivePath3Level + 1, this, SkillShield, ReflectBase, ReflectMP));
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackSpeed = data.baseAttackSpeed + (HData?.path1AttackSpeedPerLevel ?? 0.05f) * level;
        baseMagicPower  = data.baseMagicPower  + (HData?.path1MagicPowerPerLevel  ?? 5f)    * level;
    }

    public override string GetName() => "<b><color=#9B30D0>Hellebore</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} weaves poison and shelter together, protecting allies while punishing those who dare attack them.";

    public override string GetAttackDescription() =>
        $"Fires a thorned projectile dealing <color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage.";

    public override string GetPassiveDescription() =>
        $"Each attack hit reduces skill cooldown by <color=green><b>{CDRPerHit:F1}s</b></color>. " +
        $"Plants within attack range gain <color=#A0522D><b>Thorned Guard</b></color>, increasing their " +
        $"<color=#A0522D>Physical Resistance</color> by <color=green><b>{AuraResist * 100f:F0}%</b></color>.";

    public override string GetSkillDesription() =>
        $"Targets a plant anywhere on the field, granting <color=#9B30D0><b>Hellebore's Protection</b></color>: " +
        $"a shield of <color=green><b>{SkillShieldBase:F0}</b></color> [<color=#FFB6C1><b>+{SkillShieldMP:F0}</b></color>] health for <color=green><b>{SkillDur:F0}s</b></color>. " +
        $"While shielded, attackers receive <color=purple><b>{ReflectBase:F0}</b></color> " +
        $"[<color=#FFB6C1><b>+{magicPower * ReflectMP:F0}</b></color>] " +
        $"<color=purple>Poison</color> <color=#FFB6C1>Magic</color> damage per hit. " +
        $"Negative effects are reflected back to the attacker. The protection fades when the shield breaks.";

    public override string GetPath1Name() => "Thorns";
    public override string GetPath2Name() => "Shelter";
    public override string GetPath3Name() => "Protection";

    public override string GetPath1Description()
    {
        float aspl = HData?.path1AttackSpeedPerLevel ?? 0.05f;
        float mppl = HData?.path1MagicPowerPerLevel  ?? 5f;
        return $"Attack:\n\n{GetAttackDescription()}\n\n" +
               $"Increase Attack Speed by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase Magic Power by <color=green><b>{mppl:F0}</b></color> per level. [<color=#FFB6C1><b>+{mppl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>";
    }

    public override string GetPath2Description()
    {
        float cdrpl    = HData?.path2CDRPerLevel        ?? 0.1f;
        float resistpl = HData?.path2PhysResistPerLevel ?? 0.02f;
        return $"Passive:\n\n{GetPassiveDescription()}\n\n" +
               $"Increase Cooldown Reduction per hit by <color=green><b>{cdrpl:F1}s</b></color> per level. [<color=green><b>+{cdrpl * effectivePath2Level:F1}s</b></color>]\n\n" +
               $"Increase Physical Resistance aura by <color=green><b>{resistpl * 100f:F0}%</b></color> per level. [<color=green><b>+{resistpl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>";
    }

    public override string GetPath3Description()
    {
        float shieldpl = HData?.path3ShieldPerLevel   ?? 30f;
        float durpl    = HData?.path3DurationPerLevel ?? 2f;
        float reflpl   = HData?.path3ReflectPerLevel  ?? 5f;
        return $"Skill:\n\n{GetSkillDesription()}\n\n" +
               $"Scaling: <color=#FFB6C1><b>{(HData?.shieldMP ?? 0.5f) * 100f:F0}%</b></color> <color=#FFB6C1>Magic Power</color> (Shield)\n\n" +
               $"Scaling: <color=#FFB6C1><b>{(HData?.reflectPoisonMP ?? 0.2f) * 100f:F0}%</b></color> <color=#FFB6C1>Magic Power</color> (Reflect Damage)\n\n" +
               $"Increase shield by <color=green><b>{shieldpl:F0}</b></color> per level. [<color=green><b>+{shieldpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase protection duration by <color=green><b>{durpl:F0}s</b></color> per level. [<color=green><b>+{durpl * effectivePath3Level:F0}s</b></color>]\n\n" +
               $"Increase reflect damage by <color=green><b>{reflpl:F0}</b></color> per level. [<color=green><b>+{reflpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>";
    }
}
