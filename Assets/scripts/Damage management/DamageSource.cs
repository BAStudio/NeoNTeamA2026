using UnityEngine;

public abstract class DamageSource : MonoBehaviour
{
    [SerializeField] protected float damageAmount;

    public float DamageAmount => damageAmount;

    public abstract void DealDamage(PlayerDamageManager player);
}