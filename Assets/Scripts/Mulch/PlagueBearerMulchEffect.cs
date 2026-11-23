using System.Collections.Generic;
using UnityEngine;

// PoisonedEffect.OnTargetDied finds this on the poison's source (MulchManager.PlagueBearerOf) and
// calls Spread. a spread that kills its new insect spreads again, so chains are fine
public class PlagueBearerMulchEffect : MulchEffect
{
    private readonly PlagueBearerMulchData typedData;
    private bool on;
    // insects it passed poison to this level and the seconds it passed
    private int passedTo;
    private float seconds;

    public PlagueBearerMulchEffect(PlagueBearerMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate() => on = Owner != null;

    public override void Deactivate() => on = false;

    // the nearest living insects within the radius each get the share of what the dying one had left
    public void Spread(PoisonedEffect dying)
    {
        if (!on || dying == null || !(dying.target is Insect dead) || dead == null) return;
        float passed = dying.duration * V(typedData.durationShare);
        int count = VI(typedData.targets);
        float radius = V(typedData.radius);
        if (passed <= 0f || count <= 0) return;

        Vector3 at = dead.transform.position;
        List<Insect> near = new List<Insect>();
        foreach (Insect insect in Insect.allInsects)
            if (insect != null && insect != dead && insect.IsAlive && insect.team != Team.Friendly
                && Vector3.Distance(at, insect.transform.position) <= radius)
                near.Add(insect);
        near.Sort((a, b) => Vector3.Distance(at, a.transform.position).CompareTo(Vector3.Distance(at, b.transform.position)));

        for (int i = 0; i < near.Count && i < count; i++)
        {
            if (!PoisonedEffect.SpreadTo(near[i], dying, passed)) continue;
            passedTo++;
            seconds += passed;
        }
    }

    public override string GetStatus() => $"Poison passed on: {passedTo} times ({seconds:0.#}s in all)";
}
