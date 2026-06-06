using UnityEngine;

// fast cave insect with high evasion. all tuning (speed, evasion) is set in its InsectData SO
public class Silverfish : Insect
{
    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }
}
