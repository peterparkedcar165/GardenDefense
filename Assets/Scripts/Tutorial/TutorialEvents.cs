// ids for specific in-game actions a tutorial page can wait on (see TutorialPage.waitForEventId
// and TutorialManager.NotifyEvent). call sites announce these unconditionally wherever that
// action actually happens - NotifyEvent is a no-op unless a tutorial is currently on a page
// waiting for that exact id, so there's no harm calling it outside a tutorial
public static class TutorialEvents
{
    public const string HoverAcornKnightSlot = "hover_acornknight_slot";
    public const string SelectAcornKnightSlot = "select_acornknight_slot";
    public const string ClickConfirmButton = "click_confirm_button";
    public const string SelectFertilizer = "select_fertilizer";
    public const string PlaceAcornKnightOnDirt = "place_acornknight_dirt";
    public const string UprootAcornKnightFromDirt = "uproot_acornknight_dirt";
    public const string PlaceAcornKnightOnGrass = "place_acornknight_grass";
    public const string ClickStartLevelButton = "click_start_level_button";
    public const string ClickPlacedAcornKnight = "click_placed_acornknight";
    public const string HoverPlantInfoIcon = "hover_plantinfo_icon";
    public const string HoverStatsPanel = "hover_statspanel";
    public const string HoverPlantPath = "hover_plant_path";
    public const string UpgradeAcornAttackPath = "upgrade_acorn_attack_path";
    public const string ClickFertilizerQueueButton = "click_fertilizer_queue_button";
    public const string SelectRandomFertilizer = "select_random_fertilizer";
    public const string PlaceSunflowerOnDirt = "place_sunflower_dirt";
}
