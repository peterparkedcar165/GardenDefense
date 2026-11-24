using UnityEngine;
using TMPro;

public class SunIndicator : TextIndicator
{
    // gap between the end of the text and the start of the icon; tune to taste
    private const float IconGap = 0.2f;

    // same launch speed as DamageIndicator
    protected override float MinVerticalSpeed => 1f;
    protected override float MaxVerticalSpeed => 1.25f;

    // loaded once on first use instead of a resources lookup on every sun gain
    private static GameObject prefab;

    public static void Spawn(Vector3 position, int amount)
    {
        if (prefab == null) prefab = Resources.Load<GameObject>("SunIndicator");
        GameObject go = Object.Instantiate(prefab, position, Quaternion.identity);
        SunIndicator indicator = go.GetComponent<SunIndicator>();
        if (indicator != null) indicator.Initialize(amount);
    }

    // bonus sun gain (e.g. from Aeonium bloom, Sun Mark) now looks identical to regular sun gain
    public static void SpawnBonus(Vector3 position, int amount) => Spawn(position, amount);

    public void Initialize(int amount)
    {
        shrink = false;
        tmpText.fontStyle = FontStyles.Bold;
        tmpText.color = new Color(1f, 0.95f, 0f);
        tmpText.text  = $"+{amount}";
        PositionIconAfterText(IconGap);
    }
}
