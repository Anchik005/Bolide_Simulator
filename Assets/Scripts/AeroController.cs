using UnityEngine;

public class AeroController : MonoBehaviour
{
    // Аэродинамическое сопротивление
    [SerializeField] private float airDensity = 1.225f;
    [SerializeField] private float dragCoefficient = 0.9f;
    [SerializeField] private float crossSection = 0.6f;

    // Заднее антикрыло
    [SerializeField] private Transform wingPosition;
    [SerializeField] private float wingSize = 0.4f;
    [SerializeField] private float liftCoefficient = 0.05f;
    [SerializeField] private float attackAngle = 10f;

    // Эффект земли
    [SerializeField] private float groundEffect = 300f;
    [SerializeField] private float groundCheckDistance = 1.0f;

    private Rigidbody body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        ApplyDrag();
        ApplyWingForce();
        ApplyGroundEffect();
    }

    private void ApplyDrag()
    {
        Vector3 velocity = body.linearVelocity;
        float speed = velocity.magnitude;

        if (speed < 0.01f)
            return;

        float drag = 0.5f * airDensity * dragCoefficient * crossSection * speed * speed;
        Vector3 dragForce = -velocity.normalized * drag;

        body.AddForce(dragForce, ForceMode.Force);
    }

    private void ApplyWingForce()
    {
        if (wingPosition == null)
            return;

        float speed = body.linearVelocity.magnitude;
        if (speed < 0.01f)
            return;

        float angleRad = attackAngle * Mathf.Deg2Rad;
        float cl = liftCoefficient * angleRad;

        float downforce = 0.5f * airDensity * cl * wingSize * speed * speed;
        Vector3 downforceVector = -transform.up * downforce;

        body.AddForceAtPosition(downforceVector, wingPosition.position, ForceMode.Force);
    }

    private void ApplyGroundEffect()
    {
        if (!Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, groundCheckDistance))
            return;

        float height = hit.distance;
        if (height < 0.01f)
            height = 0.01f;

        float effectForce = groundEffect / height;
        Vector3 effectVector = -transform.up * effectForce;

        body.AddForce(effectVector, ForceMode.Force);
    }
}