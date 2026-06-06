using UnityEngine;

public interface IAttackable
{
    bool ReceiveAttack(float damage, Insect attacker);
    bool IsAlive { get; }
    Vector3 Position { get; }
    Vector3 GetApproachPoint(Vector3 from);
}
