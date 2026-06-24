using UnityEngine;

[CreateAssetMenu(fileName = "PlantRegistry", menuName = "Scriptable Objects/Plant Registry")]
public class PlantRegistry : ScriptableObject
{
    public PlantData[] plants;
}
