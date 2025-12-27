using UnityEngine;

public class SuspensionController : MonoBehaviour
{
    // Точки подвески
    [SerializeField] private Transform suspensionFL;
    [SerializeField] private Transform suspensionFR;
    [SerializeField] private Transform suspensionRL;
    [SerializeField] private Transform suspensionRR;

    // Настройки подвески
    [SerializeField] private float neutralLength = 0.4f;
    [SerializeField] private float suspensionRange = 0.2f;
    [SerializeField] private float springHardness = 2000f;
    [SerializeField] private float dampingCoefficient = 350f;
    [SerializeField] private float tireSize = 0.3f;

    // Стабилизаторы
    [SerializeField] private float frontStabilizer = 800f;
    [SerializeField] private float rearStabilizer = 600f;

    // Диагностика
    [SerializeField] private bool displayInfo = true;

    private Rigidbody physicsBody;

    private float flCompression;
    private float frCompression;
    private float rlCompression;
    private float rrCompression;

    private void Awake()
    {
        physicsBody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        UpdateSuspension(suspensionFL, ref flCompression);
        UpdateSuspension(suspensionFR, ref frCompression);
        UpdateSuspension(suspensionRL, ref rlCompression);
        UpdateSuspension(suspensionRR, ref rrCompression);

        ApplyStabilizerForces();
    }

    private void ApplyStabilizerForces()
    {
        float frontDifference = flCompression - frCompression;
        float frontStabilizerForce = frontDifference * frontStabilizer;

        if (flCompression > -0.0001f)
            physicsBody.AddForceAtPosition(-transform.up * frontStabilizerForce, suspensionFL.position, ForceMode.Force);

        if (frCompression > -0.0001f)
            physicsBody.AddForceAtPosition(transform.up * frontStabilizerForce, suspensionFR.position, ForceMode.Force);

        float rearDifference = rlCompression - rrCompression;
        float rearStabilizerForce = rearDifference * rearStabilizer;

        if (rlCompression > -0.0001f)
            physicsBody.AddForceAtPosition(-transform.up * rearStabilizerForce, suspensionRL.position, ForceMode.Force);

        if (rrCompression > -0.0001f)
            physicsBody.AddForceAtPosition(transform.up * rearStabilizerForce, suspensionRR.position, ForceMode.Force);
    }

    private void UpdateSuspension(Transform mountPoint, ref float compression)
    {
        Vector3 startPoint = mountPoint.position;
        Vector3 direction = -mountPoint.up;

        float maxDistance = neutralLength + suspensionRange + tireSize;
        if (!Physics.Raycast(startPoint, direction, out RaycastHit contact, maxDistance))
            return;

        float currentLength = contact.distance - tireSize;
        currentLength = Mathf.Clamp(currentLength, neutralLength - suspensionRange, neutralLength + suspensionRange);

        float compressionValue = neutralLength - currentLength;
        float springForce = compressionValue * springHardness;

        float compressionSpeed = (compressionValue - compression) / Time.fixedDeltaTime;
        float dampingForce = compressionSpeed * dampingCoefficient;

        compression = compressionValue;

        float totalForce = springForce + dampingForce;
        Vector3 forceVector = mountPoint.up * totalForce;

        physicsBody.AddForceAtPosition(forceVector, mountPoint.position, ForceMode.Force);
    }

    private void OnGUI()
    {
        if (!displayInfo) return;
        if (physicsBody == null) return;

        Rect displayBox = new Rect(20f, 280f, 400f, 160f);

        GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.6f);
        GUI.Box(displayBox, "");
        GUI.color = Color.white;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            normal = { textColor = Color.cyan }
        };

        float x = displayBox.x + 12f;
        float y = displayBox.y + 10f;
        float w = displayBox.width - 24f;

        float speed = physicsBody.linearVelocity.magnitude;
        float speedKmh = speed * 3.6f;

        float frontDiff = flCompression - frCompression;
        float frontStabForce = frontDiff * frontStabilizer;
        float rearDiff = rlCompression - rrCompression;
        float rearStabForce = rearDiff * rearStabilizer;

        float flSpring = flCompression * springHardness;
        float frSpring = frCompression * springHardness;
        float rlSpring = rlCompression * springHardness;
        float rrSpring = rrCompression * springHardness;

        GUI.Label(new Rect(x, y, w, 18f), "Телеметрия подвески", style); y += 20f;

        GUI.Label(new Rect(x, y, w, 18f),
            $"Скорость: {speed:0.0} м/с ({speedKmh:0.0} км/ч)", style); y += 18f;

        GUI.Label(new Rect(x, y, w, 18f),
            $"Стаб. перед: {frontStabForce:0} Н | зад: {rearStabForce:0} Н", style); y += 18f;

        GUI.Label(new Rect(x, y, w, 18f),
            $"ПЛ сжатие: {flCompression:0.000} м | Сила: {flSpring:0} Н", style); y += 18f;

        GUI.Label(new Rect(x, y, w, 18f),
            $"ПП сжатие: {frCompression:0.000} м | Сила: {frSpring:0} Н", style); y += 18f;

        GUI.Label(new Rect(x, y, w, 18f),
            $"ЗЛ сжатие: {rlCompression:0.000} м | Сила: {rlSpring:0} Н", style); y += 18f;

        GUI.Label(new Rect(x, y, w, 18f),
            $"ЗП сжатие: {rrCompression:0.000} м | Сила: {rrSpring:0} Н", style);
    }
}