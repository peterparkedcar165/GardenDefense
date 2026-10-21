// mirrors DamageTag, but for Entity.Heal()/Entity.OnHeal - lets a mulch/stat react to WHAT KIND
// of heal just landed instead of only how much. start minimal; add more as a mulch/stat actually
// needs to distinguish another heal flavor (e.g. lifesteal, skill burst)
public enum HealTag
{
    Regeneration,
}
