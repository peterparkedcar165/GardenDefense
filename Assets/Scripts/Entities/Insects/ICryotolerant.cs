// marker interface for insects immune to Freeze (Winter Moth, Snow Ant, Snow Fly). the actual
// immunity is enforced centrally in Insect.ApplyEffect (and, for flyers, guarded against in
// FlyingInsect.ApplyEffect so a blocked Freeze can't still ground them) - implementers don't
// need to override ApplyEffect themselves at all, just implement this and get the immunity for
// free, plus the shared "Cryotolerance" description line via Insect.CryotoleranceLine()
public interface ICryotolerant
{
}
