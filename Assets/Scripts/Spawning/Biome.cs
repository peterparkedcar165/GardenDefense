// a level's biome (Tommy, 2026-11-25): for now it only picks the right panel's wood colour (GameHUD).
// Auto takes it from the level number, in the level selector's groups of five
public enum Biome
{
    Auto = 0,
    Daytime,
    Nighttime,
    Desert,
    DesertNight,
    Cave,
    Snow,
    Underwater,
}

public static class Biomes
{
    // 1 to 5 day, 6 to 10 night, 11 to 15 desert, 16 to 20 cave, 21 to 25 snow, 26 and on underwater: the
    // level selector's columns. no level number falls on desert night yet; a level gets it by its own
    // LevelConfig.biome
    public static Biome ForLevel(int level) =>
        level <= 5 ? Biome.Daytime
        : level <= 10 ? Biome.Nighttime
        : level <= 15 ? Biome.Desert
        : level <= 20 ? Biome.Cave
        : level <= 25 ? Biome.Snow
        : Biome.Underwater;
}
