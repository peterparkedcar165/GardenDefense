using System.Collections.Generic;

// every heal whose source is a rhodiola (her direct heals, her burgeon ticks, her revive heal)
// gives the healed plant GoldenRootArmorEffect. one plant only ever holds one, a new heal just
// refreshes it. removing the mulch strips the armor from every plant holding it
public class GoldenRootMulchEffect : MulchEffect
{
    private readonly GoldenRootMulchData typedData;

    public GoldenRootMulchEffect(GoldenRootMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate() => Entity.OnHeal += HandleHeal;

    public override void Deactivate()
    {
        Entity.OnHeal -= HandleHeal;
        foreach (Plant plant in new List<Plant>(Plant.allPlants))
            if (plant != null) plant.RemoveEffect<GoldenRootArmorEffect>();
    }

    private void HandleHeal(EntityEventData heal)
    {
        if (heal.source is not Rhodiola rhodiola || rhodiola == null) return;
        if (heal.target is not Plant healed || healed == null || !healed.IsAlive) return;
        healed.ApplyEffect(new GoldenRootArmorEffect(healed, typedData.duration, 1, rhodiola, typedData.armorBonus));
    }
}
