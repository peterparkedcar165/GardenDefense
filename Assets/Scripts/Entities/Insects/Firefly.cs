public class Firefly : FlyingInsect
{
    private UnityEngine.Rendering.Universal.Light2D _light2D;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    public override void UpdateStats()
    {
        base.UpdateStats();

        if (lightEmissionRange > 0f && _light2D == null && visual != null)
        {
            _light2D = visual.gameObject.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            _light2D.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            _light2D.intensity = 0.5f;
            _light2D.falloffIntensity = 0.5f;
        }

        if (_light2D != null)
        {
            _light2D.pointLightOuterRadius = lightEmissionRange;
            _light2D.pointLightInnerRadius = lightEmissionRange * 0.3f;
        }
    }
}
