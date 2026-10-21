// lets every plant's heals critically strike, same +50% crit-heal bonus Entity.Heal already
// rolls for any source with HealCanCrit - this just grants that flag board-wide
public class CriticalRejuvenationMulchData : MulchData
{
    public override MulchEffect CreateEffect() => new CriticalRejuvenationMulchEffect(this);
}
