using UnityEngine;
using UnityEngine.AI;

public class MovableObstacle : MonoBehaviour
{
    [Header("Obstacle Settings")]
    [Tooltip("Максимальное расстояние, с которого можно толкать препятствие.")]
    public float interactionRange = 3f;
    [Tooltip("Время (в секундах), которое нужно держать кнопку 'Толкать' для сдвига.")]
    public float requiredPushTime = 3.0f;
    [Tooltip("Скорость перемещения препятствия после сдвига.")]
    public float moveSpeed = 1.0f;
    [Tooltip("Позиция, в которую переместится препятствие после сдвига.")]
    public Transform targetMovePosition;
    [Tooltip("Целевое вращение, которое примет объект после сдвига.")]
    public Quaternion targetMoveRotation = Quaternion.identity; // Новое поле для целевого вращения
    [Tooltip("Скорость поворота препятствия после сдвига.")]
    public float rotationSpeed = 1.0f; // Новое поле для скорости поворота
    [Tooltip("Максимальный угол между взглядом игрока и объектом для начала толкания.")]
    public float playerPushAngleThreshold = 45f;


    [Header("References")]
    public Collider blockingCollider;
    public TimerReductionConfig timerReductionConfig;
    public CreatureController creatureController;
    public CartController cartController;
    public PlayerInputHandler playerInputHandler;
    public PlayerController playerController;

    private float _currentPushProgress = 0f;
    private bool _isMoved = false;
    public bool IsMoved => _isMoved;
    private NavMeshObstacle _navMeshObstacle;
    private bool _timerReduced = false; // Флаг, который показывает, сокращен ли уже таймер.
    private bool _isBlocking = true; // Флаг, показывающий, блокирует ли еще объект NavMesh.

    void Start()
    {
        // Поиск необходимых ссылок, если они не назначены в Инспекторе
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
        if (playerInputHandler == null) playerInputHandler = FindAnyObjectByType<PlayerInputHandler>();
        if (creatureController == null) creatureController = FindAnyObjectByType<CreatureController>();
        if (cartController == null)
        {
            cartController = FindAnyObjectByType<CartController>();

        }
        if (blockingCollider == null) blockingCollider = GetComponent<Collider>();
        _navMeshObstacle = GetComponent<NavMeshObstacle>();
        if (_navMeshObstacle == null)
        {
            Debug.LogWarning($"MovableObstacle {gameObject.name} не имеет компонента NavMeshObstacle. Тележка не будет его обходить.", this);
        }


    }

    void Update()
    {
        if (!_isBlocking) // Если уже не блокируем (объект в конечной точке), нет смысла в Update
        {
            // Убедимся, что компонент отключен, если он больше не нужен
            if (enabled) enabled = false;
            return;
        }
        bool playerCanPush = CheckPlayerCanPush();

        if (playerCanPush)
        {
            // 1. Увеличиваем прогресс (для начала сдвига и сокращения таймера)
            _currentPushProgress = Mathf.Min(requiredPushTime, _currentPushProgress + Time.deltaTime);

            // 2. Логика СДВИГА: Если прогресс достигнут И объект еще не достиг конечной точки
            if (_currentPushProgress >= requiredPushTime)
            {
                // Если таймер еще не сокращен, сокращаем его
                if (!_timerReduced)
                {
                    ReduceMonsterTimer();
                }

                // Двигаем и поворачиваем препятствие
                MoveAndRotateObstacleTowardsTarget(); // Изменено на новый метод
            }

            // Останавливаем тележку, пока игрок взаимодействует с препятствием
            if (cartController != null)
            {
                cartController.StopCartMovement();
            }
        }
        else
        {
            // Если игрок прекратил толкать, прогресс сбросится, и объект остановится.
            _currentPushProgress = 0f;

            // Возобновляем движение тележки, если игрок не толкает этот объект
            if (cartController != null)
            {
                cartController.ResumeCartMovement();
            }
        }
    }

    private bool CheckPlayerCanPush()
    {
        if (playerController == null || playerInputHandler == null || playerController.playerCamera == null) return false;

        // 1. Кнопка "Толкать" зажата?
        if (!playerInputHandler.IsPushInputHeld()) return false;

        // 2. Игрок достаточно близко?
        float dist = Vector3.Distance(playerController.transform.position, transform.position);
        if (dist > interactionRange) return false;

        // 3. Игрок смотрит на препятствие?
        Vector3 directionToObstacle = (transform.position - playerController.playerCamera.transform.position).normalized;
        float angle = Vector3.Angle(playerController.playerCamera.transform.forward, directionToObstacle);
        if (angle > playerPushAngleThreshold) return false;

        // 4. Линия видимости (рейкаст)
        RaycastHit hit;

        if (Physics.Raycast(playerController.playerCamera.transform.position, directionToObstacle, out hit, interactionRange, ~0))
        {
            if (hit.collider.gameObject != gameObject)
            {
                // Найдено что-то другое, блокирующее вид/путь
                return false;
            }
        }

        return true;
    }

    private void ReduceMonsterTimer()
    {
        _timerReduced = true;
        Debug.Log($"Obstacle {gameObject.name} push started! Reducing monster timer.");

        // Логика сокращения таймера
        if (creatureController != null && timerReductionConfig != null)
        {
            float reductionAmount = Random.Range(timerReductionConfig.minReduction, timerReductionConfig.maxReduction);
            creatureController.ReduceTimer(reductionAmount);
            Debug.Log($"Monster timer reduced by {reductionAmount:F1}s due to obstacle push.");
        }
    }

    // Измененный метод для движения и поворота
    private void MoveAndRotateObstacleTowardsTarget()
    {
        // 1. Движение
        transform.position = Vector3.MoveTowards(transform.position, targetMovePosition.position, Time.deltaTime * moveSpeed);

        // 2. Поворот
        transform.rotation = Quaternion.Slerp(transform.rotation, targetMoveRotation, Time.deltaTime * rotationSpeed);


        // 3. Проверка достижения цели (позиции И поворота)
        // Достигнута ли позиция
        bool positionReached = Vector3.Distance(transform.position, targetMovePosition.position) < 0.05f;
        // Достигнут ли поворот (с небольшой погрешностью)
        bool rotationReached = Quaternion.Angle(transform.rotation, targetMoveRotation) < 1.0f;


        if (positionReached && rotationReached)
        {
            // Цель достигнута - объект больше не блокирует путь
            _isBlocking = false;

            Debug.Log("Obstacle reached final position and rotation. Path is clear.");

            // Отключаем все коллайдеры для NavMesh, чтобы тележка могла проехать
            if (blockingCollider != null) blockingCollider.enabled = false;
            if (_navMeshObstacle != null) _navMeshObstacle.enabled = false;

            // Теперь тележка может возобновить движение, если игрок все еще пытается ее толкать
            if (cartController != null)
            {
                cartController.ResumeCartMovement();
            }

            enabled = false; // Отключаем Update, так как объект достиг конечного состояния
        }
    }

    // Для отладки в редакторе
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        if (targetMovePosition != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, targetMovePosition.position);
            Gizmos.DrawSphere(targetMovePosition.position, 0.2f);

            // Визуализация целевого вращения
            Gizmos.color = Color.blue;
            // Рисуем стрелку, показывающую, куда будет смотреть объект
            Vector3 targetForward = targetMoveRotation * Vector3.forward;
            Gizmos.DrawRay(targetMovePosition.position, targetForward * 0.5f);
        }
    }

}