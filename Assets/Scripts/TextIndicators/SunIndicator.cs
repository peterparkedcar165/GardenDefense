using UnityEngine;
using TMPro;

public class SunIndicator : TextIndicator
{
    // regular sun gain (bright yellow)
    public static void Spawn(Vector3 position, int amount)
    {
        GameObject go = Object.Instantiate(
            Resources.Load<GameObject>("SunIndicator"), position, Quaternion.identity);
        go.GetComponent<SunIndicator>()?.Initialize(amount, false);
    }

    // bonus sun gain (golden, e.g. from Aeonium bloom)
    public static void SpawnBonus(Vector3 position, int amount)
    {
        GameObject go = Object.Instantiate(
            Resources.Load<GameObject>("SunIndicator"), position, Quaternion.identity);
        go.GetComponent<SunIndicator>()?.Initialize(amount, true);
    }

    public void Initialize(int amount, bool isBonus)
    {
        tmpText.fontStyle = FontStyles.Bold;
        if (isBonus)
        {
            tmpText.color = new Color(1f, 0.84f, 0f);
            tmpText.text  = $"+{amount} Sun Bonus";
        }
        else
        {
            tmpText.color = new Color(1f, 0.95f, 0f);
            tmpText.text  = $"+{amount} Sun";
        }
    }
}
