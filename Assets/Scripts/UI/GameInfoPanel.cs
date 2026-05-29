using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

// lives inside the settings panel; refreshes when the panel opens (OnEnable).
// shows active weather, temperature, and fertilizer during a match, and stays
// blank outside of a level (e.g. the main menu settings).
// NOTE: never disables its own GameObject, or OnEnable could never fire again
public class GameInfoPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text infoText;
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
        if (infoText != null) infoText.gameObject.SetActive(inLevel);
        if (inLevel) Refresh();
    }

    private void Refresh()
    {
        if (infoText == null) return;

        var sb = new StringBuilder();

        // weather
        sb.AppendLine("<size=120%><b>Weather</b></size>");
        if (WeatherManager.instance == null)
        {
            sb.AppendLine("None.");
        }
        else
        {
            bool any = false;
            foreach (WeatherEntry entry in WeatherManager.instance.GetActiveWeather())
            {
                any = true;
                sb.AppendLine($"<b>{WeatherManager.GetWeatherName(entry.type)}</b> <color=#FFD700>(Lv {entry.intensity})</color>");
                sb.AppendLine($"<size=85%>{WeatherManager.GetWeatherDescription(entry.type, entry.intensity)}</size>");
            }
            if (!any) sb.AppendLine("Clear. No active weather.");
        }

        // temperature
        sb.AppendLine();
        sb.AppendLine("<size=120%><b>Temperature</b></size>");
        if (WeatherManager.instance != null)
        {
            TemperatureType temp = WeatherManager.instance.temperature;
            sb.AppendLine($"<b>{WeatherManager.GetTemperatureName(temp)}</b>");
            sb.AppendLine($"<size=85%>{WeatherManager.GetTemperatureDescription(temp)}</size>");
        }
        else
        {
            sb.AppendLine("Normal.");
        }

        // fertilizer
        sb.AppendLine();
        sb.AppendLine("<size=120%><b>Fertilizer</b></size>");
        string fertilizer = FertilizerManager.instance != null
            ? FertilizerManager.instance.GetActiveSummary()
            : null;
        sb.Append(string.IsNullOrEmpty(fertilizer) ? "No fertilizer selected." : fertilizer);

        infoText.text = sb.ToString();
    }
}
