using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    [Header("Door Settings")]
    public Transform leftDoor;
    public Transform rightDoor;
    public float slideDistance = 1f;
    public float slideSpeed = 2f;

    [Header("Audio Settings")]
    public AudioClip openSound; // Звук открытия двери
    public AudioClip closeSound; // Звук закрытия двери
    [Range(0f, 1f)]
    public float volume = 1f; // Громкость звуков
    private AudioSource audioSource; // Компонент для воспроизведения звуков


    [Header("Trigger Settings")]
    public TriggerType triggerType = TriggerType.Player;
    public LayerMask playerLayer = 1 << 3; // Layer 3 - Player

    [Header("AI Interaction")]
    public float soundReductionWhenClosed = 0.7f;
    public bool blocksVisionWhenClosed = true;
    public bool isClosed = true;

    protected Vector3 leftDoorClosedPos;
    protected Vector3 rightDoorClosedPos;
    protected Vector3 leftDoorOpenPos;
    protected Vector3 rightDoorOpenPos;

    protected bool isExternalyForcedOpen = false; // Новый флаг для внешнего управления
    protected bool isExternalyControlled = false; // Флаг для указания, что открытие управляется внешним скриптом 

    private bool isOpening = false;
    private bool wasOpening = false; // Дополнительная переменная для отслеживания изменения состояния
    private Collider doorCollider;

    public enum TriggerType
    {
        Player,
        AnyObject,
        MonsterOnly,
        PlayerAndMonster
    }
    public void SetOpenCondition(bool shouldOpen, bool externalControl = true)
    {
        isExternalyControlled = externalControl;
        isExternalyForcedOpen = shouldOpen;

        if (isExternalyForcedOpen)
        {
            OpenDoor();
        }
        else if (isOpening && !isExternalyForcedOpen)
        {
            CloseDoor();
        }
    }
    protected virtual void Start()
    {
        // Сохраняем начальные позиции
        leftDoorClosedPos = leftDoor.position;
        rightDoorClosedPos = rightDoor.position;
        Vector3 localSlideDirection = transform.right;


        // Рассчитываем позиции когда дверь открыта
        leftDoorOpenPos = leftDoorClosedPos - localSlideDirection * slideDistance;
        rightDoorOpenPos = rightDoorClosedPos + localSlideDirection * slideDistance;

        // Получаем коллайдер для проверки пересечений
        doorCollider = GetComponent<Collider>();

        // ИНИЦИАЛИЗАЦИЯ AUDIO SOURCE
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) // Если AudioSource нет, добавляем его
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.volume = volume;
        audioSource.playOnAwake = false; // Не проигрывать звук при старте
    }

    protected virtual void Update()
    {
        // Плавное движение дверей
        if (isOpening)
        {
            leftDoor.position = Vector3.MoveTowards(leftDoor.position, leftDoorOpenPos, slideSpeed * Time.deltaTime);
            rightDoor.position = Vector3.MoveTowards(rightDoor.position, rightDoorOpenPos, slideSpeed * Time.deltaTime);

            if (Vector3.Distance(leftDoor.position, leftDoorOpenPos) < 0.01f)
            {
                // Дверь полностью открыта
                isClosed = false;
                if (wasOpening) // Проверяем, изменилось ли состояние с "открывается" на "открыто"
                {
                    wasOpening = false; // Сбрасываем флаг
                }
            }
        }
        else // Дверь закрывается
        {
            leftDoor.position = Vector3.MoveTowards(leftDoor.position, leftDoorClosedPos, slideSpeed * Time.deltaTime);
            rightDoor.position = Vector3.MoveTowards(rightDoor.position, rightDoorClosedPos, slideSpeed * Time.deltaTime);

            if (Vector3.Distance(leftDoor.position, leftDoorClosedPos) < 0.01f)
            {
                // Дверь полностью закрыта
                isClosed = true;
                if (!wasOpening) // Проверяем, изменилось ли состояние с "закрывается" на "закрыто"
                {
                    wasOpening = true; // Сбрасываем флаг
                }
            }
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        // Если дверь контролируется внешним скриптом, игнорируем триггер
        if (isExternalyControlled) return;

        if (ShouldOpen(other))
        {
            if (!isOpening) // Проверяем, если дверь уже не открывается
            {
                isOpening = true;
                PlaySound(openSound); // Воспроизводим звук открытия
            }
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        // Если дверь контролируется внешним скриптом, игнорируем триггер
        if (isExternalyControlled) return;

        if (ShouldClose(other))
        {

            if (isOpening) // Если дверь была в состоянии открытия
            {
                isOpening = false;
                PlaySound(closeSound); // Воспроизводим звук закрытия
            }
        }
    }

    private bool ShouldOpen(Collider other)
    {
        switch (triggerType)
        {
            case TriggerType.Player:
                return ((1 << other.gameObject.layer) & playerLayer) != 0;
            case TriggerType.AnyObject:
                return !other.isTrigger;
            case TriggerType.MonsterOnly:
                return other.CompareTag("Monster");
            case TriggerType.PlayerAndMonster:
                return ((1 << other.gameObject.layer) & playerLayer) != 0 || other.CompareTag("Monster");
            default:
                return false;
        }
    }

    private bool ShouldClose(Collider other)
    {
        // Для MonsterOnly не закрываем автоматически, чтобы монстр мог спокойно пройти
        if (triggerType == TriggerType.MonsterOnly)
            return false;

        return ShouldOpen(other);
    }

    public bool IsPointBehindDoor(Vector3 fromPosition, Vector3 targetPosition)
    {
        if (!isClosed || doorCollider == null) return false;

        Ray ray = new Ray(fromPosition, (targetPosition - fromPosition).normalized);
        float distance = Vector3.Distance(fromPosition, targetPosition);

        return doorCollider.Raycast(ray, out RaycastHit hit, distance);
    }

    public float GetSoundReduction()
    {
        return isClosed ? soundReductionWhenClosed : 0f;
    }

    public bool BlocksVision()
    {
        return isClosed && blocksVisionWhenClosed;
    }

    // Методы для ручного управления дверью
    public void OpenDoor()
    {
        if (!isOpening) // Если дверь уже не открывается
        {
            isOpening = true;
            PlaySound(openSound);
        }
    }

    public void CloseDoor()
    {
        if (isOpening) // Если дверь открыта или в процессе открытия
        {
            isOpening = false;
            PlaySound(closeSound);
        }
    }

    protected void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    void OnDrawGizmosSelected()
    {
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            Gizmos.color = isClosed ? new Color(1, 0, 0, 0.3f) : new Color(0, 1, 0, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;

            if (collider is BoxCollider boxCollider)
            {
                Gizmos.DrawCube(boxCollider.center, boxCollider.size);
            }
        }
    }
}