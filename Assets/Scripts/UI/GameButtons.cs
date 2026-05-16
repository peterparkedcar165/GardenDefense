using UnityEngine;

public class GameButtons : MonoBehaviour
{
    public void TogglePause()  => GameManager.instance?.TogglePause();
    public void ToggleSpeed()  => GameManager.instance?.ToggleSpeed();

    public void ToggleUproot()
    {
        if (PlantSelector.instance == null) return;
        PlantSelector.instance.SetUprootMode(!PlantSelector.instance.uprootMode);
    }
}
