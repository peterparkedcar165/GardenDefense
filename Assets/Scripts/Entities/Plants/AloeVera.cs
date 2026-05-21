using UnityEngine;
using System.Collections;

public class AloeVera : Lobber
{
    [Header("Aloe Vera")]
    public float baseHealAmount = 24f;
    public float baseTempReduction = 4.5f;
    public float healAmount;
    public float tempReduction;
    public float baseSkillHealPerTick = 10f;
    public float baseSkillHealInterval = 1f;
    public float baseSkillTempReduction = 2f;
    public float skillHealPerTick;
    public float channelDuration = 1.5f;
    [SerializeField] private GameObject soothingRainPrefab;

    private AloeVeraData AVData => data as AloeVeraData;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        if (AVData != null)
        {
            baseHealAmount        = AVData.baseHealAmount;
            baseTempReduction     = AVData.baseTempReduction;
            baseSkillHealPerTick   = AVData.baseSkillHealPerTick;
            baseSkillHealInterval  = AVData.baseSkillHealInterval;
            baseSkillTempReduction = AVData.baseSkillTempReduction;
        }
    }

    public override void UpdateStats()
    {
        base.UpdateStats();
        healAmount       = baseHealAmount + 4f * effectivePath2Level + magicPower * 0.12f;
        tempReduction    = baseTempReduction + 0.5f * effectivePath2Level;
        skillHealPerTick = baseSkillHealPerTick + 1f * effectivePath3Level + magicPower * 0.03f;
    }

    protected override GameObject FindLobberTarget()
    {
        Plant bestPlant = null;
        float lowestHealth = Mathf.Infinity;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == this || plant == null || !plant.IsAlive) continue;
            if (plant.health >= plant.maxHealth) continue;
            if (Vector3.Distance(transform.position, plant.transform.position) > attackRange) continue;
            if (plant.health < lowestHealth) { lowestHealth = plant.health; bestPlant = plant; }
        }
        if (bestPlant != null) return bestPlant.gameObject;
        return base.FindLobberTarget();
    }

    protected override void Fire(GameObject target, Vector3 landingPos)
    {
        if (projectilePrefab == null) return;
        bool isHealMode = target.GetComponent<Plant>() != null;
        GameObject obj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        AloeVeraProjectile proj = obj.GetComponent<AloeVeraProjectile>();
        if (proj == null) return;
        proj.minFlightDuration = minFlightDuration;
        proj.arcPeakHeight     = projectileHeight;
        proj.Initialize(landingPos, attackDamage, projectileSpeed, aoERadius,
                        healAmount, tempReduction, isHealMode, damageType, elementalType, this);
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackSpeed = data.baseAttackSpeed + level * 0.03f;
        baseAttackRange = data.baseAttackRange + level * 0.2f;
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + 1f * level;
        baseSkillRadius   = data.baseSkillRadius   + 0.3f * level;
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        SkillTargetingManager.instance.BeginTargeting(baseSkillRadius, OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        skillCooldownTimer = skillCooldown;
        StartCoroutine(ChannelAndSpawn(position));
    }

    private IEnumerator ChannelAndSpawn(Vector3 position)
    {
        yield return new WaitForSeconds(channelDuration);
        if (soothingRainPrefab == null) yield break;
        GameObject obj = Instantiate(soothingRainPrefab, position, Quaternion.identity);
        obj.GetComponent<SoothingRain>()?.Initialize(baseSkillRadius, skillDuration, skillHealPerTick, baseSkillHealInterval, baseSkillTempReduction, this);
    }

    public override string GetName() => "<b><color=#4FC3F7>Aloe Vera</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} lobs arcing water droplets that burst on impact. She prioritizes healing injured allies before engaging insects.";

    public override string GetAttackDescription() =>
        $"Lob a water droplet that bursts on landing, dealing <color=green><b>{attackDamage:F0}</b></color> <color=#4FC3F7>Water</color> <color=#FFB6C1>Magic</color> damage to all insects within <color=green><b>{aoERadius:F1}</b></color> radius.";

    public override string GetPassiveDescription() =>
        $"Water droplets also heal plants, restoring <color=green><b>{healAmount:F0}</b></color> [<color=#FFB6C1><b>+{magicPower * 0.12f:F0}</b></color>] Health and reducing temperature by <color=#4FC3F7><b>{tempReduction:F1}</b></color>, until comfort, for all plants within <color=green><b>{aoERadius:F1}</b></color> radius. If an injured plant is within range, switch targetting to the one with the lowest Health.";

    public override string GetPath1Description() =>
        $"Attack:\n\n{GetAttackDescription()}\n\n" +
        $"Increase Attack Speed by <color=green><b>0.03</b></color> per level. [<color=green><b>+{0.03f * effectivePath1Level:F2}</b></color>]\n\n" +
        $"Increase Attack Range by <color=green><b>0.2</b></color> per level. [<color=green><b>+{0.2f * effectivePath1Level:F1}</b></color>]\n\n" +
        $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>";

    public override string GetPath2Description() =>
        $"Passive:\n\n{GetPassiveDescription()}\n\n" +
        $"Increase base healing by <color=green><b>4</b></color> per level. [<color=green><b>+{4 * effectivePath2Level}</b></color>]\n\n" +
        $"Increase temperature reduction by <color=green><b>0.5</b></color> per level. [<color=green><b>+{0.5f * effectivePath2Level:F1}</b></color>]\n\n" +
        $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>";

    public override string GetSkillDesription()
    {
        float totalHealing = skillHealPerTick * Mathf.Floor(skillDuration / baseSkillHealInterval);
        return $"Channels for <color=green><b>{channelDuration:F1}s</b></color>, then calls down a Soothing Rain on a targeted area, healing all plants within <color=green><b>{baseSkillRadius:F1}</b></color> radius for <color=green><b>{skillHealPerTick:F0}</b></color> [<color=#FFB6C1><b>+{magicPower * 0.03f:F0}</b></color>] Health and reducing temperature by <color=#4FC3F7><b>{baseSkillTempReduction:F1}</b></color>, until comfort, every <color=green><b>{baseSkillHealInterval:F1}s</b></color> over <color=green><b>{skillDuration:F0}</b></color> seconds. " +
               $"For a total of <color=green><b>{totalHealing:F0}</b></color> Health.";
    }

    public override string GetPath3Description()
    {
        float totalHealing = skillHealPerTick * Mathf.Floor(skillDuration / baseSkillHealInterval);
        return $"Skill:\n\n{GetSkillDesription()}\n\n" +
               $"Scaling: <color=#FFB6C1><b>3%</b></color> <color=#FFB6C1>Magic Power</color>\n\n" +
               $"Increase healing by <color=green><b>1</b></color> per tick per level. [<color=green><b>+{1 * effectivePath3Level}</b></color>]\n\n" +
               $"Increase rain duration by <color=green><b>1</b></color> second per level. [<color=green><b>+{1 * effectivePath3Level}s</b></color>]\n\n" +
               $"Increase rain radius by <color=green><b>0.3</b></color> per level. [<color=green><b>+{0.3f * effectivePath3Level:F1}</b></color>]\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>";
    }
}
