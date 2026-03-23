using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    [SerializeField] private Mantling mantling;
    [SerializeField] private WallClimbing wallClimbing;
    [SerializeField] private float shakeDuration = 1f;
    [SerializeField] private float positionShakeAmount = 0.05f;
    [SerializeField] private float rotationShakeAmount = 2f;
    [SerializeField] private float shakeSpeed = 0.05f;
    [SerializeField] private float smooth = 20f;
    private Vector3 basePos, curPos, targetPos;
    private Quaternion baseRot;
    private float curRoll, targetRoll;
    private Coroutine shakeCo;

    void Start()
    {
        basePos = transform.localPosition;
        baseRot = transform.localRotation;
        if (mantling) mantling.OnStartMantling += () => StartShake(false);
        if (wallClimbing) wallClimbing.OnWallClimb += StartShake;
    }

    void Update()
    {
        curPos = Vector3.Lerp(curPos, targetPos, Time.deltaTime * smooth);
        curRoll = Mathf.Lerp(curRoll, targetRoll, Time.deltaTime * smooth);
        transform.localPosition = basePos + curPos;
        transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, curRoll);
    }

    private void StartShake(bool loop)
    {
        if (!loop)
        {
            if (shakeCo != null) StopCoroutine(shakeCo);
            shakeCo = StartCoroutine(Shake(shakeDuration));
            return;
        }

        if (shakeCo == null) shakeCo = StartCoroutine(Shake(-1f));
    }

    private IEnumerator Shake(float duration)
    {
        float t = 0f;
        while (duration < 0f || t < duration)
        {
            targetPos = new Vector3(Random.Range(-1f, 1f) * positionShakeAmount, Random.Range(-1f, 1f) * positionShakeAmount, 0f);
            targetRoll = Random.Range(-1f, 1f) * rotationShakeAmount;
            yield return new WaitForSeconds(shakeSpeed);
            t += shakeSpeed;
        }

        targetPos = Vector3.zero;
        targetRoll = 0f;
        shakeCo = null;
    }
}