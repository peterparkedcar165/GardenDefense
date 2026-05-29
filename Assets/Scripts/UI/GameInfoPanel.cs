using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

// lives inside the settings panel; refreshes when the panel opens (OnEnable).
// shows active weather, temperature, and fertilizer during a match, and hides
// every section outside of a level (e.g. the main menu settings).
// NOTE: never disables its own GameObject, or OnEnable could never fire again
public class GameInfoPanel : MonoBehaviour
{
    [Header("Section wrappers (title + text)")]
    [SerializeField] private GameObject weatherSection;
    [SerializeField] private GameObject temperatureSection;
    [SerializeField] private GameObject fertilizerSection;

    [Header("Description texts")]
    [SerializeField] private TMP_Text weatherText;
    [SerializeField] private TMP_Text temperatureText;
    [SerializeField] private TMP_Text fertilizerText;

    [SerializeField] private Image background; // optional; hidden outside a level

    private void Awake()
    {
        // fall back to the Image on this object so the box hides outside a level
        // without needing to be wired explicitly
        if (background == null) background = GetComponent<Image>();
    }

    private void OnEnable()
    {
        bool inLevel = GameManager.instance != null && GameManager.instance.IsGameActive;

        if (background != null) background.enabled = inLevel;
        SetActive(weatherSection, inLevel);
        SetActive(temperatureSection, inLevel);
        SetActive(fertilizerSection, inLevel);

        if (inLevel) Refresh();
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    private void Refresh()
    {
        if (weatherText != null)     weatherText.text     = BuildWeather();
        if (temperatureText != null) temperatureText.text = BuildTemperature();
        if (fertilizerText != null)  fertilizerText.text  = BuildFertilizer();
    }

    private string BuildWeather()
    {
        if (WeatherManager.instance == null) return "None.";

        var sb = new StringBuilder();
        bool any = false;
        foreach (WeatherEntry entry in WeatherManager.instance.GetActiveWeather())
        {
            any = true;
            sb.AppendLine($"<b>{WeatherManager.GetWeatherName(entry.type)}</b> <color=#FFD700>(Lv {entry.intensity})</color>");
            sb.AppendLine($"<size=85%>{WeatherManager.GetWeatherDescription(entry.type, entry.intensity)}</size>");
        }
        if (!any) return "Clear. No active weather.";

        return sb.ToString().TrimEnd();
    }

    private string BuildTemperature()
    {
        if (WeatherManager.instance == null) return "Normal.";

        TemperatureType temp = WeatherManager.instance.temperature;
        return $"<b>{WeatherManager.GetTemperatureName(temp)}</b>\n" +
               $"<size=85%>{WeatherManager.GetTemperatureDescription(temp)}</size>";
    }

    private string BuildFertilizer()
    {
        string fertilizer = FertilizerManager.instance != null
            ? FertilizerManager.instance.GetActiveSummary()
            : null;
        return string.IsNullOrEmpty(fertilizer) ? "No fertilizer selected." : fertilizer;
    }
}
