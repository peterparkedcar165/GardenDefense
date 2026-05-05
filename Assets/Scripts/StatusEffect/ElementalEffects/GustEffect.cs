using UnityEngine;

public class GustEffect : ElementalDebuff
{
    public GustEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        
    }

    public override void OnApply()
    {
        Debug.Log("Gust inflicted");
    }

    public override void OnTick(float deltaTime)
    {
        
    }

    public override void OnExpire()
    {
        Debug.Log("Gust removed");
    }
}
