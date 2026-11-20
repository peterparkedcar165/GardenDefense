using UnityEngine;

[CreateAssetMenu(fileName = "GreenhouseFertilizer", menuName = "Scriptable Objects/Fertilizer/Greenhouse")]
public class GreenhouseFertilizerData : FertilizerData
{
    public override FertilizerEffect CreateEffect() => new GreenhouseFertilizerEffect(this);

    public override string GetDescription() =>
        "The level has <b>no weather</b>, clearing any weather it would normally have.";
}
