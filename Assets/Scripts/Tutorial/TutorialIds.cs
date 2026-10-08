// central place for every tutorial popup's id string, grouped by level - keeps call sites from
// scattering raw string literals (a typo'd id just silently never fires again, no compile error
// to catch it). naming convention: level{N}_{moment}
public static class TutorialIds
{
    public const string Level1PlantSelection = "level1_plantselection";
    public const string Level1FertilizerSelection = "level1_fertilizerselection";
    public const string Level1StartLevel = "level1_startlevel";
    public const string Level1PlantInfo = "level1_plantinfo";
    public const string Level1FirstFertilizer = "level1_firstfertilizer";
    public const string Level1SoldierAntAppears = "level1_soldierantappears";
    public const string Level2StartLevel = "level2_startlevel";
    public const string Level2Primers = "level2_primers";
}
