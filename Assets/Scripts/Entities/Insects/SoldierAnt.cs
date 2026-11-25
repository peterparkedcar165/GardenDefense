using System.Collections;
using UnityEngine;

public class SoldierAnt : Ant
{
    protected override void Awake()
    {
        base.Awake();
        LoadData();

        // once seen, TryShow would only turn it down (and log) four seconds later on every spawn
        if (ProceduralLevel.CurrentConfig != null && ProceduralLevel.CurrentConfig.levelNumber == 1
            && !(TutorialManager.instance != null && TutorialManager.instance.HasSeen(TutorialIds.Level1SoldierAntAppears)))
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
                TutorialSpeaker.AcornKnight, () => GameManager.instance.SetPause(false)))
        {
            GameManager.instance.SetPause(true);
        }
    }

    // moved here from the Worker Ant (Tommy, 2026-11-24). nothing reads eatMultiplier yet: the game
    // has no path blockers, so this only shows in the description for now
    public override float eatMultiplier => 1.5f;

    public override string GetAbilityDescription() =>
        $"Takes <b>{(data != null ? data.flatPhysicalDamageReduction : 0f):F0}</b> reduced damage from <color=#A0522D><b>Physical Attacks</b></color>, " +
        $"and deals <b>{eatMultiplier:F1}x</b> damage to path blockers.";

    public override void Damage(float damageDealt, DamageType damageType, ElementalType elementalType, Entity source, bool canCrit, DamageTag[] damageTag, bool forceCrit = false, float? onHitEffectivenessOverride = null) // sourced
    {
        float reducedDamage;
        // passive of the soldier ant, reduces physical damage taken by a flat amount
        if (damageType == DamageType.Physical)
        {
            reducedDamage = Mathf.Max(0, damageDealt - FlatPhysicalReduction);
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
            reducedDamage = Mathf.Max(0, damageDealt - FlatPhysicalReduction);
        }
        else // if its magic or true, ignores the reduction
        {
            reducedDamage = damageDealt;
        }

        base.Damage(reducedDamage, damageType, elementalType, damageTag); // calls up to parent for damage reduction
    }

    // no data (LoadData allows that) means no reduction, instead of an error on every hit
    private float FlatPhysicalReduction => data != null ? data.flatPhysicalDamageReduction : 0f;
}
