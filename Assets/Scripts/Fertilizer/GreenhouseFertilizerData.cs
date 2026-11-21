using UnityEngine;

[CreateAssetMenu(fileName = "GreenhouseFertilizer", menuName = "Scriptable Objects/Fertilizer/Greenhouse")]
public class GreenhouseFertilizerData : FertilizerData
{
    public override FertilizerEffect CreateEffect() => new GreenhouseFertilizerEffect(this);

    public override string GetDescription() =>
        "Clear the weather.";
}
