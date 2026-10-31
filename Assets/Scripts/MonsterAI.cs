using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro; // Для TextMeshProUGUI
using UnityEngine.SceneManagement; // Для SceneManager

public class MonsterAI : MonoBehaviour
{
    [Header("Core Components")]
    [Tooltip("Ссылка на дочерний объект с 3D-моделью, NavMeshAgent и коллайдером.")]
    public GameObject monsterModel; // Измените в инспекторе на Robot Model
    private Animator animator; // Этот animator будет использоваться только для проверки, но не для прямого вызова
    private RobotAnimationController robotAnimator; // НОВАЯ ССЫЛКА на RobotAnimationController

    [Header("Audio (Manual Timing)")]
    [Tooltip("Компонент для воспроизведения звуков.")]
    public AudioSource audioSource;
    [Tooltip("Массив звуковых клипов для шагов. Будет проигрываться случайный.")]
    public AudioClip[] footstepSounds;
    [Tooltip("Громкость звуков шагов.")]
    [Range(0f, 1f)]
    public float footstepVolume = 0.5f;

    [Header("Footstep Timings")]
    [Tooltip("Интервал между шагами в состоянии 'Patrol' (в секундах).")]
    public float patrolStepInterval = 0.8f;
    [Tooltip("Интервал между шагами в состояниях 'Hunt' и 'Investigate'.")]
    public float huntStepInterval = 0.5f;
    [Tooltip("Интервал между шагами в состоянии 'Chase'.")]
    public float chaseStepInterval = 0.3f;

    [Header("State Transition Sounds")] // <<= НОВАЯ СЕКЦИЯ
    [Tooltip("Звук, когда монстр замечает что-то подозрительное (переходит в Investigate/Hunt).")]
    public AudioClip onBecomeAlertSound;
    [Tooltip("Звук (крик), когда монстр начинает погоню (переходит в Chase).")]
    public AudioClip onStartChaseSound;
    [Tooltip("Звук, когда монстр теряет игрока и успокаивается (переходит в Patrol).")]
    public AudioClip onCalmDownSound;
    [Tooltip("Общая громкость для звуков смены состояний.")]
    [Range(0f, 1f)]
    public float stateSoundVolume = 0.8f;

    [Header("AI Settings")]
    public float baseAggression = 0.1f;
    public float currentAggression = 0f;
    public float aggressionDecayRate = 0.05f;

    [Header("Movement")]
    public float patrolSpeed = 2f;
    public float huntSpeed = 4f;
    public float chaseSpeed = 6f;

    [Header("Detection")]
    public float visionRange = 15f;
    public float visionAngle = 90f; // Total angle of vision (e.g., 90 degrees means +/- 45 degrees from forward)

    [Header("Hiding Spot Check")]
    public float checkDuration = 2f;
    public float checkAnimationDuration = 2.5f; // Длительность анимации проверки (SitDown, StandUp, CheckCab)
    public float doorInteractionTime = 1.0f; // Время на открытие/закрытие двери (для реальной механики двери, не анимации)

    [Header("Pursuit Settings")]
    public float maxChaseTime = 15f;
    public float maxHuntTime = 8f;
    public float catchDistance = 2.5f;
    [Tooltip("Как долго (в секундах) монстр будет продолжать погоню на полной скорости после потери игрока из виду.")]
    public float chasePersistenceTime = 3.0f;


    [Header("Cinematic Death References")]
    [Tooltip("Камера, которая будет активирована для показа монстра. Должна быть дочерней по отношению к монстру.")]
    public Camera monsterDeathCamera;
    [Tooltip("Точка, на которую будет смотреть камера монстра. Должна быть дочерней по отношению к игроку.")]
    public Transform playerDeathLookAtPoint;
    [Tooltip("Задержка перед воспроизведением анимации удара после переключения камеры.")]
    public float preAnimationDelay = 0.0f;
    [Tooltip("Задержка после воспроизведения анимации удара перед показом панели смерти. Установлено на 3 секунды.")]
    public float postAnimationDelay = 1.0f;
    [Tooltip("Минимальное расстояние, на котором монстр должен быть от игрока для кинематографической смерти от первого лица. Игрок будет отодвинут назад, если расстояние меньше.")]
    public float minDistanceForFPCatch = 1.5f;
    [Tooltip("Высота, на которую камера игрока должна смотреть на монстра (относительно земли).")]
    public float playerLookAtHeight = 1.0f;


    [Header("UI")]
    public TextMeshProUGUI deathText;
    public GameObject gameOverPanel; // Добавлено для панели Game Over
    public float restartSceneDelay = 3f; // Добавлено для задержки перезапуска сцены


    [Header("Door Interaction")]
    public LayerMask doorLayerMask = 1 << 8; // Предположим, что двери на слое 8
    private SlidingDoor[] allDoors;

    private NavMeshAgent agent;
    private CapsuleCollider monsterCollider; // Измените в инспекторе на Robot Collider
    private FuzzyLogicSystem fuzzySystem;
    private PlayerController player;
    private List<HidingSpot> hidingSpots = new List<HidingSpot>();
    private List<Room> rooms = new List<Room>();

    // Input data for fuzzy logic
    private float playerNoiseLevel = 0f;
    private float playerVisibility = 0f;
    private float timeSinceLastPlayerContact = 100f; // Time since last seen OR heard
    private float lastPlayerVisualContactTime = 100f; // Time since last *visually seen* only

    // Current behavior
    private AIState currentState = AIState.Patrol;
    private float stateTimer = 0f;
    private bool isCheckingHidingSpot = false;
    private bool _playerCaught = false; // Добавлено для отслеживания состояния поимки игрока

    // Robot memory
    private Vector3 lastKnownPlayerPosition;
    private Room lastKnownPlayerRoom;
    private float lastNoiseHeardTime = 0f; // Not directly used for state, mainly for debug/info
    private bool wasPlayerVisibleLastFrame = false;
    private HidingSpot prioritizedHidingSpot = null; // Укрытие, которое робот видел, как в него спрятались
    private float memoryOfHidingSpotTimer = 0f;
    private const float HIDING_SPOT_MEMORY_DURATION = 7f; // Как долго робот помнит про конкретное укрытие
    private Room currentRoom;       // В какой комнате он сейчас
    private Room previousRoom;      // В какой комнате он был до этого
    private Room destinationRoom;   // В какую комнату он направляется
    private bool isPerformingLocalPatrol = false;
    private int localPatrolPointsVisited = 0;
    private const int maxLocalPatrolPoints = 2; // Сколько точек обойти в коридоре перед уходом
    private float chaseLostPlayerTimestamp = -1f;
    [Tooltip("Длительность паузы (в секундах), когда монстр 'думает' у запертой двери.")]
    public float lockedDoorPauseDuration = 2.0f;
    // For random patrol
    private float patrolTimer = 0f;
    private float stuckTimer = 0f;
    private Vector3 lastPosition;

    private Vector3 _lureGizmoPosition = Vector3.zero;
    private float _lureGizmoRadius = 0f;
    private float _lureGizmoTimer = 0f;
    public float lureGizmoDisplayDuration = 3f; // Как долго отображать Gizmo приманки

    private Vector3 _cameraAlarmPosition = Vector3.zero;
    private float _cameraAlarmTimer = 0f;
    public float cameraAlarmGizmoDisplayDuration = 5f; // Как долго отображать Gizmo тревоги камеры

    private Coroutine currentSearchCoroutine;
    private bool isInteractingWithDoor = false;
    private bool isExecutingMandatorySearch = false;

    // _maxCurrentAgentSpeed теперь используется только для нормализации,
    // actual speed is set on agent and passed to RobotAnimationController
    private float _maxCurrentAgentSpeed;
    private Vector3 _finalDestination; // Здесь будем хранить "истинную" цель, если идем к двери
    private bool _isPathfindingToDoor = false; // Флаг, что мы в процессе открытия двери

