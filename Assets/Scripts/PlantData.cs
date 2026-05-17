using UnityEngine;

public class PlantData : ScriptableObject
{
    public Plant plantPrefab;
    public Sprite icon;
    public string plantName;
    public string displayName;
    public ElementalType elementalType;
    public DamageType damageType;

    public virtual string GetAttackDescription() => "";
    public virtual string GetPassiveDescription() => "";
    public virtual string GetSkillDescription() => "";
}
