using UnityEngine;

public abstract class TimedDamageSource : DamageSource
{
    protected float timer;
    protected PlayerDamageManager player;

    protected virtual void Awake()
    {
        player = GetComponent<PlayerDamageManager>();
    }

    protected virtual void Update()
    {
        TrackTime();
    }

    protected abstract void TrackTime();
}