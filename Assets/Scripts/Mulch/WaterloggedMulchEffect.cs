using System.Collections.Generic;

// SoakedEffect calls Mark when a soak lands at its waterlily's max stacks and Unmark when that
// soak ends. the bonus goes into the insect's water vulnerability slot (Entity.SetWaterVulnerability),
// the same one bubble prison uses, so an insect that is both soaked and bubbled only gets the
// stronger of the two. picking this mulch marks insects already at max stacks right away, and
// removing it clears every mark
public class WaterloggedMulchEffect : MulchEffect
{
    public static bool IsActive;
    private static readonly HashSet<SoakedEffect> marked = new HashSet<SoakedEffect>();

    public WaterloggedMulchEffect(WaterloggedMulchData data) : base(data) {}

    public static void Mark(SoakedEffect soaked, float bonus)
    {
        if (soaked == null || soaked.target == null) return;
        soaked.target.SetWaterVulnerability(soaked, bonus);
        marked.Add(soaked);
    }

    public static void Unmark(SoakedEffect soaked)
    {
        if (soaked == null || !marked.Remove(soaked)) return;
        if (soaked.target != null) soaked.target.ClearWaterVulnerability(soaked);
    }

    public override void Activate()
    {
        IsActive = true;
        foreach (Insect insect in new List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive) continue;
            SoakedEffect soaked = insect.GetEffect<SoakedEffect>();
            if (soaked != null && soaked.AtMaxStacks && soaked.source is Waterlily waterlily)
                Mark(soaked, waterlily.BubbleDamageTaken);
        }
    }

    public override void Deactivate()
    {
        IsActive = false;
        foreach (SoakedEffect soaked in marked)
            if (soaked != null && soaked.target != null) soaked.target.ClearWaterVulnerability(soaked);
        marked.Clear();
    }
}
