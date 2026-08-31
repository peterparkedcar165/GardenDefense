// implemented by insect types that can carry another insect along with them (e.g. Duskdarter).
// the carried insect itself doesn't need to know about this interface: it just tracks carriedBy
// (see Insect.cs), which alone is enough to freeze its own movement/attacking, exempt it from
// gravity, and exclude it from single-target selection while still leaving it vulnerable to
// AoE/splash damage. Insect.Kill()/Kill(Entity) already call DropCarriedInsect() on death for
// any insect implementing this, so a future carrier type only needs to implement TryCarry and
// keep the carried insect's carriedBy field in sync with pickup/drop
public interface ICarrierInsect
{
    Insect CarriedInsect { get; }
    bool TryCarry(Insect target);
    void DropCarriedInsect();
}
