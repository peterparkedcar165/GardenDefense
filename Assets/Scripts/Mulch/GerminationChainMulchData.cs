using UnityEngine;

// the bloom and the primer both come from GerminateEffect (see its PrimeHitInsects), so this
// mulch has no numbers of its own: the primer lasts as long as any other (the insect's
// elementalDebuffDuration) and the aura is begonia's attack radius, the same one her blessing uses
[CreateAssetMenu(fileName = "GerminationChainMulchData", menuName = "Scriptable Objects/Mulch/Germination Chain")]
public class GerminationChainMulchData : MulchData
{
    public override MulchEffect CreateEffect() => new GerminationChainMulchEffect(this);

    public override string GetDescription() =>
        $"<color=green><b>Germinate</b></color> blooms inside {TargetPlantLabel("Begonia")}'s aura give every insect they hit a fresh " +
        $"<color=#4FC3F7><b>Water Primer</b></color>. A chain of blooms never primes the same insect twice.";
}
