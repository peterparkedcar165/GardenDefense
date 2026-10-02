using UnityEngine;
using TMPro;

public class SunIndicator : TextIndicator
{
    // gap between the end of the text and the start of the icon; tune to taste
    private const float IconGap = 0.2f;

    public static void Spawn(Vector3 position, int amount)
    {
        GameObject go = Object.Instantiate(
            Resources.Load<GameObject>("SunIndicator"), position, Quaternion.identity);
        go.GetComponent<SunIndicator>()?.Initialize(amount);
    }

    // bonus sun gain (e.g. from Aeonium bloom, Sun Mark) now looks identical to regular sun gain
    public static void SpawnBonus(Vector3 position, int amount) => Spawn(position, amount);

    public void Initialize(int amount)
    {
        tmpText.fontStyle = FontStyles.Bold;
        tmpText.color = new Color(1f, 0.95f, 0f);
        tmpText.text  = $"+{amount}";
        PositionIconAfterText(IconGap);
    }
}
