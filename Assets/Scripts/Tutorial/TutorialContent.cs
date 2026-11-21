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
        new TutorialPage("Hi! Nice to meet you, I'm the <b><color=green>Acorn Knight</color></b>. I'm here to help you learn how to play the game!"),
        new TutorialPage("First, let's learn how to select a plant."),
        new TutorialPage("Hover over me in the <b>Available Plants</b> section. You should get a preview of what I'm able to do!\n\nThen, click on me to select me. Once that's done, I'll move up to the <b>Current Loadout</b> section.", TutorialEvents.SelectAcornKnightSlot),
        new TutorialPage("Great! Now that I'm selected, click on the <b>Next</b> button.", TutorialEvents.ClickConfirmButton),
    };

    public static readonly TutorialPage[] Level1FertilizerSelection =
    {
        new TutorialPage("After choosing your team of plants, you get to pick one <b>Fertilizer</b> which grants you a bonus that can change the way a level plays out!"),
        new TutorialPage("Select the <b>Fertilizer</b> you wish to benefit from!", TutorialEvents.SelectFertilizer),
    };

    public static readonly TutorialPage[] Level1StartLevel =
    {
        new TutorialPage("Now that the level has started, you can see the selected plants in the top right corner of the screen."),
        new TutorialPage("Click on me (or Press <b>1</b>) , that'll select me.\n\nThen, place me on any <b><color=#8B4513>Dirt</color></b> tile on the map!", TutorialEvents.PlaceAcornKnightOnDirt), // here, the player is expected to select the acorn knight , and place it on any dirt tile in the map, once that's done, jump to next tutorial text
        new TutorialPage("<b>Hold on, my mistake!</b> You shouldn't place me on a <b><color=#8B4513>Dirt Tile</color></b>!\n\nClick on the <b>Shovel</b> on the right side of the screen, and then select the <b><color=green>Acorn Knight</color></b> you just placed on the <b><color=#8B4513>Dirt Tile</color></b>!", TutorialEvents.UprootAcornKnightFromDirt), // here, the player is expected to select the shovel, and then remove the acorn from the field, once thats done, move to next
        new TutorialPage("When you <b>Shovel</b> a plant, you get a <b>50% refund</b> of all the <b>Sun</b> spent on it. However, since we're still in the <b>Setup Phase</b>, you get <b>100%</b>.\n\nNow select me once again (or Press <b>1</b>), and place me on a <b><color=green>Grass Tile</color></b>!", TutorialEvents.PlaceAcornKnightOnGrass), // player is expected to select the acorn knight again, and then place it on a grass tile
        new TutorialPage("Awesome! So, since I'm a <b><color=green>Grass</color></b> element plant, I can be placed on <b><color=green>Grass Tiles</color></b>.\nOn top of that, we <b><color=green>Grass</color></b> element plants get a bonus point in our <b>Passive Tree</b> which increases the effectiveness of what makes us unique!"),
        new TutorialPage("Wonderful, now we're ready to start the level! Click on the <b>START</b> button at the bottom right of your screen (or press <b>Space</b>) to begin!", TutorialEvents.ClickStartLevelButton),
    };

    public static readonly TutorialPage[] Level1PlantInfo =
    {
        new TutorialPage("For more information on what I can do, click on the placed <b><color=green>Acorn Knight</color></b>.", TutorialEvents.ClickPlacedAcornKnight), // the player is expected to click on the acorn knight placed then next tutorial proceeds
        new TutorialPage("You can hover over my portrait to see my <b>General Information</b>", TutorialEvents.HoverPlantInfoIcon), // the player is expected to hover over the acorn knight's icon in the plant info panel then next tutorial proceeds
        new TutorialPage("On the right side of that, you can see my <b>Basic Combat Stats</b> and if you hover over it, you'll be able to see additional details regarding my stats.\nYou could also see active <b>Status Effects</b>! Except, I don't have any right now so you won't see anything. Ha!", TutorialEvents.HoverStatsPanel), // the player is expected to hover over the statspanel then next tutorial proceeds
        new TutorialPage("Below that, you'll see the <b>Attack</b>, <b>Passive</b> and <b>Skill</b> paths! Hover over any of them to see that intricacies of what I have to offer!", TutorialEvents.HoverPlantPath), // the player is expected to hover over either the attack, or passive, or skill tree, once at least one is done, proceed tutorial
        new TutorialPage("I notice that you have enough <b>Sun</b> to upgrade something! Let's upgrade the <b>Attack</b> tree by clicking on the <b>upgrade</b> button next to the <b>Attack</b> tree!", TutorialEvents.UpgradeAcornAttackPath), // the player is expected to upgrade the attack path, once done, proceed tutorial, during this time, CANNOT upgrade anything else.
        new TutorialPage("Great! Now that the <b>Attack</b> tree is upgraded, I'm a little bit more powerful!\n\nIf you look below, you'll notice that the <b>Passive Tree</b> has an extra level. Remember? It's because you placed me on a <b><color=green>Grass Tile</color></b>!"), // player can click on the panel to close the tutorial.
    };

    // shown the first time a mulch drops in level 1 (MulchManager.OnDropSpawned), game paused.
    // page 3 waits for the player to take the drop (TutorialEvents.TakeMulch: clicked or dragged
    // into the bag, or dragged onto a plant); a drop can be taken mid hop, so pausing doesn't block it
    public static readonly TutorialPage[] Level1FirstMulch =
    {
        new TutorialPage("Insects sometimes drop <b>Mulches</b> when slain!\n\nThese are little bags with cute glitter around them."),
        new TutorialPage("<b>Mulches</b> are going to be your friends in the garden, they grant a plant various bonuses that could really help them defend your garden!\n\nYou can hover over them to see what kind of bonus they can provide."),
        new TutorialPage("Click on it to add it in your <b>Mulch Inventory</b>, or you could actually drag it on your plant!\n\nYour inventory will serve as storage for mulches you wish to save or to use later in the level.", TutorialEvents.TakeMulch), // here, the player is expected to either click on the mulch to add it to the inventory OR drag it to a plant OR the inventory.
        new TutorialPage("<b>BUT</b>! You should know that the mulches don't carry over from level to level!"),
        new TutorialPage("A plant can hold up to <b>6</b> mulches, and so can you. Two <b>identical</b> mulches of the same rarity will <b>merge</b> into a stronger one.\n\nIf a mulch seems uninteresting, right click on it to sell it! You can only sell mulches that are in <b>your</b> inventory."),
    };

    public static readonly TutorialPage[] Level1SoldierAntAppears = // player is not expected to do anything, but simply read the tutorial and click to proceed, but the game is paused and non-unpausable while this tutorial holds active
    {
        new TutorialPage("So this is a <b>Soldier Ant</b>. It's a tougher variant of an ant that's like suuuuuper duuuuper bulky and takes reduced damage from <color=#A0522D><b>Physical</b></color> AND <b><color=green>Grass</color></b> attacks!\n\nI'm not really the best plant to fight this ant."),
        new TutorialPage("They're also mildly <b><color=#A0522D>aggressive</color></b>, meaning that they walk off the path to attack us!"),
        new TutorialPage("Here's where I shine! So, you know how every plant has a passive?\n\nYeah, well mine allows me to raise my shield, entering a <b><color=green>Defensive Stance</color></b> whenever I sense that an insect sees me as their target!"),
        new TutorialPage("While I'm holding up my shield, I have increased <b><color=#00CED1>Armor</color></b>, and my attacks change to a <b>Shield Bash</b>, which doesn't benefit from my <b><color=green>Attack Damage</color></b>, but rather my <b><color=#00CED1>Armor</color></b>!"),
        new TutorialPage("So, when the <b>Soldier Ant</b> attacks a plant, he hits once, then continues on its way.\nAnother cool thing about my <b>passive</b> is that insects that target me won't leave after hitting me. I'm able to hold their attention while other plants tear them down!"),
    };

    // triggers the moment the player enters level 2's setup phase (the initial pre-level
    // fertilizer selection), same hook point as Level1StartLevel - see FertilizerSelectionUI
    
    public static readonly TutorialPage[] Level2StartLevel =
    {
        new TutorialPage("Hiya! I'm the <b><color=orange>Sunflower</color></b>. I'm happy to meet you!"),
        new TutorialPage("Click on me, that'll select me.\n\nThen, place me on any <b><color=#8B4513>Dirt Tile</color></b> on the map!", TutorialEvents.PlaceSunflowerOnDirt), // player is expected to select the sunflower and place her on any dirt tile, once done, proceed tutorial
        new TutorialPage("See, unlike the <b><color=green>Acorn Knight</color></b>, I'm a <b><color=orange>Fire</color></b> plant, so I can't be placed on <b><color=green>Grass Tiles</color></b>.\n\nWe <b><color=orange>Fire</color></b> plants do however benefit from a <b><color=orange>Sunny Weather</color></b>. So, we get an extra <b>Passive</b> point whenever we're exposed to the <b><color=orange>sun</color></b>!"),
        new TutorialPage("I can help you defend the garden in many ways!\n\nMy fireballs deal <color=orange><b>Fire</b></color> <color=#FFB6C1><b>Magic Damage</b></color>, so I'm really the perfect solution to the tough <b>Soldier Ants</b>!"),
        new TutorialPage("As for my <b>passive</b>, I produce <b><color=orange>Sun</color></b> as time passes, so you'll be able to place and upgrade more plants as long as I'm on the field!\n\nLanding shots with my fireballs also <b>reduces</b> the cooldown on my <b><color=orange>sun</color></b> generation!"),
        new TutorialPage("And finally my skill allows me to gather fire energy and shoot down a <color=orange><b>Sunray</b></color>, a beam that deals continuous <color=orange><b>Fire</b></color> damage!"), // player can click on the panel to close the tutorial.
    };

    // triggers the moment any plant inflicts an elemental primer on an insect for the first
    // time (see ElementalDebuff.OnApply) - purely informational, no action required
    public static readonly TutorialPage[] Level2Primers =
    {
        new TutorialPage("Notice the elemental icon above that insect? That means it's being <b>primed</b> with an <b>Elemental Debuff</b>!\n\nIf another elemental attack lands on it while primed, it triggers an <b>Elemental Reaction</b>!"),
        new TutorialPage("In our case, whenever <b><color=green>Grass</color></b> comes in contact with <color=orange><b>Fire</b></color>, it starts a <color=orange><b>Burn</b></color> effect on the target, dealing <color=#FFB6C1><b>Magic</b></color> <color=orange><b>Fire</b></color> damage over time!"), // player can click on the panel to close the tutorial.
    };

    // triggers the moment the player enters level 3's setup phase (the initial pre-level
    // fertilizer selection), same hook point as Level1StartLevel/Level2StartLevel - see
    // FertilizerSelectionUI
    public static readonly TutorialPage[] Level3StartLevel =
    {
        new TutorialPage("Hi! I'm <b><color=#3399FF>Waterlily</color></b>, soooo nice to meet you!"),
        new TutorialPage("Click on me, that'll select me.\n\nThen, place me on any <b><color=#4FC3F7>Water</color></b> tile on the map!\n\nYup, I'm an <b><color=#4FC3F7>aquatic</color></b> plant so I can only be placed on <b><color=#4FC3F7>Water</color></b>.", TutorialEvents.PlaceWaterlilyOnWater), // player is expected to select the waterlily and place her on any water tile, once done, proceed tutorial
        new TutorialPage("I shoot slow-moving bubbles that deal <color=#4FC3F7><b>Water</b></color> <b><color=#FFB6C1>Magic Damage</color></b> to insects in an area around the impact!"),
        new TutorialPage("My <b>passive</b> makes it so that my attacks inflict <b><color=#4FC3F7>Soaked</color></b> on insects, which <b><color=#87CEEB>Slows</color></b> them down but also <b>grounds</b> flying insects!"),
        new TutorialPage("My skill lets me shoot a huge bubble that deals damage and traps insects in place while keeping them airborne!"), // player can click on the panel to close the tutorial.
    };

    // level 1, from 4 seconds into wave 3, once there is an Acorn Knight whose skill is still locked and
    // enough sun to unlock it (see ProceduralLevel.Update), game paused. until then level 1 keeps every
    // skill locked (TutorialManager.Level1SkillLocked). page 1 waits for the unlock, page 2 for the
    // thrown shield to land: the game runs from the throw until it lands (AcornKnight)
    public static readonly TutorialPage[] Level1UseSkillForTheFirstTime =
    {
        new TutorialPage("Things are about to get rough.\n\nAll plants also have a <b>Skill</b>, but they need to be unlocked, so you just gotta click on the vines to unlock my skill.", TutorialEvents.UnlockAcornSkill), // here, the player is expected to click on the skill unlock vines to unlock the skill. note that before this tutorial, the player should NOT be able to unlock the skill.
        new TutorialPage("You can cast my skill by clicking on me, then clicking on the <b>Skill Icon</b> or just by <b>Pressing \"Q\"</b> as a shortcut, then you'll have to aim and click with your mouse to cast it!", TutorialEvents.AcornShieldLanded), // here, the player is expected to cast the acorn skill. the game runs from the cast until the shield lands (AcornKnight), then pauses again for page 3
        new TutorialPage("My skill allows me to throw my shield in a straight line, dropping on the ground when it reaches the destination or it hits an insect. It deals damage, and <b>stuns</b> for a little bit, but it stays on the ground, blocking the path, so insects have no choice but to munch on my shield."), // shown once the thrown shield has landed on the ground
        new TutorialPage("I just won't be able to hold a <b><color=green>Defensive Stance</color></b> while my shield is on the floor, so I will be vulnerable. Just remember that...\n\n\nPlease."),
    };

    // the first time a Waterlily puts a Water primer on an insect, 1 second after it lands (see
    // ElementalDebuff.OnApply), game paused. click through pages only
    public static readonly TutorialPage[] Level3WaterPrimer =
    {
        new TutorialPage("Just like the other plants, my attacks also apply a <b><color=#4FC3F7>Water</color></b> <b>primer</b> to react with the other elements!"),
        new TutorialPage("Whenever <b><color=#4FC3F7>Water</color></b> reacts with <b><color=green>Grass</color></b>, it <b><color=green>Germinates</color></b> for a few seconds, then blooms, dealing <b><color=green>Grass</color></b> <b><color=#A0522D>Physical Damage</color></b> in an area.\n\nWhenever it reacts with <b><color=orange>Fire</color></b>, it <b><color=#4FC3F7>Steams</color></b>, reducing <b><color=orange>Fire Resistance</color></b>, <b><color=#4FC3F7>Water Resistance</color></b>, and <b><color=#FF6B81>Healing Received</color></b> on the affected insect."),
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