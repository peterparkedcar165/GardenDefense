// how high things stand, in tiles, for line of sight (Tommy, 2026-11-28: heights). the map is seen from above at a
// slant, so a thing's picture shows its height as that many tiles up the screen: the numbers come from the art. a
// plant's eye sits that high over the spot it stands on (its middle, the same dot as its range circle), and what
// stands between it and a body stops the look only where it rises above the straight line between them (LightMask,
// RayFan). on high ground everything stands Cliff higher. light uses none of it: lights are round and go through
// everything (Tommy, 2026-11-28)
public static class Heights
{
    // high ground over low ground: the cliff face is drawn 22 pixels tall
    public const float Cliff = 22f / LightMask.PixelsPerUnit;
    // a plant's eye over its ground, a little under the cliff's top, so from low ground the ground up on a cliff stays
    // out of sight
    public const float Eye = 0.3f;
    // what of a plant or an insect a plant sees: its body, this high over its ground (most insects' pictures sit about
    // this high over their spot). over a cliff's top, so a body below a cliff is seen from above
    public const float Body = 0.4f;
    // a pine (the forest's trees): its top over its foot. only a cell a pine stands in blocks: the pine drawn over
    // the cells behind it is up in the air
    public const float Tree = 184f / LightMask.PixelsPerUnit;
    // the least a boulder, bush or any other obstacle stands: over a body, so from low ground nothing is seen past
    // one, and well under a high ground eye, so from high ground a plant sees over the ones below. a taller picture
    // stands half its drawn height
    public const float Rock = 28f / LightMask.PixelsPerUnit;

    // the eye of something standing on high ground or not
    public static float EyeOver(bool high) => (high ? Cliff : 0f) + Eye;
}
