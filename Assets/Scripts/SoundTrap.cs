using UnityEngine;

public class SoundTrap : MonoBehaviour
{
    [Header("Timer Reduction")]
    [Tooltip("Минимальное количество секунд, на которое будет сокращен таймер.")]
    public float minReduction = 5f;
    [Tooltip("Максимальное количество секунд, на которое будет сокращен таймер.")]
    public float maxReduction = 8f;

    [Header("Settings")]
    [Tooltip("Будет ли ловушка одноразовой.")]
    public bool isOneTimeUse = true;
    [Tooltip("Время до повторной активации ловушки, если она не одноразовая.")]
    public float cooldownTime = 10f;

    [Tooltip("Ссылка на CreatureController в сцене.")]
    public CreatureController creatureController;

    [Header("Sound Settings")] // Новый заголовок для настроек звука
    [Tooltip("Звуковой клип, который будет воспроизводиться при срабатывании ловушки.")]
    public AudioClip triggerSound;
    [Tooltip("Громкость звука ловушки.")]
    [Range(0f, 1f)]
    public float soundVolume = 1.0f;

    private bool _hasTriggered = false;
    private float _lastTriggerTime;
    private AudioSource _audioSource; // Ссылка на компонент AudioSource

    void Awake()
    {
        // Получаем или добавляем компонент AudioSource
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        // Настраиваем AudioSource (можно сделать это и в инспекторе)
        _audioSource.playOnAwake = false; // Не проигрывать звук при старте
        _audioSource.spatialBlend = 1.0f; // 3D-звук
        _audioSource.volume = soundVolume; // Устанавливаем громкость
    }

    void Start()
    {
        if (creatureController == null)
        {
            creatureController = FindAnyObjectByType<CreatureController>();
            if (creatureController == null)
            {
                Debug.LogError("CreatureController not found in scene for SoundTrap!", this);
                enabled = false;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (creatureController == null) return;

        if (other.CompareTag("Player"))
        {
            if (isOneTimeUse && _hasTriggered) return;
            if (!isOneTimeUse && Time.time < _lastTriggerTime + cooldownTime) return;

            float reductionAmount = Random.Range(minReduction, maxReduction);
            creatureController.ReduceTimer(reductionAmount);

            Debug.Log($"Sound trap triggered by Player! Monster timer reduced by {reductionAmount:F1}s.");

            // Воспроизводим звук
            if (triggerSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(triggerSound, soundVolume); // Используем PlayOneShot для однократного проигрывания
            }

            _hasTriggered = true;
            _lastTriggerTime = Time.time;

            if (isOneTimeUse)
            {
                // Можно добавить небольшую задержку перед деактивацией, чтобы звук успел проиграться
                // Например, Invoke("DeactivateTrap", triggerSound.length);
                gameObject.SetActive(false); // Отключаем ловушку после использования
            }
        }
    }

    // Метод для деактивации ловушки после проигрывания звука, если isOneTimeUse
    void DeactivateTrap()
    {
        gameObject.SetActive(false);
    }


    void OnDrawGizmos()
    {
        // Визуализация триггера для удобства в редакторе
        Collider col = GetComponent<Collider>();
        if (col != null && col.isTrigger)
        {
            Gizmos.color = Color.yellow;
            if (col is BoxCollider box)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.TransformPoint(box.center), transform.rotation, transform.lossyScale);
                Gizmos.DrawWireCube(Vector3.zero, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Vector3 center = transform.TransformPoint(sphere.center);
                float radius = sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
                Gizmos.DrawWireSphere(center, radius);
            }
        }
    }
}