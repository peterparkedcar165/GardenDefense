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
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: Click on me (or Press <b>1</b>) , that'll select me.\n\nThen, place me on any <b><color=#8B4513>Dirt</color></b> tile on the map!", TutorialEvents.PlaceAcornKnightOnDirt), // here, the player is expected to select the acorn knight , and place it on any dirt tile in the map, once that's done, jump to next tutorial text
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: <b>Hold on, my mistake!</b> You shouldn't place me on a <b><color=#8B4513>Dirt</color></b> tile!\n\nClick on the <b>Shovel</b> on the right side of the screen, and then select the <b><color=green>Acorn Knight</color></b> you just placed on the <b><color=#8B4513>Dirt</color></b> tile!", TutorialEvents.UprootAcornKnightFromDirt), // here, the player is expected to select the shovel, and then remove the acorn from the field, once thats done, move to next
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: When you <b>Shovel</b> a plant, you get a 50% refund of all the <b>Sun</b> spent on it. However, since we're still in the <b>Setup Phase</b>, you get 100%.\n\nNow select me once again (or Press <b>1</b>), and place me on a <b><color=green>Grass</color></b> tile!", TutorialEvents.PlaceAcornKnightOnGrass), // player is expected to select the acorn knight again, and then place it on a grass tile
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: Great! Since I'm a <b><color=green>Grass</color></b> element plant, I can be placed on <b><color=green>Grass</color></b> Tiles, and get a bonus point in my <b>Passive Tree</b> which makes me slightly stronger!"),
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: Wonderful, now we're ready to start the level! Click on the <b>START</b> button at the bottom right of your screen (or press <b>Space</b>) to begin!", TutorialEvents.ClickStartLevelButton),
    };

    public static readonly TutorialPage[] Level1PlantInfo =
    {
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: For more information on what I can do, click on the placed <b><color=green>Acorn Knight</color></b>.", TutorialEvents.ClickPlacedAcornKnight), // the player is expected to click on the acorn knight placed then next tutorial proceeds
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: You can hover over me to see my <b>General Information</b>", TutorialEvents.HoverPlantInfoIcon), // the player is expected to hover over the acorn knight's icon in the plant info panel then next tutorial proceeds
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: On the right side of that, you can see my <b>Basic Combat Stats</b> and if you hover over it, you'll see more!", TutorialEvents.HoverStatsPanel), // the player is expected to hover over the statspanel then next tutorial proceeds
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: Below that, you'll see the <b>Attack</b>, <b>Passive</b> and <b>Skill</b> paths! Hover over any of them to see that intricacies of what I have to offer!", TutorialEvents.HoverPlantPath), // the player is expected to hover over either the attack, or passive, or skill tree, once at least one is done, proceed tutorial
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: I notice that you have enough <b>Sun</b> to upgrade something! Let's upgrade the <b>Attack</b> tree by clicking on the <b>+</b> button next to the <b>Attack</b> tree!", TutorialEvents.UpgradeAcornAttackPath), // the player is expected to upgrade the attack path, once done, proceed tutorial, during this time, CANNOT upgrade anything else.
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: Great! Now that the <b>Attack</b> tree is upgraded, I'm a little bit more powerful!\n\nIf you look below, you'll notice that the <b>Passive Tree</b> has an extra level, that's because you placed me on a <b><color=green>Grass</color></b> Tile!"), // player can click on the panel to close the tutorial.
    };

    public static readonly TutorialPage[] Level1FirstFertilizer =
    {
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: After some waves, you'll collect more <b>Fertilizers</b>. They will appear on the left side of the <b>Shovel</b>, click on it!", TutorialEvents.ClickFertilizerQueueButton), // player is expected to click on the fertilizer button, once done, proceed tutorial
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: Once again, you'll be met with three choices of <b>Fertilizers</b>, except these ones will be randomized! You get to select one that you like the most, but if you don't like any of them, you can <b>reroll</b> each one once!", TutorialEvents.SelectRandomFertilizer), // player is expected to click on a fertilizer, once done, proceed tutorial
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: The stat bonuses will be applied to all of your plants for the rest of the level!"), // player can click on the panel to close the tutorial.
    };

    public static readonly TutorialPage[] Level1SoldierAntAppears = // player is not expected to do anything, but simply read the tutorial and click to proceed, but the game is paused and non-unpausable while this tutorial holds active
    {
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: <b>Soldier Ants</b> are tougher ants that take reduced damage from <color=#A0522D><b>Physical</b></color> attacks, but also <b><color=green>Grass</color></b> damage!"),
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: They're also aggressive, but only a little bit...\n\nThey'll attack the closest plant to them, then continue onto their path!"),
        new TutorialPage("<b><color=green>Acorn Knight</color></b>: However, since I am from the <color=#A9A9A9>Ironbark</color> family, I can keep them distracted while others tear them down!"),
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

// level 3, waterlily
// water bonus on water tiles
// wither
// germinate