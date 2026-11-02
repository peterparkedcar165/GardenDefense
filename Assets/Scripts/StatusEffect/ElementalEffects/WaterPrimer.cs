using UnityEngine;

public class WaterPrimer : ElementalDebuff
{
    public WaterPrimer(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        elementalType = ElementalType.Water;
    }

    public override string GetName() => "<color=#4FC3F7>Water</color>";
    public override string GetDescription() => "Used as a primer to react with other elements";

    // a reaction this primer sets off normally belongs to whoever applied it. Begonia's attack path
    // max bonus hands it to whoever applied the first primer instead, so it runs on their stats
    private Entity ReactionSource(ElementalDebuff firstPrimer)
    {
        if (source is Begonia begonia && begonia.YieldsReactionSource
            && firstPrimer != null && firstPrimer.source != null && firstPrimer.source.health > 0f)
            return firstPrimer.source;
        return source;
    }

    protected override void OnPrimerApply()
    {
        Insect insect = (Insect)target;

        if (insect.HasEffect<IcePrimer>())
        {
            if (insect.freezeInternalCooldown <= 0)
            {
                Entity reactionSource = ReactionSource(insect.GetEffect<IcePrimer>());
                insect.RemoveEffect<WaterPrimer>();
                insect.RemoveEffect<IcePrimer>();
                insect.freezeInternalCooldown = 5f;
                insect.ApplyEffect(new FreezeEffect(insect, 3f * (1f + reactionSource.elementalAffinity), 1, reactionSource));
            }
        }
        else if (insect.HasEffect<PoisonPrimer>())
        {
            Entity reactionSource = ReactionSource(insect.GetEffect<PoisonPrimer>());
            insect.RemoveEffect<WaterPrimer>();
            insect.RemoveEffect<PoisonPrimer>();
            insect.ApplyEffect(new SludgeEffect(insect, 4f, 1, reactionSource));
        }
        else if (insect.HasEffect<FirePrimer>())
        {
            Entity reactionSource = ReactionSource(insect.GetEffect<FirePrimer>());
            insect.RemoveEffect<WaterPrimer>();
            insect.RemoveEffect<FirePrimer>();
            insect.ApplyEffect(new SteamEffect(insect, 8f, 1, reactionSource));
        }
        else if (insect.HasEffect<GrassPrimer>())
        {
            if (insect.germinateInternalCooldown <= 0)
            {
                Entity reactionSource = ReactionSource(insect.GetEffect<GrassPrimer>());
                insect.RemoveEffect<WaterPrimer>();
                insect.RemoveEffect<GrassPrimer>();
                insect.germinateInternalCooldown = 2f;
                insect.ApplyEffect(new GerminateEffect(insect, 2f, 1, reactionSource));
            }
        }
    }
}
