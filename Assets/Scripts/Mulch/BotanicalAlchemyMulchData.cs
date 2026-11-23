using System.Collections.Generic;
using UnityEngine;

// Botanical Alchemy (Tommy, 2026-11-23): elemental reactions this plant sets off make it Sun, when
// they land and each time they take effect; primers alone never pay. MulchManager.ReactionEvent and
// PayReaction do the paying, called where each reaction lands or works: Entity.ApplyEffect (landing),
// the Burn, Poisoned and Frostbite ticks, Corrosion's armor drops, Sludge used up (DoTEffect.OnApply,
// SludgeEffect), Brittle's bonus hits and crits on a Fractured insect (Entity.Damage), every insect
// Germinate's bloom hits, and both hits of Windshear's blast on every insect (WindPrimer)
[CreateAssetMenu(fileName = "BotanicalAlchemyMulch", menuName = "Scriptable Objects/Mulch/Botanical Alchemy")]
public class BotanicalAlchemyMulchData : MulchData
{
    // at most this many payouts per reaction on one insect, counted again from 0 when the reaction is
    // refreshed; a Germinate bloom or a Windshear blast counts its own (Tommy: 16)
    public const int MaxEventsPerReaction = 16;

    [Tooltip("Sun made for every event of a reaction this plant set off")]
    public TierValue sunPerEvent = new TierValue(1f, 2f, 4f);

    public override MulchEffect CreateEffect(MulchItem item) => new BotanicalAlchemyMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"Each time the plant applies an {Kw("Elemental Reaction", ReactionColor)}, and each time one it applied takes effect, " +
            $"it produces {Green(sunPerEvent.GetInt(r).ToString())} {SunLabel}. Primers alone never do.\n\n" +
            $"{Kw("Burn", FireColor)}, {Kw("Poisoned", PoisonColor)} and {Kw("Frostbite", IceColor)} pay on every tick, " +
            $"{Kw("Corrosion", CorrosionColor)} on every armor drop, {Kw("Brittle", IceColor)} on every bonus hit, " +
            $"{Kw("Fracture", IceColor)} on every critical hit taken, {Kw("Sludge", PoisonColor)} when it is used up, " +
            $"{Kw("Germinate", GrassColor)} and {Kw("Windshear", WindColor)} on every insect they hit; " +
            $"{Kw("Freeze", IceColor)} and {Kw("Steam", SteamColor)} only when applied.\n\n" +
            $"At most {Green(MaxEventsPerReaction.ToString())} times per reaction on one insect, counted again when the reaction is refreshed.";
    }

    // a reaction needs two different elements in the loadout
    public override bool IsUseful(List<PlantData> loadout)
    {
        HashSet<ElementalType> elements = new HashSet<ElementalType>();
        if (loadout != null)
            foreach (PlantData plant in loadout)
                if (plant != null && plant.elementalType != ElementalType.Neutral) elements.Add(plant.elementalType);
        return elements.Count >= 2;
    }
}
