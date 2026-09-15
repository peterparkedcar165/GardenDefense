using UnityEngine;
using UnityEngine.UI;

// optional helper: stretches this object's own Image between two RectTransforms so it reads as
// a connecting line from "from" to "to". add this to a plain UI Image (thin, full-width sprite)
// under the same panel, assign the two node RectTransforms, and it lays itself out once on
// enable. purely cosmetic - draw the lines by hand instead if that is easier for a given layout
[RequireComponent(typeof(RectTransform))]
public class SkillTreeLineConnector : MonoBehaviour
{
    [SerializeField] private RectTransform from;
    [SerializeField] private RectTransform to;
    [SerializeField] private float thickness = 4f;

    private void OnEnable() => Layout();

    public void Layout()
    {
        if (from == null || to == null) return;
        RectTransform rt = (RectTransform)transform;

        Vector3 a = from.position;
        Vector3 b = to.position;
        Vector3 mid = (a + b) * 0.5f;
        float distance = Vector3.Distance(a, b);
        float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;

        rt.position = mid;
        rt.sizeDelta = new Vector2(distance, thickness);
        rt.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
