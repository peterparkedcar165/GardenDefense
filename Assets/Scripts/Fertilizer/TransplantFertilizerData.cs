using UnityEngine;

// every plant can relocate itself for free, once every cooldownSeconds. see PlantUpgradeUI's
// Transplant button, PlantSelector.transplantMode, Tile.OnMouseDown and Plant.TriggerTransplant
[CreateAssetMenu(fileName = "TransplantFertilizer", menuName = "Scriptable Objects/Fertilizer/Transplant")]
public class TransplantFertilizerData : FertilizerData
{
    [Tooltip("seconds between uses, per plant")]
    public float cooldownSeconds = 60f;

    public override FertilizerEffect CreateEffect() => new TransplantFertilizerEffect(this);

    public override string GetDescription() =>
        $"Gain the ability to relocate a plant for free. Cooldown <b><color=green>{cooldownSeconds:F0}</color></b> seconds per plant.";
}
