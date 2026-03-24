using UnityEngine;

public class EnemyHitDamage : DamageSource
{
    public override void DealDamage(PlayerDamageManager player)
    {
        player.TakeDamage(damageAmount);
    }

    private void OnCollisionEnter(Collision collision)
    {
        PlayerDamageManager player = collision.gameObject.GetComponent<PlayerDamageManager>();

        if (player != null)
        {
            DealDamage(player);
        }
    }
}