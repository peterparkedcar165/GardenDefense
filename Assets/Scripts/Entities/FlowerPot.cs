using UnityEngine;

public class FlowerPot : MonoBehaviour
{
    private static readonly int[] _sunCosts = { 25, 20, 15, 10 };

    public static int SunCost
    {
        get
        {
            if (SaveManager.instance == null) return 25;
            int level = SaveManager.instance.saveData.flowerPotLevel;
            return _sunCosts[Mathf.Clamp(level - 1, 0, _sunCosts.Length - 1)];
        }
    }

    public TileType originalTileType;
}
