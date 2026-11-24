using UnityEngine;
using System.Collections.Generic;

public abstract class Aura : Plant
{
    public override void UpdateStats()
    {
        base.UpdateStats();
        
    }

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Update()
    {
        base.Update();

        attackCooldown = 1 / attackSpeed;

    }
    
    protected override bool GetAttackBarVisible() => attackCooldown > 0f;

    protected virtual void Attack()
    {
        attackCooldownTimer = 0; // reset attack timer
        if (data != null) SfxPlayer.Play(data.attackSound, transform.position);
    }

    // one list per aura plant, filled again on every call instead of a new list each time. callers
    // loop over it straight away and must not keep it, the next call empties it
    private readonly List<Insect> _insectsInRange = new List<Insect>();

    protected List<Insect> GetInsectsInRange()
    {
        _insectsInRange.Clear();
        Vector2 center = transform.position;
        float rangeSqr = attackRange * attackRange;

        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive) continue;
            if (((Vector2)insect.transform.position - center).sqrMagnitude <= rangeSqr)
                _insectsInRange.Add(insect);
        }
        return _insectsInRange;
    }

    protected bool HasInsectsInRange()
    {
        Vector2 center = transform.position;
        float rangeSqr = attackRange * attackRange;
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive) continue;
            if (((Vector2)insect.transform.position - center).sqrMagnitude <= rangeSqr)
                return true;
        }
        return false;
    }
}
