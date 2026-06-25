using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public int highestLevelUnlocked = 0;
    public List<string> unlockedPlants = new List<string>();
    public int currency = 0;

    // 0 = 4 slots, 1 = 5, 2 = 6, 3 = 7, 4 = 8
    public int plantSlotLevel = 0;

    // 0 = locked, 1 = unlocked (25 sun), 2 = 20 sun, 3 = 15 sun, 4 = 10 sun
    public int flowerPotLevel = 0;
    public int waterPotLevel  = 0;

    public int MaxLoadoutSize => 4 + plantSlotLevel;
}
