using UnityEditor;
using UnityEngine;

// adds a live "Current Weather" readout plus a dropdown + Set/Remove/Clear buttons directly on
// WeatherManager's own inspector, so weather can be previewed in Play mode without needing a
// mulch or skill to actually trigger it
[CustomEditor(typeof(WeatherManager))]
public class WeatherManagerEditor : Editor
{
    private GUIStyle richLabel;

    private WeatherType _previewWeather = WeatherType.Rain;
    private int _previewIntensity = 1;

    // keeps the "Current Weather" readout below live while playing - subscribing to
    // EditorApplication.update (and unsubscribing in OnDisable) is the well-behaved way to do
    // this; calling Repaint() unconditionally from inside OnInspectorGUI itself churns Editor
    // GUI/preview caching enough to surface "ObjectPreview not disposed properly" warnings
    private void OnEnable()
    {
        EditorApplication.update += Repaint;
        // GUIStyle setters like richText can't run from a static field initializer/constructor -
        // Unity requires this kind of setup to happen in OnEnable instead
        richLabel = new GUIStyle(EditorStyles.label) { richText = true };
    }

    private void OnDisable() => EditorApplication.update -= Repaint;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WeatherManager manager = (WeatherManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Current Weather", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play mode to see and change the active weather.", MessageType.Info);
            return;
        }

        bool any = false;
        foreach (WeatherEntry entry in manager.GetActiveWeather())
        {
            any = true;
            EditorGUILayout.LabelField($"{WeatherManager.GetWeatherName(entry.type)} (intensity {entry.intensity})", richLabel);
        }
        if (!any) EditorGUILayout.LabelField("Clear - no weather active");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview Weather", EditorStyles.boldLabel);
        _previewWeather = (WeatherType)EditorGUILayout.EnumPopup("Weather", _previewWeather);
        _previewIntensity = EditorGUILayout.IntSlider("Intensity", _previewIntensity, 1, 5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Set")) manager.SetWeather(_previewWeather, _previewIntensity);
        if (GUILayout.Button("Remove")) manager.RemoveWeather(_previewWeather);
        if (GUILayout.Button("Clear All")) manager.ClearAllWeather();
        EditorGUILayout.EndHorizontal();
    }
}
