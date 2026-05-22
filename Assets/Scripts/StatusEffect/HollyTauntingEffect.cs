public class HollyTauntingEffect : TauntingEffect
{
    public HollyTauntingEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source) { }

    private Holly Holly => source as Holly;

    public override string GetName() => "<color=#00FFFF>Holly Taunt</color>";
    public override string GetDescription()
    {
        if (Holly == null) return "Forces nearby insects to target it.";
        float hollyDmg  = Holly.RetaliationHollyPct  * Holly.attackDamage;
        float insectPct = Holly.RetaliationInsectPct * 100f;
        return $"Forces nearby insects to target it. Returns <color=#00FFFF><b>{hollyDmg:F0}</b></color> + <color=green><b>{insectPct:F0}%</b></color> Attacker ATK as <color=#00FFFF>Ice</color> Physical damage.";
    }
}
