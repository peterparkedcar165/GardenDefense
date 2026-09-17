using System.Linq;
using UnityEditor;
using UnityEngine;

// renders a StatType field as a dropdown restricted to whatever isn't already covered by
// FertilizerStatRules.AllGloballyHandled - keeps PlantData.fertilizerPossibleStats from letting
// someone pick a stat that's already available generically/via element/via family, which would
// just be a confusing no-op
[CustomPropertyDrawer(typeof(RestrictedStatTypeAttribute))]
public class RestrictedStatTypeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.Enum)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        StatType[] allowed = System.Enum.GetValues(typeof(StatType))
            .Cast<StatType>()
            .Where(s => !FertilizerStatRules.AllGloballyHandled.Contains(s))
            .OrderBy(s => s.ToString())
            .ToArray();

        string[] displayNames = allowed.Select(s => s.ToString()).ToArray();

        StatType current = (StatType)property.intValue;
        int currentIndex = System.Array.IndexOf(allowed, current);
        if (currentIndex < 0) currentIndex = 0;

        EditorGUI.BeginProperty(position, label, property);
        int newIndex = EditorGUI.Popup(position, label.text, currentIndex, displayNames);
        if (allowed.Length > 0)
            property.intValue = (int)allowed[Mathf.Clamp(newIndex, 0, allowed.Length - 1)];
        EditorGUI.EndProperty();
    }
}
