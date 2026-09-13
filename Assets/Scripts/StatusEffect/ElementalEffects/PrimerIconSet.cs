using UnityEngine;

// one sprite per element, assigned in the Inspector (the sprites themselves can live anywhere -
// only this asset itself needs to sit in a Resources folder so it can be loaded by name)
[CreateAssetMenu(fileName = "PrimerIconSet", menuName = "Scriptable Objects/Primer Icon Set")]
public class PrimerIconSet : ScriptableObject
{
    public Sprite fire, water, grass, poison, ice, wind;

    [Header("Layout")]
    public float iconScale = 0.35f;
    public float iconHeight = 0.9f;

    public Sprite Get(ElementalType type) => type switch
    {
        ElementalType.Fire   => fire,
        ElementalType.Water  => water,
        ElementalType.Grass  => grass,
        ElementalType.Poison => poison,
        ElementalType.Ice    => ice,
        ElementalType.Wind   => wind,
        _ => null
    };
}