    public enum AIState { Patrol, Investigate, Hunt, Chase }

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogWarning("[RobotAI] AudioSource не найден. Звуки работать не будут.");
            }
        }

        // Запускаем корутину, которая будет управлять звуками шагов
        if (audioSource != null && footstepSounds.Length > 0)
        {
            StartCoroutine(FootstepPlayerCoroutine());
        }
        else
        {
            Debug.LogWarning("[RobotAI] Звуки шагов не будут воспроизводиться, так как не назначен AudioSource или отсутствуют аудиоклипы.");
        }

        if (monsterModel == null)
        {
            Debug.LogError("[RobotAI] Ссылка на 3D-модель робота (Robot Model) не назначена в инспекторе!");
            this.enabled = false;
            return;
        }

        // Получаем RobotAnimationController
        robotAnimator = GetComponent<RobotAnimationController>(); ;
        if (robotAnimator == null)
        {
            Debug.LogError("[RobotAI] RobotAnimationController не найден на 'monsterModel'. Анимации работать не будут.");
            this.enabled = false;
            return;
        }

        animator = monsterModel.GetComponent<Animator>(); // Используем только для изначальной проверки
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            Debug.LogError("[RobotAI] ERROR: На 'monsterModel' не назначен Animator или Animator Controller! Анимации не будут работать.");
            this.enabled = false;
            return;
        }
        Debug.Log($"[RobotAI] Animator Controller Name: {animator.runtimeAnimatorController.name}");


        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("[RobotAI] NavMeshAgent не найден на объекте RobotAI.");
            this.enabled = false;
            return;
        }

        monsterCollider = GetComponent<CapsuleCollider>();
        fuzzySystem = GetComponent<FuzzyLogicSystem>();
        player = FindFirstObjectByType<PlayerController>();

        InitializeFuzzySystem();
        FindAllHidingSpots();
        FindAllRooms();
        FindAllDoors();
        StartCoroutine(BehaviorUpdateCoroutine());

        if (deathText != null)
            deathText.gameObject.SetActive(false);
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false); // Убедимся, что панель смерти неактивна по умолчанию

        lastPosition = transform.position;
        _maxCurrentAgentSpeed = chaseSpeed; // Инициализируем максимальную скорость
        SetRandomPatrolDestination(); // Вызов UpdateAnimatorSpeed теперь через robotAnimator
        FindAllDoors();
        SetupMonsterCollider();

        agent.stoppingDistance = 0.1f;
        PickNextDestinationRoom();
        // Инициализируем анимацию скорости сразу после настройки агента и контроллера
        StartCoroutine(DelayedUpdateAnimatorSpeed());
    }

    void InitializeFuzzySystem()
    {
        if (fuzzySystem == null) return;

        fuzzySystem.inputVariables.Clear();
        fuzzySystem.outputVariables.Clear();
        fuzzySystem.rules.Clear();

        FuzzyLogicSystem.FuzzyVariable noiseVar = new FuzzyLogicSystem.FuzzyVariable
        {
            variableName = "PlayerNoise"
        };
        FuzzyLogicSystem.FuzzySet noiseLow = new FuzzyLogicSystem.FuzzySet
        {
            name = "NoiseLow",
            membershipFunction = CreateCurve(new float[] { 0f, 1f, 0.3f, 0f })
        };
        FuzzyLogicSystem.FuzzySet noiseMedium = new FuzzyLogicSystem.FuzzySet
        {
            name = "NoiseMedium",
            membershipFunction = CreateCurve(new float[] { 0.15f, 0f, 0.45f, 1f, 0.75f, 0f })
        };
        FuzzyLogicSystem.FuzzySet noiseHigh = new FuzzyLogicSystem.FuzzySet
        {
            name = "NoiseHigh",
            membershipFunction = CreateCurve(new float[] { 0.5f, 0f, 0.75f, 1f, 1f, 1f })
        };
        noiseVar.fuzzySets.AddRange(new[] { noiseLow, noiseMedium, noiseHigh });

        FuzzyLogicSystem.FuzzyVariable visibleVar = new FuzzyLogicSystem.FuzzyVariable
        {
            variableName = "PlayerVisible"
        };
        FuzzyLogicSystem.FuzzySet visibleLow = new FuzzyLogicSystem.FuzzySet
        {
            name = "VisibleLow",
            membershipFunction = CreateCurve(new float[] { 0f, 1f, 0.3f, 0f })
        };
        FuzzyLogicSystem.FuzzySet visibleHigh = new FuzzyLogicSystem.FuzzySet
        {
            name = "VisibleHigh",
            membershipFunction = CreateCurve(new float[] { 0.2f, 0f, 0.5f, 1f, 1f, 1f })
        };
        visibleVar.fuzzySets.AddRange(new[] { visibleLow, visibleHigh });

        FuzzyLogicSystem.FuzzyVariable aggressionVar = new FuzzyLogicSystem.FuzzyVariable
        {
            variableName = "AggressionLevel"
        };
        FuzzyLogicSystem.FuzzySet aggressionLow = new FuzzyLogicSystem.FuzzySet
        {
            name = "AggressionLow",
            membershipFunction = CreateCurve(new float[] { 0f, 1f, 0.3f, 0f })
        };
        FuzzyLogicSystem.FuzzySet aggressionMedium = new FuzzyLogicSystem.FuzzySet
        {
            name = "AggressionMedium",
            membershipFunction = CreateCurve(new float[] { 0.1f, 0f, 0.35f, 1f, 0.65f, 0f })
        };
        FuzzyLogicSystem.FuzzySet aggressionHigh = new FuzzyLogicSystem.FuzzySet
        {
            name = "AggressionHigh",
            membershipFunction = CreateCurve(new float[] { 0.4f, 0f, 0.6f, 1f, 1f, 1f })
        };
        aggressionVar.fuzzySets.AddRange(new[] { aggressionLow, aggressionMedium, aggressionHigh });

        fuzzySystem.inputVariables.Add(noiseVar);
        fuzzySystem.inputVariables.Add(visibleVar);
        fuzzySystem.outputVariables.Add(aggressionVar);

        FuzzyLogicSystem.FuzzyRule rule1 = new FuzzyLogicSystem.FuzzyRule
        {
            name = "HighNoise",
            conditionVariable = "PlayerNoise",
            conditionSet = "NoiseHigh",
            outputVariable = "AggressionLevel",
            outputSet = "AggressionHigh"
        };
        FuzzyLogicSystem.FuzzyRule rule2 = new FuzzyLogicSystem.FuzzyRule
        {
            name = "PlayerVisible",
            conditionVariable = "PlayerVisible",
            conditionSet = "VisibleHigh",
            outputVariable = "AggressionLevel",
            outputSet = "AggressionHigh"
        };
        FuzzyLogicSystem.FuzzyRule rule3 = new FuzzyLogicSystem.FuzzyRule
        {
            name = "MediumNoise",
            conditionVariable = "PlayerNoise",
            conditionSet = "NoiseMedium",
            outputVariable = "AggressionLevel",
            outputSet = "AggressionMedium"
        };
        FuzzyLogicSystem.FuzzyRule rule4 = new FuzzyLogicSystem.FuzzyRule
        {
            name = "LowNoise",
            conditionVariable = "PlayerNoise",
            conditionSet = "NoiseLow",
            outputVariable = "AggressionLevel",
            outputSet = "AggressionLow"
        };
        fuzzySystem.rules.AddRange(new[] { rule1, rule2, rule3, rule4 });
    }

    AnimationCurve CreateCurve(float[] points)
    {
        AnimationCurve curve = new AnimationCurve();
        for (int i = 0; i < points.Length; i += 2)
        {
            curve.AddKey(points[i], points[i + 1]);
        }
        return curve;
    }

    IEnumerator BehaviorUpdateCoroutine()
    {
        while (true)
        {
            if (_playerCaught || monsterModel == null || agent == null || robotAnimator == null) yield break; // Добавлена проверка на _playerCaught
            UpdateSensors();
            UpdateFuzzyLogic();
            UpdateBehavior();
            yield return new WaitForSeconds(0.2f);
        }
    }

    void UpdateAnimatorSpeed()
    {
        if (robotAnimator == null)
        {
            Debug.LogError("[RobotAI] ERROR: RobotAnimationController component is NULL! Cannot update speed.");
            return;
        }
        if (agent == null)
        {
            Debug.LogError("[RobotAI] ERROR: NavMeshAgent component is NULL! Cannot update speed.");
            return;
        }

        float normalizedSpeed;
        float agentVelocityMagnitude = agent.velocity.magnitude;

        bool isMoving = agent.enabled && !agent.isStopped && agentVelocityMagnitude > 0.05f && agent.hasPath && agent.remainingDistance > agent.stoppingDistance;

        if (!isMoving)
        {
            normalizedSpeed = 0f;
            if (robotAnimator.GetCurrentNavMeshSpeed() != 0f) // Используем метод из RobotAnimationController для проверки
            {
                Debug.Log($"[RobotAI] INFO: Agent stopped or very slow (Vel: {agentVelocityMagnitude:F2}), setting animation Speed to 0.");
            }
        }
        else // Агент активен и движется
        {
            float currentAgentEffectiveSpeed = agentVelocityMagnitude;
            normalizedSpeed = Mathf.InverseLerp(0f, _maxCurrentAgentSpeed, currentAgentEffectiveSpeed);

            Debug.Log($"[RobotAI] DEBUG: Agent moving. currentAgentEffectiveSpeed: {currentAgentEffectiveSpeed:F2}, MaxAgentSpeed: {_maxCurrentAgentSpeed:F2}, normalizedSpeed calculated: {normalizedSpeed:F2}");

            if (currentAgentEffectiveSpeed > 0.1f && normalizedSpeed < 0.01f)
            {
                Debug.LogWarning($"[RobotAI] WARNING: currentAgentEffectiveSpeed is {currentAgentEffectiveSpeed:F2} but normalizedSpeed is {normalizedSpeed:F2}! MaxAgentSpeed: {_maxCurrentAgentSpeed:F2}");
            }
        }

        // Устанавливаем параметр Animator через RobotAnimationController
        robotAnimator.SetSpeed(normalizedSpeed);
    }

    IEnumerator DelayedUpdateAnimatorSpeed()
    {
        yield return null;
        UpdateAnimatorSpeed();
    }


    void UpdateSensors()
    {
        playerNoiseLevel = CalculatePlayerNoise();
        playerVisibility = CalculatePlayerVisibility();

        if (wasPlayerVisibleLastFrame && playerVisibility < 0.1f && player != null && player.isHiding)
        {
            prioritizedHidingSpot = player.currentHidingSpot;
            memoryOfHidingSpotTimer = HIDING_SPOT_MEMORY_DURATION;
            Debug.Log($"[RobotAI] ВНИМАНИЕ! Робот видел, как игрок спрятался в {prioritizedHidingSpot.name}.");
        }

        if (memoryOfHidingSpotTimer > 0)
        {
            memoryOfHidingSpotTimer -= Time.deltaTime;
            if (memoryOfHidingSpotTimer <= 0)
            {
                prioritizedHidingSpot = null;
                Debug.Log("[RobotAI] Робот забыл про конкретное укрытие.");
            }
        }

        if (_lureGizmoTimer > 0)
        {
            _lureGizmoTimer -= Time.deltaTime;
        }
        if (_cameraAlarmTimer > 0)
        {
            _cameraAlarmTimer -= Time.deltaTime;
        }


        Debug.Log($"[RobotAI] PlayerNoiseLevel: {playerNoiseLevel:F2}, PlayerVisibility: {playerVisibility:F2}");
        if (player != null)
        {
            Debug.Log($"[RobotAI Debug] Player Noise Source: Current Noise Level={player.currentNoiseLevel:F2}, Current Noise Range={player.currentNoiseRange:F2}, Is Running={player.isRunning}");
        }

        if (playerVisibility > 0.1f)
        {
            lastKnownPlayerPosition = player.transform.position;
            lastKnownPlayerRoom = FindClosestRoom(player.transform.position);
            timeSinceLastPlayerContact = 0f;
            lastPlayerVisualContactTime = 0f;
            Debug.Log($"[RobotAI] Player VISUALLY detected. Last known position updated to {lastKnownPlayerPosition}.");
        }
        else if (playerNoiseLevel > 0.1f)
        {
            lastKnownPlayerPosition = player.transform.position;
            lastKnownPlayerRoom = FindClosestRoom(player.transform.position);
            lastNoiseHeardTime = Time.time;
            timeSinceLastPlayerContact = 0f;
            Debug.Log($"[RobotAI] Player HEARD. Last known position updated to {lastKnownPlayerPosition}.");
        }
        else
        {
            timeSinceLastPlayerContact += Time.deltaTime;
            lastPlayerVisualContactTime += Time.deltaTime;
        }
        wasPlayerVisibleLastFrame = (playerVisibility > 0.1f);

    }

    float CalculatePlayerNoise()
    {
        if (player == null) return 0f;
        if (player.currentNoiseRange <= 0.01f) return 0f;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (distanceToPlayer > player.currentNoiseRange) return 0f;

        float distanceFactor = 1f - (distanceToPlayer / player.currentNoiseRange);
        float doorSoundReduction = GetDoorSoundReduction(transform.position, player.transform.position);
        float effectiveNoise = player.currentNoiseLevel * distanceFactor * (1f - doorSoundReduction);

        if (player.isRunning && distanceToPlayer <= player.currentNoiseRange)
        {
            effectiveNoise = Mathf.Max(effectiveNoise, 0.8f * (1f - doorSoundReduction));
        }

        return Mathf.Clamp01(effectiveNoise);
    }


    bool IsDoorBlockingVision(Vector3 fromPosition, Vector3 targetPosition)
    {
        foreach (SlidingDoor door in allDoors)
        {
            if (door != null && door.BlocksVision() && door.IsPointBehindDoor(fromPosition, targetPosition))
            {
                return true;
            }
        }
        return false;
    }

    float GetDoorSoundReduction(Vector3 fromPosition, Vector3 targetPosition)
    {
        float totalReduction = 0f;
        int doorsBetween = 0;

        foreach (SlidingDoor door in allDoors)
        {
            if (door != null && door.IsPointBehindDoor(fromPosition, targetPosition))
            {
                totalReduction += door.GetSoundReduction();
                doorsBetween++;
            }
        }

        return doorsBetween > 0 ? Mathf.Min(totalReduction, 0.95f) : 0f;
    }

    void OnTriggerEnter(Collider other)
    {
        // Добавлена проверка на _playerCaught
        if (_playerCaught) return;

        if (currentState == AIState.Patrol && other.CompareTag("Room"))
        {
            Room enteredRoom = other.GetComponent<Room>();

            // Если мы вошли в новую для себя комнату
            if (enteredRoom != null && enteredRoom != currentRoom)
            {
                // Обновляем "память"
                previousRoom = currentRoom;
                currentRoom = enteredRoom;

                // РЕШЕНИЕ: Что делать в этой новой комнате?
                if (currentRoom.isLargePatrolArea)
                {
                    // Это большая зона. Начинаем полноценный локальный патруль из нескольких точек.
                    Debug.Log($"[RobotAI] Вошел в большую зону '{currentRoom.name}'. Начинаю локальный патруль.");
                    isPerformingLocalPatrol = true; // Включаем режим "оставаться здесь"
                    localPatrolPointsVisited = 0;
                }
                else
                {
                    // Это маленькая комната. Мы просто зайдем внутрь и осмотримся в одной точке.
                    Debug.Log($"[RobotAI] Вошел в комнату '{currentRoom.name}'. Иду к точке внутри неё.");
                    isPerformingLocalPatrol = false; // Здесь не будет долгого патруля
                }

                // В ЛЮБОМ СЛУЧАЕ, наша первая цель - найти точку ВНУТРИ этой комнаты.
                SetLocalPatrolPoint();
            }
        }

        if (other.CompareTag("Player") && player != null && !player.isHiding)
        {
            if (currentState != AIState.Patrol)
            {
                Debug.Log("[RobotAI] Player caught in the open! GAME OVER.");
                CatchPlayer();
            }
        }

        SlidingDoor door = other.GetComponent<SlidingDoor>();
        if (door != null && door.isClosed && !isInteractingWithDoor)
        {
            StartCoroutine(HandleDoorInteraction(door));
        }
    }

    void OnTriggerExit(Collider other)
    {
        SlidingDoor door = other.GetComponent<SlidingDoor>();
        if (door != null && door.triggerType == SlidingDoor.TriggerType.MonsterOnly)
        {
            door.CloseDoor();
            Debug.Log($"[RobotAI] Closing door: {door.name}");
        }
    }


    float CalculatePlayerVisibility()
    {
        if (player == null || player.isHiding) return 0f;

        Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (distanceToPlayer > visionRange || angleToPlayer > visionAngle / 2f) return 0f;

        if (IsDoorBlockingVision(transform.position, player.transform.position))
        {
            return 0f;
        }

        Vector3 startPoint = transform.position + Vector3.up * (monsterCollider.height * 0.8f);

        CharacterController playerController = player.GetComponent<CharacterController>();
        float playerCenterY = playerController != null ? playerController.height / 2f : 1f;
        Vector3 endPoint = player.transform.position + Vector3.up * playerCenterY;

        RaycastHit hit;

        int layerMask = ~(1 << LayerMask.NameToLayer("HidingSpot"));

        Debug.DrawLine(startPoint, endPoint, Color.cyan);

        if (Physics.Linecast(startPoint, endPoint, out hit, layerMask))
        {
            if (!hit.collider.CompareTag("Player"))
            {
                return 0f;
            }
        }

        return 1f - (distanceToPlayer / visionRange);
    }

    void UpdateFuzzyLogic()
    {
        if (fuzzySystem == null) return;

        fuzzySystem.SetInput("PlayerNoise", playerNoiseLevel);
        fuzzySystem.SetInput("PlayerVisible", playerVisibility);

        fuzzySystem.Evaluate();

        float newAggressionFromFuzzy = fuzzySystem.GetOutput("AggressionLevel");

        float targetAggression = newAggressionFromFuzzy;

        if (timeSinceLastPlayerContact > 2f)
        {
            float dynamicDecayRate = aggressionDecayRate * (1f + (timeSinceLastPlayerContact / 10f));
            targetAggression = Mathf.Max(baseAggression, targetAggression - dynamicDecayRate * Time.deltaTime);
        }
        else
        {
            targetAggression = Mathf.Max(newAggressionFromFuzzy, currentAggression - aggressionDecayRate * Time.deltaTime * 0.5f);
        }

        currentAggression = Mathf.Lerp(currentAggression, targetAggression, Time.deltaTime * 5f);
        currentAggression = Mathf.Max(baseAggression, currentAggression);

        Debug.Log($"[RobotAI] Current Aggression: {currentAggression:F2} (Fuzzy output: {newAggressionFromFuzzy:F2})");
    }

    void UpdateBehavior()
    {
        // Добавлена проверка на _playerCaught
        if (_playerCaught) return;

        stateTimer += Time.deltaTime;
        patrolTimer += Time.deltaTime;

        CheckIfStuck();

        // 1. Определяем, в каком состоянии ИИ ХОТЕЛ бы быть на основе текущих данных.
        AIState desiredState = DetermineAIState();

        // 2. Всегда проверяем на прямое визуальное обнаружение - это высший приоритет.
        if (playerVisibility > 0.3f)
        {
            desiredState = AIState.Chase;
        }

        // 3. Сравниваем приоритет желаемого состояния с текущим.
        // Мы меняем состояние, только если новое состояние имеет более высокий приоритет.
        // Например, из Investigate (2) можно перейти в Hunt (3), но не наоборот.
        // Исключение: из любого состояния можно перейти в Chase (4).
        if (GetStatePriority(desiredState) > GetStatePriority(currentState))
        {
            _isPathfindingToDoor = false;
            if (currentSearchCoroutine != null) StopCoroutine(currentSearchCoroutine);
            OnStateChange(desiredState);
        }
        // Если мы в патруле, и есть причина хотя бы для расследования, переключаемся.
        else if (currentState == AIState.Patrol && desiredState != AIState.Patrol)
        {
            OnStateChange(desiredState);
        }

        // 4. Выполняем логику текущего состояния.
        ExecuteCurrentState();
    }

    void CheckIfStuck()
    {
        float movement = Vector3.Distance(transform.position, lastPosition);
        if (movement < 0.1f)
        {
            stuckTimer += Time.deltaTime;
        }
        else
        {
            stuckTimer = 0f;
        }
        lastPosition = transform.position;

        if (stuckTimer > 1.5f)
        {
            SetRandomPatrolDestination();
            stuckTimer = 0f;
            Debug.Log("[RobotAI] Robot appears stuck, setting new patrol destination.");
        }
    }

    AIState DetermineAIState()
    {
        if (prioritizedHidingSpot != null)
        {
            Debug.Log("[RobotAI] Решение: есть приоритетное укрытие. Переход в состояние Hunt.");
            return AIState.Hunt;
        }

        if (playerVisibility > 0.3f)
        {
            Debug.Log("[RobotAI] Player visible, entering CHASE state.");
            return AIState.Chase;
        }

        if (player != null && player.isRunning && playerNoiseLevel > 0.4f)
        {
            Debug.Log($"[RobotAI] Immediate running noise detected (PlayerNoiseLevel: {playerNoiseLevel:F2}), entering HUNT state.");
            return AIState.Hunt;
        }

        if (lastPlayerVisualContactTime < 5f && currentAggression > 0.6f && lastKnownPlayerPosition != Vector3.zero)
        {
            Debug.Log($"[RobotAI] Recent visual contact ({lastPlayerVisualContactTime:F2}s ago) and high aggression, entering HUNT state for direct pursuit.");
            return AIState.Hunt;
        }

        if ((playerNoiseLevel > 0.6f) || (currentAggression > 0.7f && lastKnownPlayerPosition != Vector3.zero))
        {
            Debug.Log($"[RobotAI] High general noise ({playerNoiseLevel:F2}) or very high aggression ({currentAggression:F2}), entering HUNT state.");
            return AIState.Hunt;
        }

        if ((playerNoiseLevel > 0.15f) || (currentAggression > 0.25f && lastKnownPlayerPosition != Vector3.zero))
        {
            Debug.Log($"[RobotAI] Moderate general noise ({playerNoiseLevel:F2}) or medium aggression ({currentAggression:F2}), entering INVESTIGATE state.");
            return AIState.Investigate;
        }

        return AIState.Patrol;
    }

    void OnStateChange(AIState newState)
    {
        if (newState != currentState) // Убедимся, что состояние действительно меняется
        {
            // 1. Звук начала погони (самый высокий приоритет)
            if (newState == AIState.Chase)
            {
                PlayStateSound(onStartChaseSound);
            }
            // 2. Звук тревоги (когда переходит из спокойного состояния в активное)
            else if ((newState == AIState.Investigate || newState == AIState.Hunt) && currentState == AIState.Patrol)
            {
                PlayStateSound(onBecomeAlertSound);
            }
            // 3. Звук успокоения (когда из агрессивного состояния возвращается в патруль)
            else if (newState == AIState.Patrol && (currentState == AIState.Chase || currentState == AIState.Hunt))
            {
                PlayStateSound(onCalmDownSound);
            }
        }
        Debug.Log($"[RobotAI] State changed from {currentState} to {newState}");
        currentState = newState;
        stateTimer = 0f;
        isCheckingHidingSpot = false;
        stuckTimer = 0f;

        if (currentSearchCoroutine != null)
        {
            StopCoroutine(currentSearchCoroutine);
            currentSearchCoroutine = null;
            Debug.Log("[RobotAI] Stopped previous search coroutine due to state change.");
        }

        switch (newState)
        {
            case AIState.Patrol:
                agent.speed = patrolSpeed;
                _maxCurrentAgentSpeed = patrolSpeed;
                SetRandomPatrolDestination();
                break;

            case AIState.Investigate:
                if (currentState == AIState.Hunt || currentState == AIState.Chase)
                {
                    currentAggression = Mathf.Clamp(currentAggression * 0.7f, baseAggression, 0.6f);
                    Debug.Log($"[RobotAI] Агрессия снижена до {currentAggression.ToString("F2")} для методичного поиска.");
                }
                agent.speed = huntSpeed;
                _maxCurrentAgentSpeed = huntSpeed;
                if (lastKnownPlayerPosition != Vector3.zero)
                {
                    SetSmartDestination(lastKnownPlayerPosition);
                }
                else
                {
                    SetRandomPatrolDestination();
                    currentState = AIState.Patrol;
                    Debug.LogWarning("[RobotAI] Investigate state entered but no known player position, returning to Patrol.");
                }
                break;

            case AIState.Hunt:
                agent.speed = huntSpeed;
                _maxCurrentAgentSpeed = huntSpeed;
                if (prioritizedHidingSpot != null)
                {
                    Debug.Log($"[RobotAI] Hunt: Запускаю прямую проверку укрытия {prioritizedHidingSpot.name}.");
                    currentSearchCoroutine = StartCoroutine(Coroutine_CheckHidingSpot(prioritizedHidingSpot));
                }
                else if (lastKnownPlayerRoom != null)
                {
                    Debug.Log($"[RobotAI] Hunt: Запускаю поиск в комнате {lastKnownPlayerRoom.name}.");
                    currentSearchCoroutine = StartCoroutine(SearchRoom(lastKnownPlayerRoom));
                }
                else
                {
                    Debug.Log($"[RobotAI] Hunt: Запускаю поиск в области {lastKnownPlayerPosition}.");
                    currentSearchCoroutine = StartCoroutine(SearchArea(lastKnownPlayerPosition));
                }
                break;

            case AIState.Chase:
                agent.speed = chaseSpeed;
                _maxCurrentAgentSpeed = chaseSpeed;
                chaseLostPlayerTimestamp = -1f;
                // Destination устанавливается в ExecuteCurrentState
                break;
        }
        StartCoroutine(DelayedUpdateAnimatorSpeed()); // Вызов здесь, после установки agent.speed
    }

    void ExecuteCurrentState()
    {
        if (isExecutingMandatorySearch || _playerCaught) // Добавлена проверка на _playerCaught
        {
            // Если мы выполняем принудительный поиск (от приманки/тревоги),
            // мы полностью приостанавливаем обычную логику состояний,
            // чтобы она не мешала корутине поиска.
            return; // Немедленно выходим из функции
        }
        switch (currentState)
        {
            case AIState.Patrol:
                if (!agent.pathPending && agent.remainingDistance < agent.stoppingDistance + 0.5f)
                {
                    // Если мы выполняли многоточечный патруль в большой зоне
                    if (isPerformingLocalPatrol)
                    {
                        localPatrolPointsVisited++;
                        if (localPatrolPointsVisited >= maxLocalPatrolPoints)
                        {
                            // Мы закончили патрулировать. Теперь пора выбирать новую комнату.
                            // Проверка на null здесь тоже не помешает для надежности.
                            string roomName = (currentRoom != null) ? currentRoom.name : "неизвестной зоне";
                            Debug.Log($"[RobotAI] Локальный патруль в '{roomName}' завершен. Выбираю новую глобальную цель.");
                            PickNextDestinationRoom();
                        }
                        else
                        {
                            // Продолжаем патрулировать эту же большую зону.
                            SetLocalPatrolPoint();
                        }
                    }
                    else // Если это был не многоточечный патруль
                    {
                        // --- НАЧАЛО ИСПРАВЛЕНИЯ ---

                        // ПРОВЕРЯЕМ, определена ли текущая комната.
                        if (currentRoom != null)
                        {
                            // Если да, значит, мы достигли точки в маленькой комнате. Пора уходить.
                            Debug.Log($"[RobotAI] Достиг точки в '{currentRoom.name}'. Теперь выбираю новую глобальную цель.");
                        }
                        else
                        {
                            // Если currentRoom == null, это значит, что мы только что начали игру
                            // и достигли самой первой точки.
                            Debug.Log("[RobotAI] Достиг начальной точки. Выбираю первую глобальную цель.");
                        }

                        // В любом из этих случаев, следующим шагом будет выбор новой комнаты.
                        PickNextDestinationRoom();

                        // --- КОНЕЦ ИСПРАВЛЕНИЯ ---
                    }
                }
                StartCoroutine(DelayedUpdateAnimatorSpeed());
                break;

            case AIState.Investigate:
                if (agent.remainingDistance < agent.stoppingDistance + 0.5f)
                {
                    if (playerVisibility > 0.1f) return;

                    if (!isCheckingHidingSpot && timeSinceLastPlayerContact < 8f && currentAggression > 0.4f)
                    {
                        Debug.Log($"[RobotAI] Investigate: Reached last known position, considering nearby hiding spots (aggression: {currentAggression:F2}).");
                        currentSearchCoroutine = StartCoroutine(CheckNearbyHidingSpots(lastKnownPlayerPosition));
                    }
                    else if (stateTimer > 8f)
                    {
                        Debug.Log("[RobotAI] Investigate: Timed out, returning to Patrol.");
                        currentState = AIState.Patrol;
                        OnStateChange(AIState.Patrol);
                    }
                }
                StartCoroutine(DelayedUpdateAnimatorSpeed());
                break;

            case AIState.Chase:
                // Если мы видим игрока - всё просто. Преследуем его текущую позицию.
                if (playerVisibility > 0.1f && player != null && !player.isHiding)
                {
                    // ...просто преследуем его и сбрасываем нашу "засечку времени",
                    // подтверждая, что контакт есть.
                    chaseLostPlayerTimestamp = -1f;
                    SetSmartDestination(player.transform.position);
                }
                // Если мы игрока НЕ видим...
                else
                {
                    // Проверяем, была ли уже сделана "засечка".
                    // Если нет (значение все еще -1), значит, это ПЕРВЫЙ кадр, как мы потеряли игрока.
                    if (chaseLostPlayerTimestamp < 0f)
                    {
                        // Делаем засечку текущего игрового времени.
                        chaseLostPlayerTimestamp = Time.time;
                        Debug.Log($"[RobotAI] Chase: Контакт потерян! Засек время: {chaseLostPlayerTimestamp:F1}");
                    }

                    // Теперь вычисляем, сколько времени прошло с момента потери контакта.
                    float timeSinceLost = Time.time - chaseLostPlayerTimestamp;

                    // Если прошло меньше времени, чем наша "настойчивость"...
                    if (timeSinceLost < chasePersistenceTime)
                    {
                        // ...продолжаем погоню к последней известной точке.
                        Debug.Log($"[RobotAI] Chase: Продолжаю погоню вслепую. Прошло {timeSinceLost:F1} из {chasePersistenceTime} сек.");
                        SetSmartDestination(lastKnownPlayerPosition);
                    }
                    // А если прошло больше времени...
                    else
                    {
                        // ...только тогда сдаемся и переходим в режим Охоты.
                        Debug.Log($"[RobotAI] Chase: Погоня вслепую не дала результатов ({timeSinceLost:F1} сек). Перехожу в Hunt.");
                        OnStateChange(AIState.Hunt);
                    }
                }

                // Общее ограничение по времени на погоню остается на всякий случай
                if (stateTimer > maxChaseTime)
                {
                    Debug.Log($"[RobotAI] Chase: Погоня длится слишком долго. Принудительно перехожу в Hunt.");
                    OnStateChange(AIState.Hunt);
                }

                StartCoroutine(DelayedUpdateAnimatorSpeed());
                break;

            case AIState.Hunt:
                if (stateTimer > maxHuntTime)
                {
                    Debug.Log("[RobotAI] Max hunt time exceeded, reverting to Patrol.");
                    OnStateChange(AIState.Investigate);
                    break;
                }
                else if (currentSearchCoroutine == null && !isCheckingHidingSpot)
                {
                    Debug.LogWarning("[RobotAI] HUNT STATE: Search coroutine was null. Starting a new search for safety.");
                    if (lastKnownPlayerRoom != null)
                    {
                        currentSearchCoroutine = StartCoroutine(SearchRoom(lastKnownPlayerRoom));
                    }
                    else
                    {
                        currentSearchCoroutine = StartCoroutine(SearchArea(lastKnownPlayerPosition));
                    }
                }
                StartCoroutine(DelayedUpdateAnimatorSpeed());
                break;
        }
    }

    public void ReactToLure(Vector3 lurePosition, float lureSoundRadius)
    {
        if (_playerCaught) return; // Добавлена проверка на _playerCaught

        float distanceToLure = Vector3.Distance(transform.position, lurePosition);
        if (distanceToLure > lureSoundRadius || currentState == AIState.Chase || playerVisibility > 0.3f)
        {
            return;
        }

        Debug.Log($"[RobotAI] Реагирую на приманку в {lurePosition}.");

        // Если уже выполняется какая-то корутина, останавливаем ее.
        if (currentSearchCoroutine != null)
        {
            StopCoroutine(currentSearchCoroutine);
        }

        // Меняем состояние для правильной скорости и анимации
        OnStateChange(AIState.Investigate);

        // Запускаем нашу НОВУЮ, простую и надежную корутину
        currentSearchCoroutine = StartCoroutine(GoInvestigatePoint(lurePosition));

        // Код для гизмо
        _lureGizmoPosition = lurePosition;
        _lureGizmoRadius = lureSoundRadius;
        _lureGizmoTimer = lureGizmoDisplayDuration;
    }

    public void ReactToCameraAlarm(Vector3 alarmPosition, float aggressionBoost)
    {
        if (_playerCaught) return; // Добавлена проверка на _playerCaught

        if (playerVisibility > 0.3f || currentState == AIState.Chase)
        {
            return;
        }

        Debug.Log($"[RobotAI] Реагирую на тревогу камеры в {alarmPosition}.");

        if (currentSearchCoroutine != null)
        {
            StopCoroutine(currentSearchCoroutine);
        }

        currentAggression = Mathf.Min(1.0f, currentAggression + aggressionBoost);
        OnStateChange(AIState.Hunt); // Можно оставить Hunt для более высокой скорости

        // Запускаем нашу НОВУЮ корутину
        currentSearchCoroutine = StartCoroutine(GoInvestigatePoint(alarmPosition));

        // Код для гизмо
        _cameraAlarmPosition = alarmPosition;
        _cameraAlarmTimer = cameraAlarmGizmoDisplayDuration;
    }

    IEnumerator SearchRoom(Room room)
    {
        Debug.Log($"[RobotAI] SearchRoom: Starting a quick search in room '{room.name}'.");

        if (Vector3.Distance(room.transform.position, lastKnownPlayerPosition) < 15f && timeSinceLastPlayerContact < 10f)
        {
            SetSmartDestination(lastKnownPlayerPosition);
            StartCoroutine(DelayedUpdateAnimatorSpeed());
            yield return new WaitUntil(() => agent.remainingDistance < agent.stoppingDistance + 0.5f || currentState != AIState.Hunt || playerVisibility > 0.1f || _playerCaught); // Добавлена проверка на _playerCaught

            if (currentState != AIState.Hunt || playerVisibility > 0.1f || _playerCaught) // Добавлена проверка на _playerCaught
            {
                Debug.Log("[RobotAI] SearchRoom: Interrupted by new visual contact or player caught.");
                currentSearchCoroutine = null;
                yield break;
            }
        }

        List<HidingSpot> uncheckedSpots = GetHidingSpotsInRoom(room).FindAll(spot => !spot.wasRecentlyChecked);

        if (uncheckedSpots.Count > 0)
        {
            HidingSpot spotToCheck = uncheckedSpots[Random.Range(0, uncheckedSpots.Count)];
            Debug.Log($"[RobotAI] SearchRoom: Decided to check one hiding spot: {spotToCheck.name}.");

            yield return StartCoroutine(Coroutine_CheckHidingSpot(spotToCheck));

            if (Time.timeScale == 0f || _playerCaught) // Добавлена проверка на _playerCaught
            {
                currentSearchCoroutine = null;
                yield break;
            }
        }
        else
        {
            Debug.Log($"[RobotAI] SearchRoom: No unchecked hiding spots in room '{room.name}'.");
            yield return new WaitForSeconds(1f);
        }

        Debug.Log("[RobotAI] Quick room search finished. Player not found. Reverting to Patrol.");
        currentAggression = baseAggression;
        OnStateChange(AIState.Patrol);
        PickNextDestinationRoom();
        currentSearchCoroutine = null;
    }

    IEnumerator SearchArea(Vector3 areaCenterPosition)
    {
        try
        {
            if (lastKnownPlayerPosition != Vector3.zero && timeSinceLastPlayerContact < 10f)
            {
                Debug.Log($"[RobotAI] SearchArea: Going to last known player position {lastKnownPlayerPosition} first.");
                SetSmartDestination(lastKnownPlayerPosition);
                StartCoroutine(DelayedUpdateAnimatorSpeed());
                yield return new WaitUntil(() => agent.remainingDistance < agent.stoppingDistance + 0.5f || stateTimer > 5f || currentState != AIState.Hunt || (player != null && player.isRunning && playerNoiseLevel > 0.6f) || playerVisibility > 0.3f || _playerCaught); // Добавлена проверка на _playerCaught

                if (currentState != AIState.Hunt || playerVisibility > 0.1f || (player != null && player.isRunning && playerNoiseLevel > 0.6f) || _playerCaught) // Добавлена проверка на _playerCaught
                {
                    Debug.Log("[RobotAI] SearchArea: Interrupted due to new threat or player caught.");
                    currentSearchCoroutine = null;
                    yield break;
                }

                if (agent.remainingDistance < agent.stoppingDistance + 0.5f)
                {
                    if (timeSinceLastPlayerContact < 5f && currentAggression > 0.4f)
                    {
                        Debug.Log("[RobotAI] SearchArea: Player not at last known position, but contact fresh, checking nearby hiding spots.");
                        currentSearchCoroutine = StartCoroutine(CheckNearbyHidingSpots(lastKnownPlayerPosition));
                        yield break;
                    }
                }
            }

            Debug.Log($"[RobotAI] SearchArea: Performing random sweeps in area around {areaCenterPosition}.");
            for (int i = 0; i < 3; i++)
            {
                if (currentState != AIState.Hunt && currentState != AIState.Investigate || _playerCaught) // Добавлена проверка на _playerCaught
                {
                    currentSearchCoroutine = null;
                    yield break;
                }

                Vector3 randomPoint = areaCenterPosition + Random.insideUnitSphere * 7f;
                randomPoint.y = areaCenterPosition.y;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomPoint, out hit, 7f, NavMesh.AllAreas))
                {
                    SetSmartDestination(hit.position);
                    StartCoroutine(DelayedUpdateAnimatorSpeed());
                    yield return new WaitUntil(() => agent.remainingDistance < agent.stoppingDistance + 0.5f || stateTimer > 5f || currentState != AIState.Hunt || (player != null && player.isRunning && playerNoiseLevel > 0.6f) || playerVisibility > 0.3f || _playerCaught); // Добавлена проверка на _playerCaught

                    if (currentState != AIState.Hunt || playerVisibility > 0.1f || (player != null && player.isRunning && playerNoiseLevel > 0.6f) || _playerCaught) // Добавлена проверка на _playerCaught
                    {
                        Debug.Log("[RobotAI] SearchArea (Random Point): Interrupted due to new threat or player caught.");
                        currentSearchCoroutine = null;
                        yield break;
                    }
                }
            }

            if ((currentState == AIState.Hunt || currentState == AIState.Investigate) && !_playerCaught) // Добавлена проверка на _playerCaught
            {
                Debug.Log("[RobotAI] Area search completed, player not found. Reducing aggression and patrolling.");
                currentAggression = Mathf.Max(baseAggression, currentAggression - 0.2f);
                currentState = AIState.Patrol;
                OnStateChange(AIState.Patrol);
            }
        }
        finally
        {
            isExecutingMandatorySearch = false;
            currentSearchCoroutine = null;
        }

    }

    /// <summary>
    /// "Умный" метод для установки пункта назначения. Проверяет, доступна ли цель напрямую.
    /// Если нет, пытается найти блокирующую дверь и проложить маршрут к ней.
    /// </summary>
    /// <param name="targetPosition">Конечная точка, куда должен прийти ИИ.</param>
    public void SetSmartDestination(Vector3 targetPosition)
    {
        // Сбрасываем флаги перед новой задачей
        _finalDestination = Vector3.zero;
        _isPathfindingToDoor = false;

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("[RobotAI] SetSmartDestination: Agent is not on NavMesh.");
            return;
        }

        NavMeshPath path = new NavMeshPath();
        // Пытаемся рассчитать путь до конечной цели
        if (agent.CalculatePath(targetPosition, path) && path.status == NavMeshPathStatus.PathComplete)
        {
            // Путь свободен! Просто идем к цели.
            agent.SetPath(path);
            Debug.Log($"[RobotAI] Путь к {targetPosition} свободен. Следую напрямую.");
        }
        else
        {
            // Путь заблокирован или неполный. Вероятно, дело в двери.
            Debug.Log($"[RobotAI] Прямой путь к {targetPosition} заблокирован. Ищу дверь...");
            SlidingDoor blockingDoor = FindBlockingDoor(targetPosition);

            if (blockingDoor != null)
            {
                // Нашли дверь! Наша временная цель - это она.
                Debug.Log($"[RobotAI] Нашел блокирующую дверь: {blockingDoor.name}. Иду к ней.");
                _finalDestination = targetPosition; // Запоминаем, куда мы хотели попасть изначально
                _isPathfindingToDoor = true;
                agent.SetDestination(blockingDoor.transform.position); // Идем к двери
            }
            else
            {
                // Дверь не нашли, но путь все равно заблокирован.
                // Идем в самую дальнюю точку, до которой смогли построить маршрут.
                Debug.LogWarning($"[RobotAI] Не смог найти блокирующую дверь. Иду в конец неполного пути.");
                if (path.corners.Length > 0)
                {
                    agent.SetDestination(path.corners[path.corners.Length - 1]);
                }
            }
        }
        StartCoroutine(DelayedUpdateAnimatorSpeed());
    }

    /// <summary>
    /// Находит закрытую дверь, которая находится между ИИ и его целью.
    /// </summary>
    private SlidingDoor FindBlockingDoor(Vector3 targetPosition)
    {
        if (allDoors == null || allDoors.Length == 0)
        {
            return null;
        }
        SlidingDoor closestBlockingDoor = null;
        float minDistance = float.MaxValue;

        foreach (var door in allDoors)
        {
            if (door != null && door.isClosed && door.IsPointBehindDoor(transform.position, targetPosition))
            {
                float distanceToDoor = Vector3.Distance(transform.position, door.transform.position);
                if (distanceToDoor < minDistance)
                {
                    minDistance = distanceToDoor;
                    closestBlockingDoor = door;
                }
            }
        }
        return closestBlockingDoor;
    }

    IEnumerator CheckNearbyHidingSpots(Vector3 searchCenter)
    {
        isCheckingHidingSpot = true;
        Debug.Log("[RobotAI] Checking one nearby hiding spot.");

        List<HidingSpot> nearbySpots = GetNearbyHidingSpots(searchCenter, 10f);
        List<HidingSpot> uncheckedSpots = nearbySpots.FindAll(spot => !spot.wasRecentlyChecked);

        if (uncheckedSpots.Count > 0)
        {
            // Сортируем споты по реальной дистанции пути, а не по прямой
            uncheckedSpots.Sort((a, b) => {
                NavMeshPath pathA = new NavMeshPath();
                NavMeshPath pathB = new NavMeshPath();
                float distA = float.MaxValue;
                float distB = float.MaxValue;

                if (NavMesh.CalculatePath(transform.position, a.monsterInteractionPoint.position, NavMesh.AllAreas, pathA))
                {
                    distA = GetPathLength(pathA);
                }
                if (NavMesh.CalculatePath(transform.position, b.monsterInteractionPoint.position, NavMesh.AllAreas, pathB))
                {
                    distB = GetPathLength(pathB);
                }
                return distA.CompareTo(distB);
            });

            HidingSpot spotToCheck = uncheckedSpots[0]; // Берем самый близкий по пути
            yield return StartCoroutine(Coroutine_CheckHidingSpot(spotToCheck));
        }
        else
        {
            Debug.Log("[RobotAI] No unchecked hiding spots nearby to investigate.");
        }

        isCheckingHidingSpot = false;
        Debug.Log("[RobotAI] Finished checking nearby spot. Reverting to Patrol.");
        currentAggression = baseAggression;
        OnStateChange(AIState.Patrol);
        currentSearchCoroutine = null;
        Debug.Log("[RobotAI] Поиск укрытий в этой области завершен. Покидаю зону.");
        OnStateChange(AIState.Patrol);      // 1. Сначала переходим в состояние Патруля.
        PickNextDestinationRoom();          // 2. СРАЗУ ЖЕ выбираем НОВУЮ комнату и уходим туда.
    }

    float GetPathLength(NavMeshPath path)
    {
        float length = 0.0f;
        for (int i = 1; i < path.corners.Length; i++)
        {
            length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }
        return length;
    }

    IEnumerator Coroutine_CheckHidingSpot(HidingSpot spot)
    {
        isCheckingHidingSpot = true;
        Debug.Log($"[RobotAI] Starting check of hiding spot: {spot.name} (Type: {spot.type})");
        if (spot == prioritizedHidingSpot)
        {
            prioritizedHidingSpot = null;
            memoryOfHidingSpotTimer = 0;
        }

        SetSmartDestination(spot.monsterInteractionPoint.position);
        StartCoroutine(DelayedUpdateAnimatorSpeed());
        yield return new WaitUntil(() => agent.remainingDistance < agent.stoppingDistance + 0.5f || (currentState != AIState.Investigate && currentState != AIState.Hunt) || playerVisibility > 0.1f || _playerCaught); // Добавлена проверка на _playerCaught

        if ((currentState != AIState.Investigate && currentState != AIState.Hunt) || playerVisibility > 0.1f || _playerCaught) // Добавлена проверка на _playerCaught
        {
            Debug.Log("[RobotAI] CheckingHidingSpots: Interrupted by new threat or player caught.");
            if (agent.isOnNavMesh) agent.isStopped = false;
            isCheckingHidingSpot = false;
            yield break;
        }
        float distanceToInteractionPoint = Vector3.Distance(transform.position, spot.monsterInteractionPoint.position);
        if (distanceToInteractionPoint > 3.0f) // Если мы дальше 3 метров от цели
        {
            Debug.LogWarning($"[RobotAI] ABORTING CHECK: Failed to get close to {spot.name}. Distance is {distanceToInteractionPoint:F1}m. The spot might be unreachable.");
            if (agent.isOnNavMesh) agent.isStopped = false;
            isCheckingHidingSpot = false;
            yield break; // Прерываем проверку
        }

        agent.isStopped = true;
        StartCoroutine(DelayedUpdateAnimatorSpeed());

        yield return StartCoroutine(Coroutine_FaceTarget(spot.monsterInteractionPoint.forward, 360f));

        switch (spot.type)
        {
            case HidingSpot.HidingType.Cabinet:
                robotAnimator.TriggerOpenDoor(); // Используем новый скрипт
                spot.MonsterOpenDoor();
                yield return new WaitForSeconds(doorInteractionTime);

                robotAnimator.TriggerCheckCab(); // Используем новый скрипт
                Debug.Log("[RobotAI] Playing 'CheckCab' animation for Cabinet check.");
                yield return new WaitForSeconds(checkAnimationDuration);

                robotAnimator.TriggerCloseDoor(); // Используем новый скрипт
                spot.MonsterCloseDoor();
                yield return new WaitForSeconds(doorInteractionTime);
                break;

            case HidingSpot.HidingType.Underneath:
            case HidingSpot.HidingType.Chest:
                robotAnimator.TriggerSitDown(); // Используем новый скрипт
                Debug.Log("[RobotAI] Playing 'SitDown' animation for Underneath/Chest check.");
                yield return new WaitForSeconds(checkAnimationDuration);

                Debug.Log("[RobotAI] Robot is inspecting while sitting.");
                yield return new WaitForSeconds(1.0f);

                robotAnimator.TriggerStandUp(); // Используем новый скрипт
                Debug.Log("[RobotAI] Playing 'StandUp' animation after check.");
                yield return new WaitForSeconds(checkAnimationDuration);
                break;
        }

        if (player != null && player.isHiding && player.currentHidingSpot == spot)
        {
            CatchPlayer();
            yield break;
        }

        spot.MarkAsChecked();
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            StartCoroutine(DelayedUpdateAnimatorSpeed());
        }
        isCheckingHidingSpot = false;
    }

    IEnumerator Coroutine_FaceTarget(Vector3 direction, float turnSpeed)
    {
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        float time = 0;

        while (time < 1)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, time);
            time += Time.deltaTime * turnSpeed / 360f;
            yield return null;
        }
        transform.rotation = lookRotation;
    }

    List<HidingSpot> GetHidingSpotsInRoom(Room room)
    {
        return GetNearbyHidingSpots(room.transform.position, 15f);
    }

    List<HidingSpot> GetNearbyHidingSpots(Vector3 position, float radius)
    {
        List<HidingSpot> nearbySpots = new List<HidingSpot>();
        NavMeshPath path = new NavMeshPath(); // Создаем один объект пути для переиспользования

        foreach (HidingSpot spot in hidingSpots)
        {
            if (spot == null || spot.monsterInteractionPoint == null) continue;

            // 1. Простая проверка на дистанцию, чтобы отсечь совсем далекие объекты
            if (Vector3.Distance(position, spot.transform.position) > radius)
            {
                continue;
            }

            // 2. ГЛАВНАЯ ПРОВЕРКА: может ли NavMesh агент вообще построить путь до точки взаимодействия?
            if (NavMesh.CalculatePath(transform.position, spot.monsterInteractionPoint.position, NavMesh.AllAreas, path))
            {
                // Если статус пути "PathComplete", значит, путь найден и он полный.
                if (path.status == NavMeshPathStatus.PathComplete)
                {
                    nearbySpots.Add(spot);
                }
            }
        }

        return nearbySpots;
    }

    Room FindClosestRoom(Vector3 position)
    {
        Room closestRoom = null;
        float closestDistance = Mathf.Infinity;

        foreach (Room room in rooms)
        {
            float distance = Vector3.Distance(position, room.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestRoom = room;
            }
        }

        return closestRoom;
    }

    void SetupMonsterCollider()
    {
        if (monsterCollider != null)
        {
            monsterCollider.isTrigger = false;
        }
    }

    void SetRandomPatrolDestination()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * 20f;
            randomDirection += transform.position;
            randomDirection.y = transform.position.y;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, 20f, NavMesh.AllAreas))
            {
                SetSmartDestination(hit.position);
                Debug.Log($"[RobotAI] Setting new patrol destination: {hit.position}");
                StartCoroutine(DelayedUpdateAnimatorSpeed());
                return;
            }
        }
        Debug.LogWarning("[RobotAI] Failed to find a random patrol destination after multiple attempts.");
    }

    void FindAllHidingSpots()
    {
        GameObject[] hidingSpotObjects = GameObject.FindGameObjectsWithTag("HidingSpot");
        foreach (GameObject spot in hidingSpotObjects)
        {
            HidingSpot hidingSpot = spot.GetComponent<HidingSpot>();
            if (hidingSpot != null)
            {
                hidingSpots.Add(hidingSpot);
            }
        }
        Debug.Log($"[RobotAI] Found {hidingSpots.Count} hiding spots.");
    }

    void FindAllRooms()
    {
        GameObject[] roomObjects = GameObject.FindGameObjectsWithTag("Room");
        foreach (GameObject roomObj in roomObjects)
        {
            Room room = roomObj.GetComponent<Room>();
            if (room != null)
            {
                rooms.Add(room);
            }
        }
        Debug.Log($"[RobotAI] Found {rooms.Count} rooms.");
    }

    /// <summary>
    /// Измененный метод поимки игрока для кинематографичной смерти от первого лица игрока.
    /// </summary>
    public void CatchPlayer()
    {
        if (_playerCaught) return;
        _playerCaught = true;

        Debug.Log("MonsterAI: Player caught! Initiating cinematic death sequence (First-person perspective, player turns to monster).");

        // 1. Блокируем управление игрока и перемещение монстра
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        if (player != null)
        {
            player.SetMovementEnabled(false);
            player.SetLookEnabled(false);
            // Если есть ScreenDangerWarningState у игрока, можно отключить
            // player.SetScreenDangerWarningState(false);
        }

        // 2. Запускаем корутину для последовательности событий
        StartCoroutine(CinematicDeathSequenceFirstPerson());
    }

    private IEnumerator CinematicDeathSequenceFirstPerson()
    {
        // Убедимся, что монстр активен и виден
        monsterModel.SetActive(true);

        // Отключаем навигационный агент, чтобы монстр оставался на месте
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero; // Останавливаем любое движение
        }

        if (player != null && player.playerCamera != null)
        {
            // Определяем точку на монстре, на которую игрок должен смотреть
            Vector3 monsterLookPoint = transform.position;
            // Устанавливаем высоту точки взгляда на монстра, чтобы игрок смотрел на нужную часть монстра
            monsterLookPoint.y = transform.position.y + playerLookAtHeight;

            // Направление от камеры игрока к монстру
            Vector3 directionToMonster = (monsterLookPoint - player.playerCamera.transform.position).normalized;

            // Вычисляем целевой поворот для камеры игрока
            Quaternion targetPlayerCameraRotation = Quaternion.LookRotation(directionToMonster);

            // Получаем текущий поворот камеры игрока
            Quaternion currentPlayerCameraRotation = player.playerCamera.transform.rotation;

            float turnDuration = 0.3f; // Время, за которое игрок поворачивается
            float elapsed = 0f;

            // Плавный поворот камеры игрока
            while (elapsed < turnDuration)
            {
                player.playerCamera.transform.rotation = Quaternion.Slerp(currentPlayerCameraRotation, targetPlayerCameraRotation, elapsed / turnDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            player.playerCamera.transform.rotation = targetPlayerCameraRotation;


            // Опционально: если монстр слишком близко, отодвинуть игрока назад
            float distanceToMonster = Vector3.Distance(player.transform.position, transform.position);
            if (distanceToMonster < minDistanceForFPCatch) // Игрок СЛИШКОМ БЛИЗКО
            {
                Debug.Log($"MonsterAI: Monster is too close ({distanceToMonster:F1}m), moving player back.");

                // Направление от монстра к игроку
                Vector3 directionFromMonsterToPlayer = (player.transform.position - transform.position).normalized;

                // Целевая позиция игрока: от монстра на расстоянии minDistanceForFPCatch
                Vector3 playerMoveTarget = transform.position + directionFromMonsterToPlayer * minDistanceForFPCatch;
                playerMoveTarget.y = player.transform.position.y; // Сохраняем высоту игрока

                Vector3 playerStartPos = player.transform.position;
                elapsed = 0f;
                float playerMoveDuration = 0.2f; // Время на перемещение игрока

                while (elapsed < playerMoveDuration)
                {
                    player.transform.position = Vector3.Lerp(playerStartPos, playerMoveTarget, elapsed / playerMoveDuration);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                player.transform.position = playerMoveTarget;
            }
        }
        else
        {
            Debug.LogWarning("MonsterAI: PlayerController or Player Camera not found/assigned. Player will not turn to monster.");
        }

        // Ожидание перед анимацией
        yield return new WaitForSeconds(preAnimationDelay);

        // Проигрываем анимацию удара
        PlayPunchAnimation();

        // Ожидаем завершения анимации или заданное время
        yield return new WaitForSeconds(postAnimationDelay);

        // Показываем панель Game Over
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (deathText != null)
            {
                deathText.text = "Game Over!\nYou were caught by the monster.";
                deathText.gameObject.SetActive(true);
            }
        }
        else
        {
            Debug.LogWarning("MonsterAI: Game Over Panel is not assigned. Player will still be caught, but no UI will show.");
            if (deathText != null) // Если нет панели, но есть текст
            {
                deathText.text = "Game Over!\nYou were caught by the monster.";
                deathText.gameObject.SetActive(true);
            }
        }

        // Запускаем перезапуск сцены с задержкой
        Invoke("RestartScene", restartSceneDelay);
    }


    private void RestartScene()
    {
        Debug.Log("MonsterAI: Restarting scene.");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void PlayPunchAnimation()
    {
        if (robotAnimator != null)
        {
            robotAnimator.TriggerPunch();
            Debug.Log("MonsterAI: Playing Punch animation.");
        }
        else
        {
            Debug.LogWarning("MonsterAI: RobotAnimationController not assigned, cannot play punch animation.");
        }
    }

    public void StopPunchAnimation()
    {
        if (robotAnimator != null)
        {
            // Предполагаем, что RobotAnimationController имеет метод для сброса триггера Punch
            // Или для возврата в idle состояние. Здесь нужна специфическая реализация.
            Debug.Log("MonsterAI: Stopping Punch animation (if applicable).");
        }
        else
        {
            Debug.LogWarning("MonsterAI: RobotAnimationController not assigned, cannot stop punch animation.");
        }
    }


    void OnDrawGizmos()
    {
        if (!Application.isPlaying || monsterModel == null) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        float halfAngle = visionAngle / 2f;
        int segments = 20;
        float segmentAngle = visionAngle / segments;
        Vector3 forward = transform.forward;
        Vector3 startPoint = transform.position + Vector3.up * 0.5f;

        for (int i = 0; i <= segments; i++)
        {
            float angle = -halfAngle + segmentAngle * i;
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 direction = rotation * forward;
            Vector3 endPoint = startPoint + direction * visionRange;
            Gizmos.DrawLine(startPoint, endPoint);
            if (i > 0)
            {
                float prevAngle = -halfAngle + segmentAngle * (i - 1);
                Quaternion prevRotation = Quaternion.AngleAxis(prevAngle, Vector3.up);
                Vector3 prevDirection = prevRotation * forward;
                Vector3 prevEndPoint = startPoint + prevDirection * visionRange;
                Gizmos.DrawLine(prevEndPoint, endPoint);
            }
        }

        Gizmos.color = GetStateColor();
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);

        if (lastKnownPlayerPosition != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(lastKnownPlayerPosition, 0.5f);
        }

        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.blue;
            Vector3[] pathCorners = agent.path.corners;
            for (int i = 0; i < pathCorners.Length - 1; i++)
            {
                Gizmos.DrawLine(pathCorners[i], pathCorners[i + 1]);
            }
        }

        if (_lureGizmoTimer > 0f && _lureGizmoRadius > 0f)
        {
            Gizmos.color = new Color(0f, 1f, 1f, _lureGizmoTimer / lureGizmoDisplayDuration);
            Gizmos.DrawWireSphere(_lureGizmoPosition, _lureGizmoRadius);
            Gizmos.DrawSphere(_lureGizmoPosition, 0.2f);
        }

        if (_cameraAlarmTimer > 0f)
        {
            Gizmos.color = new Color(1f, 0f, 0f, _cameraAlarmTimer / cameraAlarmGizmoDisplayDuration);
            Gizmos.DrawWireSphere(_cameraAlarmPosition, 1f);
            Gizmos.DrawLine(_cameraAlarmPosition + Vector3.up * 0.5f, _cameraAlarmPosition + Vector3.up * 2f);
        }
    }

    IEnumerator HandleDoorInteraction(SlidingDoor door)
    {
        KeycardDoor keycardDoor = door.GetComponent<KeycardDoor>();
        if (keycardDoor != null && !keycardDoor.unlocked)
        {
            Debug.Log($"[RobotAI] Дверь {door.name} заперта (требуется ключ-карта). Ищу другой путь.");
            // Мы не будем пытаться ее открыть. Можно добавить логику, чтобы монстр
            // на время пометил эту дверь как непроходимую, но пока просто выйдем.
            isInteractingWithDoor = false; // Сбрасываем флаг
            agent.isStopped = true;
            StartCoroutine(DelayedUpdateAnimatorSpeed()); // Обновляем анимацию на idle

            Debug.Log($"[RobotAI] Остановка у запертой двери на {lockedDoorPauseDuration} сек...");
            yield return new WaitForSeconds(lockedDoorPauseDuration); // Ждем 2 секунды

            OnBlockedPathDetected();
            agent.isStopped = false;

            yield break; // Прерываем выполнение корутины
        }

        PuzzleDependentDoor puzzleDoor = door.GetComponent<PuzzleDependentDoor>();
        if (puzzleDoor != null && !puzzleDoor.IsCurrentlyUnlocked)
        {
            Debug.Log($"[RobotAI] Дверь {door.name} заперта (требуется решение головоломки). Ищу другой путь.");
            isInteractingWithDoor = false;
            agent.isStopped = true;
            StartCoroutine(DelayedUpdateAnimatorSpeed()); // Обновляем анимацию на idle

            Debug.Log($"[RobotAI] Остановка у запертой двери на {lockedDoorPauseDuration} сек...");
            yield return new WaitForSeconds(lockedDoorPauseDuration); // Ждем 2 секунды

            OnBlockedPathDetected();
            agent.isStopped = false;

            yield break; // Прерываем выполнение
        }


        isInteractingWithDoor = true;
        if (agent.hasPath)
        {
            agent.isStopped = true;
            StartCoroutine(DelayedUpdateAnimatorSpeed());
        }
        Debug.Log($"[RobotAI] Остановился перед дверью {door.name}. Пытаюсь открыть.");

        robotAnimator.TriggerOpenDoor(); // Используем RobotAnimationController

        door.OpenDoor();

        float waitTimer = 0f;
        float maxWaitTime = 2.0f;

        while (door.isClosed && waitTimer < maxWaitTime)
        {
            waitTimer += Time.deltaTime;
            yield return null;
        }

        if (door.isClosed)
        {
            Debug.Log($"[RobotAI] Дверь {door.name} не открылась. Считаю её запертой и ищу другой путь.");
            OnBlockedPathDetected();
        }
        else
        {
            Debug.Log($"[RobotAI] Дверь {door.name} открыта. Возвращаю управление главному циклу.");
            // --- НАЧАЛО НОВОЙ, УПРОЩЕННОЙ ЛОГИКИ ---

            // Просто сбрасываем флаги. Мы НЕ даем команду, куда идти дальше.
            // Решение примет UpdateBehavior на следующем кадре на основе СВЕЖИХ данных.
            _isPathfindingToDoor = false;
            _finalDestination = Vector3.zero;

            // Даем короткую паузу, чтобы NavMesh успел обновиться
            yield return new WaitForSeconds(0.1f);
        }

        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            StartCoroutine(DelayedUpdateAnimatorSpeed());
        }

        yield return new WaitForSeconds(doorInteractionTime);

        isInteractingWithDoor = false;
    }

    void FindAllDoors()
    {
        allDoors = FindObjectsByType<SlidingDoor>(FindObjectsSortMode.None);
        Debug.Log($"[RobotAI] Found {allDoors.Length} doors.");
    }

    private IEnumerator FootstepPlayerCoroutine()
    {
        // Бесконечный цикл, который будет работать, пока жив объект
        while (true)
        {
            // Проверяем, движется ли агент (скорость больше минимального порога)
            if (agent != null && agent.velocity.magnitude > 0.1f)
            {
                // 1. Выбираем интервал в зависимости от текущего состояния
                float currentStepInterval = patrolStepInterval; // По умолчанию - скорость патрулирования
                switch (currentState)
                {
                    case AIState.Investigate:
                    case AIState.Hunt:
                        currentStepInterval = huntStepInterval;
                        break;
                    case AIState.Chase:
                        currentStepInterval = chaseStepInterval;
                        break;
                }

                // 2. Воспроизводим случайный звук шага из массива
                if (footstepSounds.Length > 0)
                {
                    AudioClip clipToPlay = footstepSounds[Random.Range(0, footstepSounds.Length)];
                    audioSource.PlayOneShot(clipToPlay, footstepVolume);
                }

                // 3. Ждем определенное время перед следующим шагом
                yield return new WaitForSeconds(currentStepInterval);
            }
            else
            {
                // Если агент не движется, просто ждем следующего кадра и проверяем снова.
                // Это предотвращает лишнюю нагрузку.
                yield return null;
            }
        }
    }

    /// <summary>
    /// Выбирает следующую осмысленную комнату для патрулирования, избегая повторений.
    /// </summary>
    void PickNextDestinationRoom()
    {
        isPerformingLocalPatrol = false; // Мы ищем новую комнату, значит, локальный патруль окончен

        if (rooms == null || rooms.Count <= 1)
        {
            Debug.LogWarning("[RobotAI] Недостаточно комнат для патрулирования.");
            return; // Или можно вызвать запасной метод случайного блуждания
        }

        List<Room> possibleDestinations = new List<Room>(rooms);
        if (currentRoom != null) possibleDestinations.Remove(currentRoom);
        if (previousRoom != null && possibleDestinations.Count > 1) possibleDestinations.Remove(previousRoom);

        if (possibleDestinations.Count == 0)
        {
            possibleDestinations.AddRange(rooms);
            if (currentRoom != null) possibleDestinations.Remove(currentRoom);
        }

        destinationRoom = possibleDestinations[Random.Range(0, possibleDestinations.Count)];

        SetSmartDestination(destinationRoom.transform.position);
        Debug.Log($"[RobotAI] Новая глобальная цель: комната '{destinationRoom.name}'.");
        StartCoroutine(DelayedUpdateAnimatorSpeed());
    }

    /// <summary>
    /// Находит и устанавливает случайную точку патрулирования ВНУТРИ текущей большой зоны.
    /// </summary>
    void SetLocalPatrolPoint()
    {
        if (currentRoom == null) { PickNextDestinationRoom(); return; }

        // В течение 10 попыток ищем подходящую точку
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector3 potentialPoint;
            // Используем наш существующий метод, чтобы найти валидную точку на NavMesh
            if (FindValidPointInRoom(currentRoom, out potentialPoint))
            {
                // --- НАЧАЛО НОВОЙ ЛОГИКИ ПРОВЕРКИ ---

                // Получаем вектор направления от монстра к потенциальной точке
                Vector3 directionToTarget = (potentialPoint - transform.position).normalized;

                float dotProduct = Vector3.Dot(transform.forward, directionToTarget);

                // Если точка находится перед нами (или мы стоим на месте и любая точка подойдет)
                if (dotProduct >= 0 || agent.velocity.magnitude < 0.1f)
                {
                    SetSmartDestination(potentialPoint);
                    Debug.Log($"[RobotAI] Патрулирую зону '{currentRoom.name}'. Новая локальная точка (впереди): {potentialPoint}");
                    StartCoroutine(DelayedUpdateAnimatorSpeed());
                    return; // Успех! Выходим из метода.
                }
                // Если точка оказалась сзади, мы ничего не делаем и просто переходим к следующей попытке в цикле for.
                Debug.Log($"[RobotAI] Точка-кандидат {potentialPoint} отклонена (находится сзади). Попытка {attempt + 1}/10.");

                // --- КОНЕЦ НОВОЙ ЛОГИКИ ПРОВЕРКИ ---
            }
        }
        Debug.LogWarning($"[RobotAI] Не удалось найти подходящую точку впереди за 10 попыток. Выбираю новую комнату.");
        PickNextDestinationRoom();
    }

    private void OnBlockedPathDetected()
    {
        // Если мы были в состоянии патрулирования, значит, заблокирована дверь на пути к цели.
        // Пересчитываем маршрут.
        if (currentState == AIState.Patrol)
        {
            Debug.Log("[RobotAI] Путь заблокирован. Пересчитываю цель патрулирования.");
            // Сбрасываем состояние локального патруля, если оно было активно
            isPerformingLocalPatrol = false;

            // Пытаемся выбрать новую цель.
            // Если у нас есть комнаты, выбираем новую комнату.
            if (rooms != null && rooms.Count > 0)
            {
                PickNextDestinationRoom();
            }
            else
            {
                // Если комнат нет (или система сломалась), используем старую запасную логику
                SetLegacyRandomPatrolDestination();
            }
        }
        // Если мы были не в патрулировании (например, в Investigate или Hunt),
        // то блок двери должен быть учтен NavMesh Agent'ом при пересчете пути.
        // Если же это важная дверь, ее можно вообще не учитывать в AI,
        // если она не открывается.
    }

    /// <summary>
    /// Вспомогательный метод для воспроизведения звуков состояний с проверками.
    /// </summary>
    private void PlayStateSound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, stateSoundVolume);
        }
    }

    Color GetStateColor()
    {
        switch (currentState)
        {
            case AIState.Patrol: return Color.green;
            case AIState.Investigate: return Color.yellow;
            case AIState.Hunt: return new Color(1f, 0.5f, 0f);
            case AIState.Chase: return Color.red;
            default: return Color.white;
        }
    }
    void SetLegacyRandomPatrolDestination()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * 20f;
            randomDirection += transform.position;
            randomDirection.y = transform.position.y;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, 20f, NavMesh.AllAreas))
            {
                SetSmartDestination(hit.position);
                Debug.Log($"[RobotAI] Установлена ЗАПАСНАЯ цель патрулирования: {hit.position}");
                StartCoroutine(DelayedUpdateAnimatorSpeed());
                return;
            }
        }
        Debug.LogWarning("[RobotAI] Не удалось найти случайную точку для патрулирования даже запасным методом.");
    }
    private bool FindValidPointInRoom(Room room, out Vector3 foundPoint)
    {
        foundPoint = Vector3.zero;
        if (room == null) return false;

        Collider roomCollider = room.GetComponent<Collider>();
        if (roomCollider == null) return false;

        for (int i = 0; i < 10; i++)
        {
            Bounds roomBounds = roomCollider.bounds;
            Vector3 randomPoint = new Vector3(
                Random.Range(roomBounds.min.x, roomBounds.max.x),
                transform.position.y,
                Random.Range(roomBounds.min.z, roomBounds.max.z)
            );

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 5f, NavMesh.AllAreas))
            {
                foundPoint = hit.position;
                return true;
            }
        }
        return false;
    }
    private int GetStatePriority(AIState state)
    {
        switch (state)
        {
            case AIState.Chase: return 4;
            case AIState.Hunt: return 3;
            case AIState.Investigate: return 2;
            case AIState.Patrol: return 1;
            default: return 0;
        }
    }
    /// <summary>
    /// Простая и надежная корутина для реакции на внешние раздражители (приманка, тревога).
    /// ИИ просто идет к точке, ждет и возвращается в патруль.
    /// </summary>
    IEnumerator GoInvestigatePoint(Vector3 targetPoint)
    {
        // 1. Включаем "Приоритетный режим", чтобы ExecuteCurrentState нам не мешал.
        isExecutingMandatorySearch = true;

        try
        {
            Debug.Log($"[RobotAI] Начинаю простое расследование точки {targetPoint}");

            // 2. Идем к цели.
            SetSmartDestination(targetPoint);

            // Ждем прибытия. Прерываемся, если увидели игрока.
            // Используем таймер-предохранитель (15 сек), чтобы не застрять.
            float waitTimer = 0f;
            while (agent.remainingDistance > agent.stoppingDistance + 0.5f && waitTimer < 15f)
            {
                if (playerVisibility > 0.1f || _playerCaught) // Добавлена проверка на _playerCaught
                {
                    Debug.Log("[RobotAI] Во время движения к точке заметил игрока! Прерываюсь.");
                    yield break; // Выходим, finally сработает
                }
                waitTimer += Time.deltaTime;
                yield return null;
            }

            // 3. Короткая пауза на месте для имитации "осмотра".
            Debug.Log("[RobotAI] Прибыл на место. Осматриваюсь...");
            agent.isStopped = true; // Останавливаемся
            yield return new WaitForSeconds(2.0f); // Ждем 2 секунды
            agent.isStopped = false;
        }
        finally
        {
            // 4. ГАРАНТИРОВАННАЯ ОЧИСТКА. Этот код выполнится всегда.
            Debug.Log("[RobotAI] Расследование точки завершено. Возвращаюсь в патруль.");
            isExecutingMandatorySearch = false; // Отключаем "Приоритетный режим"
            OnStateChange(AIState.Patrol);      // Возвращаемся в патруль
            currentSearchCoroutine = null;      // Сбрасываем ссылку
        }
    }
}