using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Zinnia's skill: lives on Zinnia herself for the skill's duration. owns the light visual and
// grants every plant within range Sunlight Exposure — including plants placed after the cast,
// while the sun is still up. the granted exposure isn't independently timed: it lasts exactly
// as long as this effect does, and is explicitly pulled the moment this one ends (naturally or
// because Zinnia died), rather than expiring on its own separate clock.
//
// it also radiates real heat: while the weather is Hot or Cold (never otherwise), every plant
// inside warms up by heatingPerSecond — helpful against Cold, but a genuine overheat risk if the
// weather is already Hot. at Zinnia's Path3 max, Fire plants inside also get an extra Passive
// (Path2) level on top of whatever the normal Sunlight Exposure weather bonus already grants.
public class ArtificialSunEffect : StatusEffect
{
    private readonly float radius;
    private readonly int sunIntensity;
    private readonly float lightIntensity;
    private readonly float heatingPerSecond;
    private readonly bool grantsExtraFirePassiveLevel;
    private GameObject _lightObj;
    private readonly List<Plant> _affectedPlants = new List<Plant>();
    private readonly List<Plant> _fireBonusPlants = new List<Plant>();

    public ArtificialSunEffect(Entity target, float duration, Entity source, float radius, int sunIntensity,
                               float lightIntensity, float heatingPerSecond, bool grantsExtraFirePassiveLevel)
        : base(target, duration, 1, source)
    {
        this.radius                     = radius;
        this.sunIntensity               = sunIntensity;
        this.lightIntensity             = lightIntensity;
        this.heatingPerSecond           = heatingPerSecond;
        this.grantsExtraFirePassiveLevel = grantsExtraFirePassiveLevel;
        effectType    = Type.positive;
        elementalType = ElementalType.Fire;
    }

    public override string GetName() => "<color=orange><b>Artificial Sun</b></color>";
    public override string GetDescription() =>
        $"Is conjuring an <color=orange><b>Artificial Sun</b></color>";

    public override void OnApply()
    {
        SpawnLight();
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive) continue;
            if (Vector3.Distance(target.transform.position, plant.transform.position) > radius) continue;
            GrantSunlight(plant);
        }
        Plant.OnPlantPlaced += HandlePlantPlaced;
    }

    // catches plants placed after the cast, while the sun is still up
    private void HandlePlantPlaced(Plant plant)
    {
        if (plant == null || !plant.IsAlive || target == null) return;
        if (Vector3.Distance(target.transform.position, plant.transform.position) > radius) return;
        GrantSunlight(plant);
    }

    private void GrantSunlight(Plant plant)
    {
        SunlightExposedEffect exposure = new SunlightExposedEffect(plant, source, sunIntensity);
        exposure.duration = float.MaxValue; // pulled explicitly in Cleanup, not timed on its own
        plant.ApplyEffect(exposure);
        if (!_affectedPlants.Contains(plant)) _affectedPlants.Add(plant);

        if (grantsExtraFirePassiveLevel && plant.elementalType == ElementalType.Fire && !_fireBonusPlants.Contains(plant))
        {
            plant.path2LevelAdder += 1;
            _fireBonusPlants.Add(plant);
        }
    }

    // OnExpire covers the natural end of duration; OnTargetDied covers Zinnia dying mid-duration
    // (Kill() never calls OnExpire on its own active effects, only OnTargetDied)
    public override void OnExpire() => Cleanup();
    public override void OnTargetDied() => Cleanup();

    private void Cleanup()
    {
        Plant.OnPlantPlaced -= HandlePlantPlaced;
        if (_lightObj != null)
        {
            DarknessManager.UnregisterLightSource(_lightObj.transform);
            LightFader fader = _lightObj.GetComponent<LightFader>();
            if (fader != null) fader.FadeOut(1f, destroyOnComplete: true);
            else Object.Destroy(_lightObj);
        }

        foreach (Plant plant in _affectedPlants)
        {
            if (plant == null || !plant.IsAlive) continue;
            SunlightExposedEffect existing = plant.GetEffect<SunlightExposedEffect>();
            if (existing != null && existing.source == source)
                plant.RemoveEffect<SunlightExposedEffect>();
        }
        _affectedPlants.Clear();

        foreach (Plant plant in _fireBonusPlants)
        {
            if (plant == null || !plant.IsAlive) continue;
            plant.path2LevelAdder -= 1;
        }
        _fireBonusPlants.Clear();
    }

    public override void OnTick(float deltaTime)
    {
        // keep the light in sync if darkness toggles mid-duration, same as Fire Wave's own light
        if (_lightObj != null && DarknessManager.instance != null)
        {
            Light2D light = _lightObj.GetComponent<Light2D>();
            if (light != null) light.enabled = DarknessManager.instance.isDark;
        }

        if (heatingPerSecond <= 0f) return;
        if (WeatherManager.instance == null) return;
        TemperatureType temp = WeatherManager.instance.temperature;
        if (temp != TemperatureType.Hot && temp != TemperatureType.Cold) return;

        // Cold: only warms back up to comfort (same cap real Sunny-during-Cold recovery uses),
        // never past it. Hot: no such cap — this is the one that can genuinely overheat a plant
        foreach (Plant plant in _affectedPlants)
        {
            if (plant == null || !plant.IsAlive) continue;
            float cap = temp == TemperatureType.Hot ? plant.temperatureMax : plant.comfortMax;
            plant.temperature = Mathf.Min(plant.temperature + heatingPerSecond * deltaTime, cap);
        }
    }

    // only lit in dark biomes, like Burn/Glowshroom/Fire Wave - a map with no darkness support
    // has no need for a light source at all
    private void SpawnLight()
    {
        if (DarknessManager.instance == null) return;

        _lightObj = new GameObject("ArtificialSun");
        _lightObj.transform.SetParent(target.transform);
        _lightObj.transform.localPosition = Vector3.zero;

        Light2D light = _lightObj.AddComponent<Light2D>();
        light.lightType             = Light2D.LightType.Point;
        light.color                 = Color.white;
        light.falloffIntensity      = 0.2f;
        light.pointLightOuterRadius = radius;
        light.pointLightInnerRadius = radius * 0.3f;
        light.targetSortingLayers   = GetAllSortingLayerIDs();
        light.enabled               = DarknessManager.instance.isDark;   // only shine while it is actually dark

        // same fade in/out logic Plant.cs uses for every illuminated plant (Calendula, Floral Glow)
        LightFader fader = _lightObj.AddComponent<LightFader>();
        fader.Setup(light, lightIntensity);
        fader.FadeIn(1f);

        DarknessManager.RegisterLightSource(_lightObj.transform, radius);
    }

    private int[] GetAllSortingLayerIDs()
    {
        var layers = SortingLayer.layers;
        int[] ids = new int[layers.Length];
        for (int i = 0; i < layers.Length; i++)
            ids[i] = layers[i].id;
        return ids;
    }
}
