// marker for status effects that should be unique per target, where the instance
// carrying the higher elemental affinity wins over a weaker reapplication attempt
public interface IElementalAffinityEffect
{
    float AffinityPower { get; }
}
