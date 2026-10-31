using UnityEngine;
using UnityEngine.AI; // Для использования NavMeshAgent для движения по маршруту
using GameSystem; // using System.Collections.Generic; // Если понадобятся списки, раскомментируйте

public class CartController : MonoBehaviour
{
    [Header("Настройки движения тележки")]
    [Tooltip("Скорость, с которой тележка будет двигаться при толкании.")]
    public float pushSpeed = 2.0f;
    [Tooltip("Расстояние, на котором тележка считает, что достигла следующей точки маршрута.")]
    public float waypointTolerance = 0.5f;
    [Tooltip("Максимальный угол между направлением взгляда игрока и ручкой тележки для начала взаимодействия.")]
    public float playerPushAngleThreshold = 45f;
    [Tooltip("Максимальное расстояние, на котором игрок может находиться от ручки, чтобы толкать тележку.")]
    public float playerPushDistance = 2f;
    [Tooltip("Маска слоя для препятствий при проверке линии видимости игрока к ручке тележки.")]
    public LayerMask obstaclesLayer;
    [Tooltip("Скорость вращения тележки, чтобы она смотрела в сторону движения.")]
    public float rotationSpeed = 5.0f; // Скорость вращения тележки

    [Header("Ссылки")]
    [Tooltip("Точка на тележке (пустой GameObject), на которую игрок должен смотреть для взаимодействия (ручка).")]
    public Transform cartHandleTransform;
    [Tooltip("Массив точек (пустые GameObject'ы) вдоль маршрута, по которому должна двигаться тележка.")]
    public Transform[] cartPathPoints;
    [Tooltip("Коллайдер GameObject'а лифта - конечная точка этого этапа квеста.")]
    public Collider liftCollider;
    [Tooltip("Ссылка на CreatureController в сцене для управления таймером монстра.")]
    public CreatureController creatureController;
    [Tooltip("Ссылка на PlayerController для получения информации о движении игрока и положении камеры.")]
    public PlayerController playerController;
    [Tooltip("Ссылка на PlayerInputHandler для получения состояния кнопки толкания.")]
    public PlayerInputHandler playerInputHandler;

    [Tooltip("Ссылка на монитор, который нужно активировать после завершения квеста.")]
    public MagicSquareMonitor magicSquareMonitor;

    [Header("Аудио")]
    [Tooltip("Звук скрипа тележки, который проигрывается при движении.")]
    public AudioClip creakSound;
    [Tooltip("Громкость звука скрипа тележки.")]
    [Range(0f, 1f)]
    public float creakVolume = 0.6f;
    private AudioSource audioSource;

    private NavMeshAgent navMeshAgent;
    private int currentPathPointIndex = 0;
    private bool isPushing = false; // Флаг, который показывает, *движется ли тележка*
    private bool isQuestCompleted = false;
    private bool robotIsAssembled = false; // Флаг, указывающий, собран ли робот и можно ли толкать тележку
    public bool IsQuestCompletedPublic => isQuestCompleted;
    public bool IsPushing => isPushing; // Публичное свойство для PlayerController

