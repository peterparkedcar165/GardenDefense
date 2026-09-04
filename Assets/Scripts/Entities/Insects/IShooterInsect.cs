// marker interface for insects that only ever attack via their own custom ranged/shooting cycle
// (e.g. Bombardier Beetle's lob-and-splash lock/charge/fire loop), never the base melee Attack().
// Insect.target's base implementation returns null for any IShooterInsect automatically (see
// Insect.cs), so implementers don't need to override `target` themselves just to disable melee -
// that's only needed for insects with their own bespoke targeting (Moth, Wasp, Harvestman, etc.),
// which aren't shooters anyway. from your own attack cycle, call Insect.GetTauntPlantTarget() to
// respect Taunt the same way every shooter should: forced to engage the taunter (via your own
// shoot mechanism, never melee) instead of your normal target-scan, for as long as it holds
public interface IShooterInsect
{
}
