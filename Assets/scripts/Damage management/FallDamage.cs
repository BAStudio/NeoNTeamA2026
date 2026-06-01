using UnityEngine;

public class FallDamage : TimedDamageSource
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float rayLength = 1.5f;
    [SerializeField] private float minimumFallTime = 0.5f;

    private bool wasGrounded;

    protected override void TrackTime()
    {
        bool grounded = IsGrounded();

        if (!grounded)
        {
            timer += Time.deltaTime;
        }
        else if (!wasGrounded && timer >= minimumFallTime)
        {
            DealDamage(player);
            timer = 0;
        }
        else if (grounded)
        {
            timer = 0;
        }

        wasGrounded = grounded;
    }

    public override void DealDamage(PlayerDamageManager player)
    {
        float finalDamage = timer * damageAmount;
        player.TakeDamage(finalDamage);
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, rayLength, groundLayer);
    }
}