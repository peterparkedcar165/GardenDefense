using System.Collections;
using UnityEngine;

// turns any plant into a small sun producer: Plant.GenerateSun on its own timer, so its sun yield,
// drought and solar flare all apply. the timer keeps its progress through a merge
public class SunSapMulchEffect : MulchEffect
{
    private readonly SunSapMulchData typedData;
    private Coroutine routine;
    private float timer;
    // sun it made this level
    private int made;

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

    public override string GetStatus() => $"Sun made: {made}\nNext {SunAmount} Sun in {Mathf.Max(0f, Interval - timer):0}s";

    private int SunAmount => VI(typedData.sunAmount);
    private float Interval => V(typedData.interval);

    private IEnumerator Run()
    {
        while (true)
        {
            if (OwnerAlive && Interval > 0f)
            {
                timer += Time.deltaTime;
                if (timer >= Interval)
                {
                    timer -= Interval;
                    Tick();
                }
            }
            yield return null;
        }
    }

    // one batch of sun
    private void Tick()
    {
        if (!OwnerAlive) return;
        int batch = Owner.GenerateSun(SunAmount);
        made += batch;
        if (batch > 0) SunGenerationParticleEffect.Spawn(Owner.transform.position, batch, 0.5f);
    }
}
