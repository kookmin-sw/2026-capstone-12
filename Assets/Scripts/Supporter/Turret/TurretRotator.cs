using UnityEngine;

public class TurretRotator : GhostDisabledBehaviour
{
    private TurretTargeting targeting;

    public Transform head;
    public float turnSpeed = 25f;
    public float trackTurnSpeed = 180f;

    private void Awake()
    {
        targeting = GetComponent<TurretTargeting>();
    }

    private void Update()
    {
        if (head == null || targeting == null) return;

        Transform target = targeting.CurrentTarget;

        if (target == null)
        {
            head.Rotate(0f, turnSpeed * Time.deltaTime, 0f, Space.Self);
            return;
        }

        Vector3 dir = target.position - head.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        head.rotation = Quaternion.RotateTowards(head.rotation, targetRot, trackTurnSpeed * Time.deltaTime);
    }
}
