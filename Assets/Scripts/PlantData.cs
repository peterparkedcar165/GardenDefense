using UnityEngine;

[CreateAssetMenu(fileName = "PlantData", menuName = "Scriptable Objects/PlantData")]
public class PlantData : ScriptableObject
{
    public Plant plantPrefab;
    public Sprite icon;
    public string plantName;
    public string displayName;
}
