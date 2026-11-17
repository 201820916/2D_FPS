using UnityEngine;

namespace GungeonFPS.Core
{
    public interface IDamageable
    {
        void TakeDamage(float amount, Vector3 hitDirection, float knockbackForce = 0f);
    }
}
