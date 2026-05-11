using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class Snowdrop : Aura
{
    private float 
    bAD = 9f, // base attack damage
    bAS = 0.33f, // base attack speed
    bAR = 1.75f; // base attack range
    private float tickTimer = 0f;
    private const float tickInterval = 0.25f;
    public int chillLevel = 1;
    [SerializeField] private GameObject blizzardPrefab;
    [SerializeField] private GameObject blizzardIndicatorPrefab;
    private float blizzardWidth;
    public float blizzardDamage;
    private GameObject blizzardIndicatorInstance;
    private const float indicatorLength = 30f;
    protected override void Awake()
    {
        base.Awake();
        baseAttackDamage = bAD;
        baseAttackSpeed = bAS;
        baseAttackRange = bAR;
        activeCooldown = 40f;
        activeDuration = 6f;
        blizzardWidth = 1.5f;
        blizzardDamage = 15f;
    }


    protected override void Update()
    {
        base.Update();
        
        if (attackCooldownTimer < attackCooldown)
        {
            attackCooldownTimer += Time.deltaTime;
        }
        else
        {
           // if there is at least one insect within range, attack.
           // ONLY WHEN PASSIVE IS UNLOCKED
           // Attack(GetInsectsInRange());
        }
        

        // passive damage tick
        tickTimer += Time.deltaTime;

        List<Insect> targets = GetInsectsInRange();
        foreach (Insect insect in targets)
        {

        if (insect.visual == null || insect.visual.localPosition.y != 0f) continue;

        insect.ApplyEffect(new ChillEffect(insect, 0.25f, chillLevel, this));

        if (tickTimer >= tickInterval)
        {
            insect.Damage(attackDamage * tickInterval, damageType, elementalType, this, false, new DamageTag [] {DamageTag.AoE, DamageTag.DoT});
        }
        }

        if (tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;
        }

        UpdateBlizzardIndicator();
    }

    private void UpdateBlizzardIndicator()
    {
        if (blizzardIndicatorInstance == null) return;

        if (!SkillTargetingManager.instance.IsTargeting)
        {
            Destroy(blizzardIndicatorInstance);
            blizzardIndicatorInstance = null;
            return;
        }

        Vector2 mouseScreen = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, Camera.main.nearClipPlane));
        mouseWorld.z = 0f;

        Vector2 dir = ((Vector2)mouseWorld - (Vector2)transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        blizzardIndicatorInstance.transform.SetPositionAndRotation(
            transform.position + (Vector3)(dir * indicatorLength * 0.5f),
            Quaternion.Euler(0f, 0f, angle));
        blizzardIndicatorInstance.transform.localScale = new Vector3(indicatorLength, blizzardWidth, 1f);

        blizzardIndicatorInstance.GetComponent<SpriteRenderer>().enabled = true;
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = bAD + (level * 1f);
        baseAttackRange = bAR + (level * 0.1f);
    }

    public override void OnPath2Upgrade(int level)
    {
        chillLevel = 1 + (level);
    }

    public override void OnPath3Upgrade(int level)
    {
        activeDuration = 6f + 1f * level;
        blizzardWidth = 1.5f + 0.5f * level;
        blizzardDamage = 15f + 5f * level;
    }

    public override void ActivateSkill()
    {
        if (blizzardIndicatorInstance != null) return;
        SkillTargetingManager.instance.BeginTargeting(0f, OnTargetConfirmed);
        if (blizzardIndicatorPrefab != null)
        {
            blizzardIndicatorInstance = Instantiate(blizzardIndicatorPrefab, transform.position, Quaternion.identity);
            blizzardIndicatorInstance.GetComponent<SpriteRenderer>().enabled = false;
        }
    }

    private void OnTargetConfirmed(Vector3 targetPosition)
    {
        skillCooldownTimer = skillCooldown;
        Vector2 direction = ((Vector2)targetPosition - (Vector2)transform.position).normalized;
        GameObject obj = Instantiate(blizzardPrefab, transform.position, Quaternion.identity);
        obj.GetComponent<Blizzard>()?.Initialize(transform.position, direction, blizzardWidth, activeDuration, blizzardDamage, chillLevel + 1, this);
    }

    protected override void Attack()
    {

        base.Attack();
    }

    // DESCRIPTION

    public override string GetName()
    {
        return "<b><color=#00FFFF>Snowdrop</color></b>";
    }

    public override string GetDescription()
    {
        return $"The {GetName()}'s mere frosty presence damages and slows insects around her.";
    }

    public override string GetAttackDescription()
    {
        return $"Freezes the ground around her continuously dealing <color=green><b>{attackDamage}</b></color> <color=#00FFFF>Ice</color> <color=#FFB6C1>Magic</color> damage per second to insects.";
    }

    public override string GetSkillDesription()
    {
        return $"Summon a strong blizzard, towards the targeted area. The blizzard deals <color=green><b>{blizzardDamage}</b></color> <color=#00FFFF>Ice</color> <color=#FFB6C1>Magic</color> damage per second to insects caught in the area.";
    }

    public override string GetPassiveDescription()
    {
        return $"The frosty aura applies a <color=#00FFFF>Chill</color> effect, slowing down insects by <color=green><b>{24+ 6*effectivePath2Level}%</b></color>.";
    }

    public override string GetPath1Description()
    {
        return $"Attack:\n\n{GetAttackDescription()}\n\nIncrease Attack Damage by <b><color=green>0.5</color></b> per level. [<b><color=green>+" + (1f * path1Level) + "</color></b>]\n" +
        "Increase Attack Range by <b><color=green>0.1</color></b> per level. [<b><color=green>+" + (0.1f * path1Level) + "</color></b>]\n\n" +
        "Level: [<color=green><b>" + path1Level + "/" + pathLevelCap + "</b></color>] <color=green><b>(+" + (effectivePath1Level-path1Level) + ")</b></color>";
    }

    public override string GetPath2Description()
    {
        return $"Passive:\n\n{GetPassiveDescription()}\n\nIncrease <color=#00FFFF>Chill</color> slowing effect by <color=green><b>6%</b></color> per level. [<color=green><b>+" + (6*effectivePath2Level) + "</b></color>]\n\n" +
        "Level: [<color=green><b>" + path2Level + "/" + pathLevelCap + "</b></color>] <color=green><b>(+" + (effectivePath2Level-path2Level) + ")</b></color>";
    }

    public override string GetPath3Description()
    {
        return $"Skill:\n\n{GetSkillDesription()}\n\nIncrease Damage Per Second by <color=green><b>5</b></color> per level. [<color=green><b>+" + (5*effectivePath3Level) + "</b></color>]\n\n" +
        "Increase duration by <color=green><b>1</b></color> second per level. [<color=green><b>" + (1*effectivePath3Level) + "</b></color>]\n\n" +
        "Increase width by <color=green><b>0.5</b></color> units per level. [<color=green><b>" + (0.5*effectivePath3Level) + "</b></color>]\n\n" +
        "Level: [<color=green><b>" + path3Level + "/" + pathLevelCap + "</b></color>] <color=green><b>(+" + (effectivePath3Level-path3Level) + ")</b></color>";  
    }
}
