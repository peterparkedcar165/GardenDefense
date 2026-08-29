// marker for status effects that grant Respiration: while one of these is active on a
// Submerged plant, Air stops depleting and instead regenerates at this effect's own rate.
// the rate is a property (not a fixed constant) so implementations can compute it dynamically
// from whichever plant/stat/level granted it (e.g. the source's magic power or path level).
// when more than one is active at once, only the strongest actually regenerates Air.
public interface IRespirationEffect
{
    float RespirationRegenPerSecond { get; }
}
