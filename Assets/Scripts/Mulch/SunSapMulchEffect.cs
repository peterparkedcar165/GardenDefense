using System.Collections;
using UnityEngine;

// turns any plant into a small sun producer: Plant.GenerateSun on its own timer, so its sun yield,
// drought and solar flare all apply. the timer keeps its progress through a merge
public class SunSapMulchEffect : MulchEffect
{
    private readonly SunSapMulchData typedData;
    private Coroutine routine;
    private float timer;

    public SunSapMulchEffect(SunSapMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        StopOnOwner(ref routine);
        routine = StartOnOwner(Run());
    }

    public override void Deactivate() => StopOnOwner(ref routine);

    public override string GetStatus() => $"Next {SunAmount} Sun in {Mathf.Max(0f, typedData.interval - timer):0}s";

    private int SunAmount => VI(typedData.sunAmount);

    private IEnumerator Run()
    {
        while (true)
        {
            if (OwnerAlive && typedData.interval > 0f)
            {
                timer += Time.deltaTime;
                if (timer >= typedData.interval)
                {
                    timer -= typedData.interval;
                    Tick();
                }
            }
            yield return null;
        }
    }

    // one batch of sun, also used by the sunset mulch at the end of a wave
    public void Tick()
    {
        if (!OwnerAlive) return;
        int made = Owner.GenerateSun(SunAmount);
        if (made > 0) SunGenerationParticleEffect.Spawn(Owner.transform.position, made, 0.5f);
    }
}
