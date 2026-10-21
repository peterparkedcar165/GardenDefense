using UnityEngine;

// grants every plant the ability to relocate itself for free, once every cooldownSeconds -
// see PlantUpgradeUI's Transplant button, PlantSelector.transplantMode, Tile.OnMouseDown, and
// Plant.TriggerTransplant for the actual flow
public class TransplantMulchData : MulchData
{
    [Tooltip("seconds between uses, per plant")]
    public float cooldownSeconds = 300f;

    public override MulchEffect CreateEffect() => new TransplantMulchEffect(this);
}
