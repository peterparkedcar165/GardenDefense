// actual tutorial copy, grouped by id - one TutorialPage per page, shown and advanced in order by
// TutorialPopupUI. a page with no waitForEventId advances on click like normal; a page with one
// set stays up without consuming clicks until TutorialManager.NotifyEvent fires that id from
// wherever that action actually happens (see TutorialEvents, and the call sites in
// LoadoutSelectionUI). referenced from wherever TutorialManager.TryShow is called (see
// ProceduralLevel.Start for level1's)
public static class TutorialContent
{
    public static readonly TutorialPage[] Level1PlantSelection =
    {
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: Hi! Nice to meet you, I'm the <b><color=green>Acorn Knight</color></b>. I'm here to help you learn how to play the game!"),
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: First, let's learn how to select a plant."),
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: Hover over me in the <b>Available Plants</b> section. You should get a preview of what I'm able to do!\n\nThen, click on me to select me. Once that's done, I'll move up to the <b>Current Loadout</b> section.", TutorialEvents.SelectAcornKnightSlot),
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: Great! Now that I'm selected, click on the <b>Next</b> button.", TutorialEvents.ClickConfirmButton),
    };

    public static readonly TutorialPage[] Level1FertilizerSelection =
    {
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: After selecting your loadout, you must choose a <b>Fertilizer</b> to help all the selected plants, for the entire level!"),
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: Select a <b>Fertilizer</b> you think will help you the most.", TutorialEvents.SelectFertilizer),
    };

    public static readonly TutorialPage[] Level1StartLevel =
    {
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: Now that the level has started, you can see the selected plants in the top right corner of the screen."),
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: Click on me, that'll select me.\n\nThen, place me on any <b><color=#8B4513>Dirt</color></b> tile on the map!", TutorialEvents.PlaceAcornKnightOnDirt), // here, the player is expected to select the acorn knight , and place it on any dirt tile in the map, once that's done, jump to next tutorial text
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: <b>Hold on, my mistake!</b> You shouldn't place me on a <b><color=#8B4513>Dirt</color></b> tile!\n\nClick on the <b>Shovel</b> on the right side of the screen, and then select the <b><color=green>Acorn Knight</color></b> you just placed on the <b><color=#8B4513>Dirt</color></b> tile!", TutorialEvents.UprootAcornKnightFromDirt), // here, the player is expected to select the shovel, and then remove the acorn from the field, once thats done, move to next
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: When you <b>Shovel</b> a plant, you get a 50% refund of all the Sun spent on it. However, since we're still in the <b>Setup Phase</b>, you get 100%.\n\nNow select me once again, and place me on a <b><color=green>Grass</color></b> tile!", TutorialEvents.PlaceAcornKnightOnGrass), // player is expected to select the acorn knight again, and then place it on a grass tile
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: Great! Since I'm a <b><color=green>Grass</color></b> element plant, I can be placed on <b><color=green>Grass Tiles</color></b>, and get a bonus point in my Passive Tree which makes me slightly stronger!"),
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: Wonderful, now we're ready to start the level! Click on the <b>START</b> button at the bottom right of your screen (or press Space) to begin!", TutorialEvents.ClickStartLevelButton),
    };

    public static readonly TutorialPage[] Level1PlantInfo =
    {
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: For more information on what I can do, click on the placed Acorn Knight.", TutorialEvents.ClickPlacedAcornKnight), // the player is expected to click on the acorn knight placed then next tutorial proceeds
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: You can hover over me to see my <b>General Information</b>", TutorialEvents.HoverPlantInfoIcon), // the player is expected to hover over the acorn knight's icon in the plant info panel then next tutorial proceeds
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: On the right side of that, you can see my <b>Basic Combat Stats</b> and if you hover over it, you'll see more!", TutorialEvents.HoverStatsPanel), // the player is expected to hover over the statspanel then next tutorial proceeds
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: Below that, you'll see the Attack, Passive and Skill paths! Hover over any of them to see that intricacies of what I have to offer!", TutorialEvents.HoverPlantPath), // the player is expected to hover over either the attack, or passive, or skill tree, once at least one is done, proceed tutorial
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: I notice that you have enough Sun to upgrade something! Let's upgrade the Attack tree by clicking on the <b>+</b> button next to the Attack tree!", TutorialEvents.UpgradeAcornAttackPath), // the player is expected to upgrade the attack path, once done, proceed tutorial, during this time, CANNOT upgrade anything else.
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: Great! Now that the Attack tree is upgraded, I'm a little bit more powerful!\n\nIf you look below, you'll notice that the Passive Tree has an extra level, that's because you placed me on a Grass Tile!"), // player can click on the panel to close the tutorial.
    };
}

// need tutorial for when soldier ant appears
// fertilizer
// skill

// level 2, sunflower
// weather
// fire element
// magic damage
// sun producing
// elemental primers, elemental reactions