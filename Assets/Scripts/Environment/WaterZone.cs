using UnityEngine;

public class WaterZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other) => Check(other);
    private void OnTriggerStay2D(Collider2D other) => Check(other);

    private void Check(Collider2D other)
    {
        Insect insect = other.GetComponentInParent<Insect>();
        if (insect == null || insect.isFlying || insect.HasEffect<Airborne>()) return;

        insect.Kill(insect.lastSource);
    }
}
