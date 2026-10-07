using System.Collections;
using UnityEngine;

public class SoldierAnt : Ant
{
    protected override void Awake()
    {
        base.Awake();
        LoadData();

        if (ProceduralLevel.CurrentConfig != null && ProceduralLevel.CurrentConfig.levelNumber == 1)
            StartCoroutine(TriggerSoldierAntTutorialAfterDelay());
    }

    // first-ever Soldier Ant spawn pauses the game for a short explainer (see
    // TutorialContent.Level1SoldierAntAppears), 4 seconds after it appears rather than
    // instantly - harmless to attempt on every spawn since TryShow no-ops once seenTutorials
    // already has it. Unity stops this coroutine automatically if this ant dies/despawns first
    private IEnumerator TriggerSoldierAntTutorialAfterDelay()
    {
        yield return new WaitForSeconds(4f);

        if (TutorialManager.instance != null
            && TutorialManager.instance.TryShow(TutorialIds.Level1SoldierAntAppears, TutorialContent.Level1SoldierAntAppears,
                () => GameManager.instance.SetPause(false)))
        {
            GameManager.instance.SetPause(true);
        }
    }

    public override string GetDescription() =>
        "Bulkier insect with increased Physical and Nature resistances. The Soldier Ant takes " +
        $"<b>{(data != null ? data.flatPhysicalDamageReduction : 0f):F0}</b> reduced damage from Physical Attacks." + AggressivityLine();

    public override void Damage(float damageDealt, DamageType damageType, ElementalType elementalType, Entity source, bool canCrit, DamageTag[] damageTag, bool forceCrit = false, float? onHitEffectivenessOverride = null) // sourced
    {
        float reducedDamage;
        // passive of the soldier ant, reduces physical damage taken by a flat amount
        if (damageType == DamageType.Physical)
        {
            reducedDamage = Mathf.Max(0, damageDealt - data.flatPhysicalDamageReduction);
        }
        else // if its magic or true, ignores the reduction
        {
            reducedDamage = damageDealt;
        }

        base.Damage(reducedDamage, damageType, elementalType, source, canCrit, damageTag, forceCrit, onHitEffectivenessOverride); // calls up to parent for damage reduction
    }

    public override void Damage(float damageDealt, DamageType damageType, ElementalType elementalType, DamageTag[] damageTag) // non-sourced
    {
        float reducedDamage;
        // passive of the soldier ant, reduces physical damage taken by a flat amount
        if (damageType == DamageType.Physical)
        {
            reducedDamage = Mathf.Max(0, damageDealt - data.flatPhysicalDamageReduction);
        }
        else // if its magic or true, ignores the reduction
        {
            reducedDamage = damageDealt;
        }

        base.Damage(reducedDamage, damageType, elementalType, damageTag); // calls up to parent for damage reduction
    }
}
