using UnityEngine;

public class GameButtons : MonoBehaviour
{
    public void TogglePause()  => GameManager.instance?.TogglePause();
    public void ToggleSpeed()  => GameManager.instance?.ToggleSpeed();

}
