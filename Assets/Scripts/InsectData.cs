using UnityEngine;

[CreateAssetMenu(fileName = "InsectData", menuName = "Scriptable Objects/Insect Data")]
public class InsectData : ScriptableObject
{
    [Header("Display")]
    public string displayName;
    public Color nameColor = Color.white;
    public string description;
    public string passiveDescription;
    public Sprite sprite;
    public GameObject insectPrefab;

    [Header("Core Stats")]
    public float baseMaxHealth;
    public float baseAttackDamage;
    public float baseMagicPower;
    public float baseAttackSpeed;
    public float baseAttackRange;
    public float baseMovementSpeed;

    [Header("Offensive")]
    public float baseLifesteal;
    public float basePhysicalShred;
    public float baseMagicShred;

    [Header("Resistances")]
    public float baseTenacity;
    public float basePhysicalResistance;
    public float baseMagicResistance;
    public float baseFireResistance;
    public float baseWaterResistance;
    public float basePoisonResistance;
    public float baseIceResistance;
    public float baseNatureResistance;
    public float baseWindResistance;
    public float baseDotResistance;


    [Header("Other")]
    public int sunDrop;
    public Aggressivity aggressivity;
    public float baseLightEmissionRange;
    public float baseHealingBonus;
    public float baseHealingReceived;
}
