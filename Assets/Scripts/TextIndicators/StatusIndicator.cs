using UnityEngine;
using TMPro;

public class StatusIndicator : TextIndicator
{
    public static void Spawn(Vector3 position, string text, Color color)
    {
        GameObject go = Object.Instantiate(
            Resources.Load<GameObject>("StatusIndicator"), position, Quaternion.identity);
        go.GetComponent<StatusIndicator>()?.Initialize(text, color);
    }

    public void Initialize(string text, Color color)
    {
        tmpText.color     = color;
        tmpText.fontStyle = FontStyles.Bold | FontStyles.Italic;
        tmpText.fontSize += 1f;
        tmpText.text      = text;
    }
}
