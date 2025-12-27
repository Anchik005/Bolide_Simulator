using UnityEngine;

public class EngineController : MonoBehaviour
{
    // Настройки из файла
    [SerializeField] private bool loadFromConfig = false;
    [SerializeField] private KartConfig configuration;

    // Обороты
    [SerializeField] private float minRpm = 500f;
    [SerializeField] private float maxRpm = 6000f;
    [SerializeField] private float limiterRpm = 5000f;

    // Кривая момента
    [SerializeField] private AnimationCurve torqueCharacteristic;

    // Динамика
    [SerializeField] private float inertia = 0.2f;
    [SerializeField] private float throttleSensitivity = 5f;

    // Потери
    [SerializeField] private float frictionFactor = 0.02f;
    [SerializeField] private float loadFactor = 5f;

    // Текущие значения
    public float CurrentRpm { get; private set; }
    public float CurrentTorque { get; private set; }
    public float FilteredThrottle { get; private set; }
    public float LimiterEffect { get; private set; } = 1f;

    private float inertiaFactor;

    private void Awake()
    {
        if (loadFromConfig)
            SetupFromConfig();

        CurrentRpm = minRpm;
        inertiaFactor = 60f / (2f * Mathf.PI * Mathf.Max(inertia, 0.0001f));
    }

    private void SetupFromConfig()
    {
        if (configuration != null)
        {
            torqueCharacteristic = configuration.engineTorqueCurve;
            inertiaFactor = configuration.engineInertia;
            maxRpm = configuration.maxRpm;
        }
    }

    public float Simulate(float throttleInput, float speed, float deltaTime)
    {
        float targetThrottle = Mathf.Clamp01(throttleInput);
        FilteredThrottle = Mathf.MoveTowards(FilteredThrottle, targetThrottle, throttleSensitivity * deltaTime);

        UpdateLimiter();

        float maxTorque = torqueCharacteristic.Evaluate(CurrentRpm);
        float effectiveThrottle = FilteredThrottle * LimiterEffect;

        float driveTorque = maxTorque * effectiveThrottle;
        float frictionTorque = frictionFactor * CurrentRpm;
        float loadTorque = loadFactor * Mathf.Abs(speed);

        float netTorque = driveTorque - frictionTorque - loadTorque;

        float rpmChange = netTorque * inertiaFactor;
        CurrentRpm += rpmChange * deltaTime;

        if (CurrentRpm < minRpm) CurrentRpm = minRpm;
        if (CurrentRpm > maxRpm) CurrentRpm = maxRpm;

        CurrentTorque = driveTorque;
        return CurrentTorque;
    }

    private void UpdateLimiter()
    {
        if (CurrentRpm <= limiterRpm)
        {
            LimiterEffect = 1f;
            return;
        }

        if (CurrentRpm >= maxRpm)
        {
            LimiterEffect = 0f;
            return;
        }

        float t = (CurrentRpm - limiterRpm) / (maxRpm - limiterRpm);
        LimiterEffect = 1f - t;
    }
}