    // Множитель ускорения таймера монстра при толкании тележки
    private const float MONSTER_TIMER_ACCELERATION_FACTOR = 1.25f;

    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent == null)
        {
            Debug.LogError("CartController требует компонент NavMeshAgent!", this);
            enabled = false;
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true; // Скрип будет зацикливаться при толкании
            audioSource.volume = 0.6f; // Громкость скрипа
            audioSource.playOnAwake = false;
        }
        audioSource.volume = creakVolume;
        // Попытка найти PlayerController, если не назначен в Инспекторе
        if (playerController == null)
        {
            playerController = FindAnyObjectByType<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("PlayerController не найден в сцене или не назначен CartController!", this);
            }
        }
        // Попытка найти CreatureController, если не назначен в Инспекторе
        if (creatureController == null)
        {
            creatureController = FindAnyObjectByType<CreatureController>();
            if (creatureController == null)
            {
                Debug.LogWarning("CreatureController не найден в сцене или не назначен CartController! Таймер монстра не будет затронут.", this);
            }
        }
        // Попытка найти PlayerInputHandler, если не назначен в Инспекторе
        if (playerInputHandler == null)
        {
            playerInputHandler = FindAnyObjectByType<PlayerInputHandler>();
            if (playerInputHandler == null)
            {
                Debug.LogError("PlayerInputHandler не найден в сцене или не назначен CartController! Тележку не удастся толкать.", this);
            }
        }

        // Изначально тележка остановлена
        navMeshAgent.isStopped = true;
        navMeshAgent.speed = pushSpeed;

        if (liftCollider == null)
        {
            Debug.LogError("Lift Collider не назначен CartController! Квест не сможет быть завершен.", this);
        }
    }

    void Update()
    {
        // Если робот не собран или квест уже завершен, тележка неактивна
        if (!robotIsAssembled || isQuestCompleted) return;

        // Вся логика определения, должна ли тележка толкаться, теперь здесь
        ManagePushingState();

        if (isPushing)
        {
            MoveCartAlongPath();
            // Вращаем тележку, чтобы она смотрела в сторону движения
            RotateCartTowardsDestination();
        }
        else
        {
            // Если игрок не толкает тележку, она останавливается
            navMeshAgent.isStopped = true;
            StopCreakSound();
            // Сбрасываем ускорение таймера монстра, если оно было активировано
            if (creatureController != null)
            {
                creatureController.SetTimerAcceleration(1.0f); // Восстанавливаем нормальную скорость
            }
        }

        // Проверка завершения квеста выполняется постоянно, когда тележка может быть толкаема
        CheckQuestCompletion();
    }

    /// <summary>
    /// Управляет состоянием толкания тележки на основе ввода игрока и условий.
    /// </summary>
    private void ManagePushingState()
    {
        if (playerController == null || playerInputHandler == null) return;

        // Проверяем все условия, чтобы определить, должен ли игрок толкать тележку
        bool shouldBePushing = playerInputHandler.IsPushInputHeld() && // Кнопка "Push" зажата (информация от PlayerInputHandler)
                               playerController.IsMovingForward() && // Игрок движется вперед
                               IsPlayerLookingAtHandle() && // Игрок смотрит на ручку
                               IsPlayerCloseToHandle(); // Игрок достаточно близко к ручке

        if (shouldBePushing && !isPushing)
        {
            Debug.Log("Игрок начал толкать тележку!");
            isPushing = true;
            navMeshAgent.isStopped = false;
            PlayCreakSound();

            // Ускоряем таймер монстра
            if (creatureController != null)
            {
                creatureController.SetTimerAcceleration(MONSTER_TIMER_ACCELERATION_FACTOR);
            }
            // Информируем PlayerController о начале толкания
            playerController.SetPushingCartState(true, pushSpeed);
        }
        else if (!shouldBePushing && isPushing)
        {
            Debug.Log("Игрок прекратил толкать тележку.");
            isPushing = false;
            navMeshAgent.isStopped = true;
            StopCreakSound();

            // Восстанавливаем нормальную скорость таймера монстра
            if (creatureController != null)
            {
                creatureController.SetTimerAcceleration(1.0f);
            }
            // Информируем PlayerController об окончании толкания
            playerController.SetPushingCartState(false, 0f);
        }
    }

    /// <summary>
    /// Проверяет, смотрит ли игрок на ручку тележки.
    /// </summary>
    private bool IsPlayerLookingAtHandle()
    {
        if (playerController == null || cartHandleTransform == null || playerController.playerCamera == null) return false;

        // Направление от камеры игрока к ручке
        Vector3 directionToHandle = (cartHandleTransform.position - playerController.playerCamera.transform.position).normalized;
        // Угол между направлением взгляда игрока и направлением на ручку
        float angle = Vector3.Angle(playerController.playerCamera.transform.forward, directionToHandle);

        return angle < playerPushAngleThreshold;
    }

    /// <summary>
    /// Проверяет, находится ли игрок достаточно близко к ручке тележки и нет ли препятствий.
    /// </summary>
    private bool IsPlayerCloseToHandle()
    {
        if (playerController == null || cartHandleTransform == null) return false;

        // Расстояние от игрока до ручки
        float distance = Vector3.Distance(playerController.transform.position, cartHandleTransform.position);

        // Дополнительная проверка рейкастом, чтобы убедиться, что нет препятствий между игроком и ручкой.
        // Используем позицию игрока + небольшое смещение вверх для рейкаста
        Vector3 rayOrigin = playerController.transform.position + Vector3.up * 0.5f;
        Vector3 rayDirection = (cartHandleTransform.position - rayOrigin).normalized;
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, playerPushDistance, obstaclesLayer))
        {
            // Если рейкаст попал в объект, и этот объект не является самой ручкой (если у ручки есть коллайдер)
            // или частью самой тележки.
            // Используем transform.root для проверки на принадлежность к тележке.
            if (hit.collider.transform != cartHandleTransform && hit.collider.transform.root != transform.root)
            {
                return false; // Найдено препятствие, блокирующее путь к ручке
            }
        }

        return distance <= playerPushDistance;
    }

    /// <summary>
    /// Перемещает тележку по заданному маршруту.
    /// </summary>
    private void MoveCartAlongPath()
    {
        if (cartPathPoints == null || cartPathPoints.Length == 0)
        {
            Debug.LogWarning("Нет точек маршрута для тележки!", this);
            isPushing = false; // Останавливаем толкание, если нет маршрута
            return;
        }

        // Если тележка достигла последней точки маршрута, останавливаемся на ней
        if (currentPathPointIndex >= cartPathPoints.Length)
        {
            navMeshAgent.isStopped = true; // Останавливаем тележку в конце маршрута
            Debug.Log("Тележка достигла конца маршрута. Ожидание условий завершения квеста.");
            isPushing = false; // Игрок должен будет дотолкать её в лифт вручную
            playerController.SetPushingCartState(false, 0f); // Сообщаем игроку, что толкание завершено
            return;
        }

        Transform targetPoint = cartPathPoints[currentPathPointIndex];
        if (targetPoint == null)
        {
            Debug.LogError($"Точка маршрута {currentPathPointIndex} равна null!", this);
            isPushing = false;
            playerController.SetPushingCartState(false, 0f); // Сообщаем игроку
            return;
        }

        // Задаем цель для NavMeshAgent
        navMeshAgent.SetDestination(targetPoint.position);

        // Если тележка достаточно близко к текущей точке маршрута, переходим к следующей
        if (Vector3.Distance(transform.position, targetPoint.position) < waypointTolerance)
        {
            currentPathPointIndex++;
            if (currentPathPointIndex >= cartPathPoints.Length)
            {
                Debug.Log("Тележка достигла последней точки маршрута.");
                navMeshAgent.isStopped = true;
                isPushing = false; // Останавливаем автоматическое толкание по маршруту
                playerController.SetPushingCartState(false, 0f); // Сообщаем игроку
            }
        }
    }

    /// <summary>
    /// Вращает тележку так, чтобы ее 'forward' смотрел в направлении текущей желаемой скорости NavMeshAgent.
    /// </summary>
    private void RotateCartTowardsDestination()
    {
        if (navMeshAgent.hasPath && !navMeshAgent.isStopped)
        {
            Vector3 desiredVelocity = navMeshAgent.desiredVelocity;
            desiredVelocity.y = 0; // Игнорируем Y-координату, вращаемся только по горизонтали

            if (desiredVelocity.sqrMagnitude > 0.01f) // Избегаем вращения, если нет значимого движения
            {
                Quaternion targetRotation = Quaternion.LookRotation(desiredVelocity);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }
    }

    /// <summary>
    /// Проигрывает звук скрипа тележки.
    /// </summary>
    private void PlayCreakSound()
    {
        if (audioSource != null && creakSound != null && !audioSource.isPlaying)
        {
            audioSource.clip = creakSound;
            audioSource.Play();
        }
    }

    /// <summary>
    /// Останавливает звук скрипа тележки.
    /// </summary>
    private void StopCreakSound()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    /// <summary>
    /// Устанавливает флаг, сигнализирующий о том, что робот собран и тележку можно толкать.
    /// Этот метод вызывается RobotAssemblyController.
    /// </summary>
    public void SetRobotAssembled(bool assembled)
    {
        robotIsAssembled = assembled;
        if (assembled)
        {
            Debug.Log("Робот собран, тележка теперь интерактивна (можно толкать).");
        }
    }

    /// <summary>
    /// Проверяет, завершен ли квест (тележка и игрок находятся в коллайдере лифта).
    /// </summary>
    private void CheckQuestCompletion()
    {
        if (isQuestCompleted || liftCollider == null || playerController == null) return;

        // Проверяем, находится ли тележка внутри коллайдера лифта
        // Используем центр NavMeshAgent для более точной проверки относительно тележки
        bool cartInLift = liftCollider.bounds.Contains(navMeshAgent.nextPosition);

        // Проверяем, находится ли игрок внутри коллайдера лифта
        // Для простоты используем корневую позицию игрока
        bool playerInLift = liftCollider.bounds.Contains(playerController.transform.position);

        if (cartInLift && playerInLift)
        {
            isQuestCompleted = true;
            Debug.Log("Тележка с роботом и игрок находятся в лифте! Квест завершен!");
            isPushing = false;
            navMeshAgent.isStopped = true;
            StopCreakSound();
            // Восстанавливаем нормальный таймер монстра после завершения квеста
            if (creatureController != null)
            {
                creatureController.SetTimerAcceleration(1.0f);
            }
            playerController.SetPushingCartState(false, 0f); // Сообщаем игроку, что толкание завершено

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.CompleteObjective("QUEST_CART");
            }

            // TODO: Запустить следующий этап квеста, анимацию лифта, затемнение экрана и т.д.
            // Например, можно вызвать метод из QuestManager, если он есть.
            // FindObjectOfType<QuestManager>()?.CompleteCartQuest();
            if (magicSquareMonitor != null)
            {
                magicSquareMonitor.ActivateMonitor();
            }
            else
            {
                Debug.LogWarning("Ссылка на MagicSquareMonitor не установлена в CartController. Монитор не будет активирован.", this);
            }
        }
    }
    public void StopCartMovement() // НОВЫЙ МЕТОД
    {
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh && !navMeshAgent.isStopped)
        {
            navMeshAgent.isStopped = true;
            // Можно добавить логику остановки звука, если это требуется при внешнем прерывании
            // StopCreakSound(); 
        }
    }

    public void ResumeCartMovement() // НОВЫЙ МЕТОД
    {
        // Возобновляем движение, только если игрок все еще находится в состоянии 'толкания'
        // Проверяем флаг isPushing, чтобы убедиться, что игрок не отпустил кнопку
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh && isPushing)
        {
            navMeshAgent.isStopped = false;
            // Возобновляем звук, если он был остановлен
            // PlayCreakSound();
        }
    }
    // --- Визуализация Гизмо для отладки в редакторе ---
    void OnDrawGizmosSelected()
    {
        if (cartHandleTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(cartHandleTransform.position, 0.1f);
            Gizmos.DrawLine(cartHandleTransform.position, cartHandleTransform.position + Vector3.up * 0.5f);
        }

        if (cartPathPoints != null)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < cartPathPoints.Length; i++)
            {
                if (cartPathPoints[i] != null)
                {
                    Gizmos.DrawSphere(cartPathPoints[i].position, waypointTolerance);
                    if (i > 0 && cartPathPoints[i - 1] != null)
                    {
                        Gizmos.DrawLine(cartPathPoints[i - 1].position, cartPathPoints[i].position);
                    }
                }
            }
        }

        if (liftCollider != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawCube(liftCollider.bounds.center, liftCollider.bounds.size);
        }
    }
}