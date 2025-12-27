using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class KartController : MonoBehaviour
{
    // Импорт параметров из конфигурации
    [SerializeField] private bool useConfigFile = false;
    [SerializeField] private KartConfig kartSettings;

    // Точки крепления колес
    [SerializeField] private Transform frontLeft;
    [SerializeField] private Transform frontRight;
    [SerializeField] private Transform rearLeft;
    [SerializeField] private Transform rearRight;

    // Система ввода
    [SerializeField] private InputActionAsset inputActions;

    // Распределение веса
    [SerializeField, Range(0, 1)] private float frontWeightRatio = 0.5f;

    // Двигатель и трансмиссия
    [SerializeField] private EngineController motor;
    [SerializeField] private float gearMultiplier = 8f;
    [SerializeField] private float transmissionEfficiency = 0.9f;

    // Ручной тормоз
    [SerializeField] private KeyCode handbrakeButton = KeyCode.Space;
    [SerializeField] private float handbrakeStrength = 600f;

    // Компоненты
    private InputAction movementAction;
    private Rigidbody body;

    // Входные данные
    private float throttleValue;
    private float steerValue;
    private bool handbrakeActive;

    // Силы на колесах
    private float flLoad;
    private float frLoad;
    private float rlLoad;
    private float rrLoad;

    // Параметры
    private Vector3 gravity = Physics.gravity;
    [SerializeField] private float motorTorque = 400f;
    [SerializeField] private float tireRadius = 0.3f;
    [SerializeField] private float velocityLimit = 20;
    [SerializeField] private float maxTurnAngle = 60f;

    // Параметры шин
    [SerializeField] private float gripCoefficient = 1f;
    [SerializeField] private float sideStiffness = 80f;
    [SerializeField] private float rollingDrag = 0.07f;

    // Вращение колес
    private Quaternion initialFLRot;
    private Quaternion initialFRRot;

    // Расчетные величины
    private float forwardVelocity = 0f;
    private float longitudinalForce = 0f;
    private float lateralForce = 0f;

    private void Start()
    {
        inputActions.Enable();
        body = GetComponent<Rigidbody>();

        var actionMap = inputActions.FindActionMap("Kart");
        movementAction = actionMap.FindAction("Move");

        if (useConfigFile)
            LoadConfiguration();

        initialFLRot = frontLeft.localRotation;
        initialFRRot = frontRight.localRotation;

        CalculateWheelLoads();
    }

    private void LoadConfiguration()
    {
        if (kartSettings != null)
        {
            body.mass = kartSettings.mass;
            gripCoefficient = kartSettings.frictionCoefficient;
            rollingDrag = kartSettings.rollingResistance;
            maxTurnAngle = kartSettings.maxSteerAngle;
            gearMultiplier = kartSettings.gearRatio;
            tireRadius = kartSettings.wheelRadius;
            sideStiffness = kartSettings.lateralStiffness;
        }
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Update()
    {
        ReadInput();
        UpdateWheelVisuals();
    }

    private void ReadInput()
    {
        Vector2 input = movementAction.ReadValue<Vector2>();

        steerValue = Mathf.Clamp(input.x, -1, 1);
        throttleValue = Mathf.Clamp(input.y, -1, 1);

        handbrakeActive = Input.GetKey(handbrakeButton);
    }

    private void UpdateWheelVisuals()
    {
        float angle = maxTurnAngle * steerValue;
        Quaternion rotation = Quaternion.Euler(0, angle, 0);

        frontLeft.localRotation = initialFLRot * rotation;
        frontRight.localRotation = initialFRRot * rotation;
    }

    private void CalculateWheelLoads()
    {
        float weight = body.mass * Mathf.Abs(gravity.y);
        float frontWeight = weight * frontWeightRatio;
        float rearWeight = weight - frontWeight;

        frLoad = frontWeight * 0.5f;
        flLoad = frLoad;

        rrLoad = rearWeight * 0.5f;
        rlLoad = rrLoad;
    }

    private void FixedUpdate()
    {
        ApplyMotorForce();

        ProcessWheel(frontLeft, flLoad, true, false);
        ProcessWheel(frontRight, frLoad, true, false);
        ProcessWheel(rearLeft, rlLoad, false, true);
        ProcessWheel(rearRight, rrLoad, false, true);
    }

    private void ApplyMotorForce()
    {
        Vector3 forward = transform.forward;

        float forwardSpeed = Vector3.Dot(body.linearVelocity, forward);
        if (throttleValue > 0 && forwardSpeed > velocityLimit)
            return;

        float torque = motorTorque * throttleValue;
        float forcePerWheel = torque / tireRadius / 2f;

        Vector3 rearForce = forward * forcePerWheel;
        body.AddForceAtPosition(rearForce, rearLeft.position, ForceMode.Force);
        body.AddForceAtPosition(rearForce, rearRight.position, ForceMode.Force);
    }

    private void ProcessWheel(Transform wheel, float load, bool isSteering, bool isDriving)
    {
        Vector3 position = wheel.position;
        Vector3 forward = wheel.forward;
        Vector3 right = wheel.right;

        Vector3 velocityAtPoint = body.GetPointVelocity(position);

        float forwardVel = Vector3.Dot(velocityAtPoint, forward);
        float sideVel = Vector3.Dot(velocityAtPoint, right);

        longitudinalForce = 0f;
        lateralForce = 0f;

        if (isDriving)
        {
            forwardVelocity = Vector3.Dot(body.linearVelocity, transform.forward);

            float engineOutput = motor.Simulate(throttleValue, forwardVelocity, Time.fixedDeltaTime);
            float totalWheelTorque = engineOutput * gearMultiplier * transmissionEfficiency;

            float wheelTorque = totalWheelTorque * 0.5f;
            longitudinalForce += wheelTorque / tireRadius;

            if (handbrakeActive)
            {
                float brakeDirection = forwardVel > 0 ? -1f : (forwardVel < 0 ? 1f : -1f);
                longitudinalForce += brakeDirection * handbrakeStrength;
            }
        }
        else if (isSteering)
        {
            float rollingDragForce = -rollingDrag * forwardVel;
            longitudinalForce += rollingDragForce;
        }

        float rawSideForce = -sideStiffness * sideVel;
        lateralForce += rawSideForce;

        float maxFriction = gripCoefficient * load;
        float totalForce = Mathf.Sqrt(longitudinalForce * longitudinalForce + lateralForce * lateralForce);

        if (totalForce > maxFriction)
        {
            float reduction = maxFriction / totalForce;
            lateralForce += reduction;
            longitudinalForce += reduction;
        }

        Vector3 finalForce = forward * longitudinalForce + right * lateralForce;
        body.AddForceAtPosition(finalForce, wheel.position, ForceMode.Force);
    }

    // Панель диагностики
    private void OnGUI()
    {
        Rect panel = new Rect(20f, 20f, 500f, 240f);

        GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);
        GUI.Box(panel, "");
        GUI.color = Color.white;

        GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.yellow }
        };

        GUIStyle textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = Color.white }
        };

        float x = panel.x + 15f;
        float y = panel.y + 15f;
        float width = panel.width - 30f;

        GUI.Label(new Rect(x, y, width, 25f), "Телеметрия машины", headerStyle);
        y += 30f;

        GUI.Label(new Rect(x, y, width, 20f), $"Скорость: {forwardVelocity:0.0} м/с ({(forwardVelocity * 3.6f):0.0} км/ч)", textStyle);
        y += 22f;
        GUI.Label(new Rect(x, y, width, 20f), $"Обороты: {motor.CurrentRpm:0}", textStyle);
        y += 22f;
        GUI.Label(new Rect(x, y, width, 20f), $"Крутящий момент: {motor.CurrentTorque:0.0} Н·м", textStyle);
        y += 28f;

        GUI.Label(new Rect(x, y, width, 20f), "Силы на колесах:", textStyle);
        y += 22f;
        GUI.Label(new Rect(x, y, width, 20f), $"Продольная: {longitudinalForce:0.0} Н", textStyle);
        y += 22f;
        GUI.Label(new Rect(x, y, width, 20f), $"Боковая: {lateralForce:0.0} Н", textStyle);

        if (handbrakeActive)
        {
            GUIStyle warningStyle = new GUIStyle(textStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.red }
            };
            GUI.Label(new Rect(x, panel.yMax - 25f, width, 22f), "РУЧНОЙ ТОРМОЗ АКТИВЕН", warningStyle);
        }
    }
}