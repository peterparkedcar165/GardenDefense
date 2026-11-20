// clears the level's base weather once, on pick (before wave 1). weather in a level is set once at
// the start and never changes by itself, so clearing it once is enough
public class GreenhouseFertilizerEffect : FertilizerEffect
{
    public GreenhouseFertilizerEffect(GreenhouseFertilizerData data) : base(data) {}

    public override void Activate() => WeatherManager.instance?.ClearAllWeather();
    public override void Deactivate() {}
}
