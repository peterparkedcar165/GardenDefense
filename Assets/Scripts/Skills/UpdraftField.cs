using UnityEngine;
using System.Collections.Generic;

// Morning Glory skill: a field that blows wind upward, keeping insects aloft and
// applying Levitating (crit vulnerability) for as long as the field is active
public class UpdraftField : MonoBehaviour
{
    private float radius, duration, liftForce, maxHeight, critBonus;
    private Plant source;
    private float tickTimer;
    private const float tickInterval = 0.01f;
    private const float levitateRefresh = 2f;

    public void Initialize(Vector3 position, float radius, float duration, float liftForce, float maxHeight, float critBonus, Plant source)
    {
        transform.position = position;
        this.radius     = radius;
        this.duration   = duration;
        this.liftForce  = liftForce;
        this.maxHeight  = maxHeight;
        this.critBonus  = critBonus;
        this.source     = source;
        transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
    }

    private void Update()
    {
        duration -= Time.deltaTime;
        if (duration <= 0f) { Destroy(gameObject); return; }

        tickTimer -= Time.deltaTime;
        if (tickTimer > 0f) return;
        tickTimer = tickInterval;

        foreach (Insect insect in new List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive || insect.isFlying) continue;
            if (Vector3.Distance(transform.position, insect.transform.position) > radius) continue;

            // set a constant upward velocity every frame while below the cap, for a smooth
            // elevator-like rise. once at/above the cap, stop pushing and let gravity bring it
            // back down (a small overshoot of liftForce^2 / 2g). Levitating still applies (below)
            float height = insect.visual != null ? insect.visual.localPosition.y : 0f;
            if (height < maxHeight)
                insect.verticalVelocity = -liftForce;

            // apply once, then just refresh the timer so the crit bonus isn't re-stacked and the
            // status indicator isn't spammed
            LevitatingEffect lev = insect.GetEffect<LevitatingEffect>();
            if (lev == null)
                insect.ApplyEffect(new LevitatingEffect(insect, levitateRefresh, 1, source, critBonus));
            else
                lev.duration = levitateRefresh;
        }
    }
}
