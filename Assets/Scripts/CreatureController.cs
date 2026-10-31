using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

public class CreatureController : MonoBehaviour
{
    public enum MonsterState
    {
        Idle,
        Patrolling,
        Chasing,
        ReturningToSpawn,
        Redirected
    }

    [Header("Monster Timer Settings")]
    public float minInitialTimer = 180f;
    public float maxInitialTimer = 300f;
    public float activeSearchDuration = 30f;
    public float waypointTolerance = 1.0f;
    [Tooltip("За сколько секунд до появления монстра активировать предупреждение игроку.")]
    public float monsterWarningDuration = 5f;

    [Header("Warning Area of Operation")]
    [Tooltip("Центр зоны (Transform), в которой будет активироваться 'Экран опасности' перед спавном монстра. Если null, предупреждение будет работать везде.")]
    public Transform warningAreaCenter;
    [Tooltip("Радиус зоны, в которой будет активироваться 'Экран опасности'.")]
    public float warningAreaRadius = 30f;

    [Header("Monster Movement & Detection")]
    public float patrolSpeed = 3.5f;
    public float chaseSpeed = 6.0f;
    public float detectionRadius = 5f;
    public LayerMask obstacleMask;
    [Tooltip("Радиус коллайдера, который будет использоваться для поимки игрока.")]
    public float catchTriggerRadius = 1.0f;

    [Header("Redirect & Blocked Spawns")]
    [Tooltip("Список заблокированных точек спавна.")]
    public List<Transform> blockedSpawnPoints = new List<Transform>();
    private float _redirectTimer = 0f;
    private MonsterState _currentStateBeforeRedirect;

    [Header("References")]
    public GameObject monsterVisuals;
    public Transform[] spawnPoints;
    public Transform[] patrolPoints;
    public PlayerController playerController;
    public GameObject gameOverPanel;
    public float restartSceneDelay = 3f;

    [Header("Cinematic Death References")]
    [Tooltip("Камера, которая будет активирована для показа монстра. Должна быть дочерней по отношению к монстру.")]
    public Camera monsterDeathCamera;
    [Tooltip("Точка, на которую будет смотреть камера монстра. Должна быть дочерней по отношению к игроку.")]
    public Transform playerDeathLookAtPoint;
    [Tooltip("Задержка перед воспроизведением анимации удара после переключения камеры.")]
    public float preAnimationDelay = 0.0f;
    [Tooltip("Задержка после воспроизведения анимации удара перед показом панели смерти. Установлено на 3 секунды.")]
    public float postAnimationDelay = 1.0f; // <--- ИЗМЕНЕНО НА 3.0f

    [Header("Audio References & Clips")]
    [Tooltip("Источник звука для шагов. Если не указан, будет добавлен автоматически.")]
    [SerializeField] private AudioSource footstepAudioSource;
    [Tooltip("Источник звука для рычания и вокализаций. Если не указан, будет добавлен автоматически.")]
    [SerializeField] private AudioSource vocalAudioSource;

    [Tooltip("Клипы шагов во время патрулирования/ходьбы.")]
    public AudioClip[] patrolFootstepClips;
    [Tooltip("Клипы шагов во время погони/бега.")]
    public AudioClip[] chaseFootstepClips;
    [Tooltip("Клипы случайного рычания/вокализаций.")]
    public AudioClip[] vocalClips;

    [Header("Audio Timing")]
    [Tooltip("Интервал между шагами (в секундах) при патрулировании.")]
    public float footstepIntervalPatrol = 0.5f;
    [Tooltip("Интервал между шагами (в секундах) при погоне.")]
    public float footstepIntervalChase = 0.25f;
    [Tooltip("Минимальный интервал (в секундах) для случайного рычания.")]
    public float minVocalInterval = 10f;
    [Tooltip("Максимальный интервал (в секундах) для случайного рычания.")]
    public float maxVocalInterval = 30f;

    [Header("First Person Catch Settings")]
    [Tooltip("Минимальное расстояние, на котором монстр должен быть от игрока для кинематографической смерти от первого лица. Игрок будет отодвинут назад, если расстояние меньше.")]
    public float minDistanceForFPCatch = 1.5f; // Изменено на МИНИМАЛЬНОЕ расстояние
    [Tooltip("Высота, на которую камера игрока должна смотреть на монстра (относительно земли).")]
    public float playerLookAtHeight = 1.5f;


    private NavMeshAgent _navMeshAgent;
    private SphereCollider _catchCollider;
    private float _currentTimer;
    private float _currentSearchTime;
    private MonsterState _currentState;
    private int _currentPatrolPointIndex;
    private bool _playerCaught = false;
    private float _timerAccelerationFactor = 1.0f;

    public MonsterState CurrentMonsterState => _currentState;
    public float RemainingTimer => _currentTimer;

    private float _footstepTimer;
    private float _vocalTimer;
    private float _currentVocalInterval;

    [SerializeField] private MonsterAnimationController animationController;

    void Awake()
    {
        Debug.Log("CreatureController: Awake method called.");

        _navMeshAgent = GetComponent<NavMeshAgent>();
        if (_navMeshAgent == null)
        {
            Debug.LogError("CreatureController: NavMeshAgent component not found on " + gameObject.name + "! Disabling script.", this);
            enabled = false;
            return;
        }
        Debug.Log("CreatureController: NavMeshAgent found.");

        if (monsterVisuals == null)
        {
            Debug.LogError("CreatureController: Monster Visuals GameObject is not assigned in CreatureController! Disabling script.", this);
            enabled = false;
            return;
        }

        if (animationController == null)
        {
            Debug.LogError("CreatureController: MonsterAnimationController is not assigned in the Inspector! Please assign the GameObject containing MonsterAnimationController to the 'Animation Controller' field.", this);
        }
        else
        {
            Debug.Log("CreatureController: MonsterAnimationController found (assigned in Inspector).");
        }

        monsterVisuals.SetActive(false);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("CreatureController: Game Over Panel is not assigned. Player will still be caught, but no UI will show.", this);
        }

        if (playerController == null)
        {
            playerController = FindAnyObjectByType<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("CreatureController: PlayerController not found in scene or not assigned! Disabling script.", this);
                enabled = false;
                return;
            }
        }
        Debug.Log("CreatureController: PlayerController found.");

        if (monsterDeathCamera != null)
        {
            monsterDeathCamera.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("CreatureController: Monster Death Camera is not assigned. Cinematic death sequence will not play correctly.", this);
        }

        SetupAudioSources();
        SetupCatchTrigger();
        Debug.Log("CreatureController: Awake method finished.");
    }

    void Start()
    {
        Debug.Log("CreatureController: Start method called.");
        ResetTimer();
    }

    void Update()
    {
        if (_playerCaught) return;

        if (_redirectTimer > 0f)
        {
            _redirectTimer -= Time.deltaTime;
            HandleRedirectedState();
            if (_redirectTimer <= 0f)
            {
                EndRedirectedState();
                Debug.Log("CreatureController: Redirect timer ended.");
            }
            return;
        }

        switch (_currentState)
        {
            case MonsterState.Idle:
                HandleIdleState();
                break;
            case MonsterState.Patrolling:
                HandlePatrollingState();
                break;
            case MonsterState.Chasing:
                HandleChasingState();
                break;
            case MonsterState.ReturningToSpawn:
                HandleReturningToSpawnState();
                break;
        }

        UpdateMonsterAnimationSpeed();
        HandleFootsteps();
        HandleVocalSounds();
    }

    private void SetupCatchTrigger()
    {
        Debug.Log("CreatureController: Setting up Catch Trigger.");
        SphereCollider catchCollider = GetComponent<SphereCollider>();

        if (catchCollider == null)
        {
            catchCollider = gameObject.AddComponent<SphereCollider>();
            Debug.Log("CreatureController: Added new SphereCollider for catch trigger.");
        }

        _catchCollider = catchCollider;

        catchCollider.isTrigger = true;
        catchCollider.radius = catchTriggerRadius;
        catchCollider.center = Vector3.up * 0.5f;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            Debug.Log("CreatureController: Added new Rigidbody for catch trigger (isKinematic=true).");
        }
    }

    private void HandleIdleState()
    {
        _currentTimer -= Time.deltaTime * _timerAccelerationFactor;

        bool playerInWarningArea = IsPlayerInWarningArea();

        if (_currentTimer <= monsterWarningDuration && playerController != null && playerInWarningArea)
        {
            if (!playerController.ScreenDangerWarningState)
            {
                Debug.Log($"CreatureController: Danger warning active (player is in area). Time left: {_currentTimer:F1}");
                playerController.SetScreenDangerWarningState(true);
            }
        }
        else
        {
            // Стандартное отключение предупреждения, если таймер далеко от нуля.
            // (Не отключаем, если игрок просто покинул зону, так как другой монстр может держать его активным).
            if (playerController != null && playerController.ScreenDangerWarningState && _currentTimer > monsterWarningDuration)
            {
                Debug.Log("CreatureController: Danger warning deactivated (timer reset/high).");
                playerController.SetScreenDangerWarningState(false);
            }
        }

        if (_currentTimer <= 0)
        {
            Debug.Log("CreatureController: Monster timer reached zero. Starting active search!");
            StartSearch();
        }
    }

    public bool IsPlayerInWarningArea()
    {
        if (playerController == null)
        {
            return false;
        }

        // Если центр зоны не назначен, зона считается бесконечной (как раньше)
        if (warningAreaCenter == null)
        {
            return true;
        }

        float distance = Vector3.Distance(playerController.transform.position, warningAreaCenter.position);
        return distance <= warningAreaRadius;
    }


    private void HandlePatrollingState()
    {
        _currentSearchTime -= Time.deltaTime;

        if (CheckForPlayerDetection())
        {
            StartChasing();
            return;
        }

        if (_navMeshAgent.remainingDistance < waypointTolerance && !_navMeshAgent.pathPending)
        {
            Debug.Log("CreatureController: Reached patrol point. Moving to next.");
            MoveToNextPatrolPoint();
        }

        if (_currentSearchTime <= 0)
        {
            Debug.Log("CreatureController: Monster's active search time ended. Returning to spawn.");
            StartReturningToSpawn();
        }
    }

    private void HandleChasingState()
    {
        if (playerController.isHiding || !IsPlayerVisibleAndClose())
        {
            Debug.Log("CreatureController: Player either hid or escaped vision. Monster resumes patrol.");
            _navMeshAgent.speed = patrolSpeed;
            _currentState = MonsterState.Patrolling;
            MoveToNearestPatrolPoint();
            return;
        }

        _navMeshAgent.SetDestination(playerController.transform.position);
    }

    private void HandleReturningToSpawnState()
    {
        if (CheckForPlayerDetection())
        {
            StartChasing();
            return;
        }

        if (_navMeshAgent.remainingDistance < waypointTolerance && !_navMeshAgent.pathPending)
        {
            Debug.Log("CreatureController: Monster reached spawn point. Despawning.");
            EndSearch();
        }
    }

    public void ResetTimer()
    {
        _currentTimer = Random.Range(minInitialTimer, maxInitialTimer);
        _currentState = MonsterState.Idle;

        if (_catchCollider != null)
        {
            _catchCollider.enabled = false;
            Debug.Log("CreatureController: Catch Collider disabled.");
        }

        if (_navMeshAgent != null)
        {
            _navMeshAgent.isStopped = true;
            _navMeshAgent.enabled = false;
            Debug.Log("CreatureController: NavMeshAgent disabled.");
        }

        monsterVisuals.SetActive(false);

        if (playerController != null)
        {
            playerController.SetScreenDangerWarningState(false);
        }

        Debug.Log($"CreatureController: Monster timer reset. Next search in: {_currentTimer:F1} seconds. State: Idle.");
    }

    private void StartSearch()
    {
        _currentState = MonsterState.Patrolling;
        monsterVisuals.SetActive(true);
        _currentSearchTime = activeSearchDuration;
        _playerCaught = false;

        if (_navMeshAgent != null)
        {
            _navMeshAgent.enabled = true; // Включаем агент перед Warp
        }

        if (_catchCollider != null)
        {
            _catchCollider.enabled = true; // Включаем коллайдер
        }

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform[] availableSpawns = spawnPoints.Where(p => p != null && !blockedSpawnPoints.Contains(p)).ToArray();

            if (availableSpawns.Length > 0)
            {
                Transform spawnPoint = availableSpawns[Random.Range(0, availableSpawns.Length)];
                _navMeshAgent.Warp(spawnPoint.position);
                Debug.Log($"CreatureController: Monster warped to spawn point: {spawnPoint.name}.");
            }
            else
            {
                Debug.LogWarning("CreatureController: All spawn points are blocked! Spawning at current position.");
            }
        }
        else
        {
            Debug.LogWarning("CreatureController: No spawn points assigned for monster! Spawning at current position.");
        }

        _navMeshAgent.speed = patrolSpeed;
        _navMeshAgent.isStopped = false;
        MoveToNearestPatrolPoint();

        if (playerController != null)
        {
            playerController.SetScreenDangerWarningState(false);
        }

        Debug.Log($"CreatureController: Monster started active search for {activeSearchDuration} seconds. State: Patrolling.");
    }

    private void StartChasing()
    {
        _currentState = MonsterState.Chasing;
        _navMeshAgent.speed = chaseSpeed;
        _navMeshAgent.isStopped = false;
        _navMeshAgent.SetDestination(playerController.transform.position);
        Debug.Log("CreatureController: Monster started chasing player! State: Chasing.");
    }

    private void StartReturningToSpawn()
    {
        _currentState = MonsterState.ReturningToSpawn;
        _navMeshAgent.speed = patrolSpeed;
        _navMeshAgent.isStopped = false;

        Transform nearestSpawn = GetNearestSpawnPoint(transform.position);
        if (nearestSpawn != null)
        {
            _navMeshAgent.SetDestination(nearestSpawn.position);
            Debug.Log($"CreatureController: Monster returning to spawn point: {nearestSpawn.name}. State: ReturningToSpawn.");
        }
        else
        {
            Debug.LogWarning("CreatureController: No spawn points to return to! Monster will despawn immediately.");
            EndSearch();
        }
    }

    private void EndSearch()
    {
        ResetTimer();
        Debug.Log("CreatureController: Monster ended search and despawned.");
    }

    private void MoveToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogWarning("CreatureController: No patrol points assigned for monster!");
            _navMeshAgent.isStopped = true;
            return;
        }

        _navMeshAgent.isStopped = false;
        _currentPatrolPointIndex = (_currentPatrolPointIndex + 1) % patrolPoints.Length;
        _navMeshAgent.SetDestination(patrolPoints[_currentPatrolPointIndex].position);
        Debug.Log($"CreatureController: Moving to patrol point {patrolPoints[_currentPatrolPointIndex].name}.");
    }

    private void MoveToNearestPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogWarning("CreatureController: No patrol points assigned for monster!");
            _navMeshAgent.isStopped = true;
            return;
        }

        _navMeshAgent.isStopped = false;
        Transform nearestPoint = GetNearestPatrolPoint(transform.position);
        if (nearestPoint != null)
        {
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == nearestPoint)
                {
                    _currentPatrolPointIndex = i;
                    break;
                }
            }
            _navMeshAgent.SetDestination(nearestPoint.position);
            Debug.Log($"CreatureController: Moving to nearest patrol point {nearestPoint.name}.");
        }
        else
        {
            Debug.LogWarning("CreatureController: Could not find a nearest patrol point!");
        }
    }

    public void SetTimerAcceleration(float factor)
    {
        _timerAccelerationFactor = Mathf.Max(1.0f, factor);
        if (_timerAccelerationFactor > 1.0f)
        {
            Debug.Log($"CreatureController: Ускорение таймера монстра установлено на {_timerAccelerationFactor}x. Таймер будет идти быстрее.");
        }
        else
        {
            Debug.Log("CreatureController: Ускорение таймера монстра сброшено до нормального (1.0x).");
        }
    }

    private bool CheckForPlayerDetection()
    {
        if (playerController == null || playerController.isHiding)
        {
            return false;
        }

        return IsPlayerVisibleAndClose();
    }

    private bool IsPlayerVisibleAndClose()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerController.transform.position);
        if (distanceToPlayer <= detectionRadius)
        {
            RaycastHit hit;
            Vector3 directionToPlayer = (playerController.transform.position - transform.position).normalized;
            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(rayOrigin, directionToPlayer, out hit, detectionRadius, ~obstacleMask))
            {
                if (hit.collider.CompareTag("Player"))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void ReduceTimer(float amount)
    {
        if (_currentState != MonsterState.Idle) return;

        _currentTimer = Mathf.Max(0, _currentTimer - amount);
        Debug.Log($"CreatureController: Monster timer reduced by {amount:F1} seconds. New remaining time: {_currentTimer:F1} seconds. (Triggered by external event)");

        if (_currentTimer <= 0)
        {
            Debug.Log("CreatureController: Timer reduced to zero. Monster starts immediate search!");
            StartSearch();
        }

        else if (_currentTimer <= monsterWarningDuration && playerController != null && IsPlayerInWarningArea())
        {
            playerController.SetScreenDangerWarningState(true);
        }

    }

    /// <summary>
    /// Измененный метод поимки игрока для кинематографичной смерти от первого лица игрока.
    /// </summary>
    public void CatchPlayer()
    {
        if (_playerCaught) return;
        _playerCaught = true;

        Debug.Log("CreatureController: Player caught! Initiating cinematic death sequence (First-person perspective, player turns to monster).");

        // 1. Блокируем управление игрока и перемещение монстра
        if (_navMeshAgent != null && _navMeshAgent.isOnNavMesh) _navMeshAgent.isStopped = true;
        if (playerController != null)
        {
            playerController.SetMovementEnabled(false);
            playerController.SetLookEnabled(false);
            playerController.SetScreenDangerWarningState(false);
        }

        // 2. Запускаем корутину для последовательности событий
        StartCoroutine(CinematicDeathSequenceFirstPerson()); // <-- Используем новую корутину
    }

    private IEnumerator CinematicDeathSequenceFirstPerson() // <-- Измененная корутина
    {
        // Убедимся, что монстр активен и виден
        monsterVisuals.SetActive(true);

        // Отключаем навигационный агент, чтобы монстр оставался на месте
        if (_navMeshAgent != null && _navMeshAgent.isOnNavMesh)
        {
            _navMeshAgent.isStopped = true;
            _navMeshAgent.velocity = Vector3.zero; // Останавливаем любое движение
        }

        if (playerController != null && playerController.playerCamera != null)
        {
            // Определяем точку на монстре, на которую игрок должен смотреть
            Vector3 monsterLookPoint = transform.position;
            // Устанавливаем высоту точки взгляда на монстра, чтобы игрок смотрел на нужную часть монстра
            monsterLookPoint.y = transform.position.y + playerLookAtHeight;

            // Направление от камеры игрока к монстру
            Vector3 directionToMonster = (monsterLookPoint - playerController.playerCamera.transform.position).normalized;

            // Вычисляем целевой поворот для камеры игрока
            Quaternion targetPlayerCameraRotation = Quaternion.LookRotation(directionToMonster);

            // Получаем текущий поворот камеры игрока
            Quaternion currentPlayerCameraRotation = playerController.playerCamera.transform.rotation;

            float turnDuration = 0.3f; // Время, за которое игрок поворачивается
            float elapsed = 0f;

            // Плавный поворот камеры игрока
            while (elapsed < turnDuration)
            {
                playerController.playerCamera.transform.rotation = Quaternion.Slerp(currentPlayerCameraRotation, targetPlayerCameraRotation, elapsed / turnDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            playerController.playerCamera.transform.rotation = targetPlayerCameraRotation;


            // Опционально: если монстр слишком близко, отодвинуть игрока назад
            float distanceToMonster = Vector3.Distance(playerController.transform.position, transform.position);
            if (distanceToMonster < minDistanceForFPCatch) // Игрок СЛИШКОМ БЛИЗКО
            {
                Debug.Log($"CreatureController: Monster is too close ({distanceToMonster:F1}m), moving player back.");

                // Направление от монстра к игроку
                Vector3 directionFromMonsterToPlayer = (playerController.transform.position - transform.position).normalized;

                // Целевая позиция игрока: от монстра на расстоянии minDistanceForFPCatch
                Vector3 playerMoveTarget = transform.position + directionFromMonsterToPlayer * minDistanceForFPCatch;
                playerMoveTarget.y = playerController.transform.position.y; // Сохраняем высоту игрока

                Vector3 playerStartPos = playerController.transform.position;
                elapsed = 0f;
                float playerMoveDuration = 0.2f; // Время на перемещение игрока

                while (elapsed < playerMoveDuration)
                {
                    playerController.transform.position = Vector3.Lerp(playerStartPos, playerMoveTarget, elapsed / playerMoveDuration);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                playerController.transform.position = playerMoveTarget;
            }
            // Важно: Если у вас есть отдельный скрипт для управления головой/телом игрока,
            // который зависит от поворота камеры, то здесь нужно будет его синхронизировать
            // или временно отключить, чтобы избежать конфликтов.
        }
        else
        {
            Debug.LogWarning("CreatureController: PlayerController or Player Camera not found/assigned. Player will not turn to monster.");
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
            TextMeshProUGUI gameOverText = gameOverPanel.GetComponentInChildren<TextMeshProUGUI>();
            if (gameOverText != null)
            {
                gameOverText.text = "Game Over!\nYou were caught by the monster.";
            }
        }
        else
        {
            Debug.LogWarning("CreatureController: Game Over Panel is not assigned. Player will still be caught, but no UI will show.");
        }

        // Запускаем перезапуск сцены с задержкой
        Invoke("RestartScene", restartSceneDelay);
    }


    private void RestartScene()
    {
        Debug.Log("CreatureController: Restarting scene.");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private Transform GetNearestPatrolPoint(Vector3 currentPosition)
    {
        Transform nearest = null;
        float minDistance = Mathf.Infinity;

        if (patrolPoints == null || patrolPoints.Length == 0) return null;

        foreach (Transform point in patrolPoints)
        {
            if (point == null) continue;
            float dist = Vector3.Distance(currentPosition, point.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = point;
            }
        }
        return nearest;
    }

    private Transform GetNearestSpawnPoint(Vector3 currentPosition)
    {
        Transform nearest = null;
        float minDistance = Mathf.Infinity;

        if (spawnPoints == null || spawnPoints.Length == 0) return null;

        Transform[] availableSpawns = spawnPoints.Where(p => p != null && !blockedSpawnPoints.Contains(p)).ToArray();

        // Fix: Changed 'availableSpawables' to 'availableSpawns'
        if (availableSpawns.Length == 0) return null;

        foreach (Transform point in availableSpawns)
        {
            if (point == null) continue;
            float dist = Vector3.Distance(currentPosition, point.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = point;
            }
            else if (dist == minDistance && nearest == null)
            {
                nearest = point;
            }
        }
        return nearest;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !_playerCaught)
        {
            // Если playerController существует и игрок прячется, игнорируем прямое столкновение.
            if (playerController != null && playerController.isHiding)
            {
                Debug.Log($"CreatureController: Monster touched player while player was hiding in a spot. Ignoring direct catch.");
                return; // Игнорируем поимку при прямом столкновении, если игрок прячется.
            }

            Debug.Log("CreatureController: Monster physically touched player!");
            CatchPlayer();
        }
    }

    void OnDrawGizmosSelected()
    {
        // Область обнаружения монстра
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Область поимки монстра
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, catchTriggerRadius);


        if (warningAreaCenter != null)
        {
            Gizmos.color = Color.Lerp(Color.blue, Color.white, 0.5f); // Светло-голубой/бирюзовый для зоны предупреждения
            // Рисуем полупрозрачную сферу
            Gizmos.DrawWireSphere(warningAreaCenter.position, warningAreaRadius);
            // Дополнительная линия к центру, чтобы было легче найти Transform
            Gizmos.DrawLine(warningAreaCenter.position, warningAreaCenter.position + Vector3.up * (warningAreaRadius * 0.1f));

            // Показываем, что область активна, если игрок в ней
            if (_currentState == MonsterState.Idle && playerController != null && IsPlayerInWarningArea())
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(warningAreaCenter.position, warningAreaRadius * 0.95f);
            }
        }

        if (spawnPoints != null)
        {
            Gizmos.color = Color.cyan;
            foreach (Transform point in spawnPoints)
            {
                if (point != null) Gizmos.DrawSphere(point.position, 0.5f);
            }
        }

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.magenta;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawSphere(patrolPoints[i].position, 0.4f);
                    if (patrolPoints.Length > 1)
                    {
                        Transform nextPoint = patrolPoints[(i + 1) % patrolPoints.Length];
                        if (nextPoint != null) Gizmos.DrawLine(patrolPoints[i].position, nextPoint.position);
                    }
                }
            }
        }
    }

    private void SetupAudioSources()
    {
        Debug.Log("CreatureController: Setting up audio sources.");
        if (footstepAudioSource == null)
        {
            AudioSource[] existingSources = GetComponents<AudioSource>();
            if (existingSources.Length >= 2)
            {
                footstepAudioSource = existingSources[0];
                vocalAudioSource = existingSources[1];
            }
            else
            {
                footstepAudioSource = gameObject.AddComponent<AudioSource>();
                vocalAudioSource = gameObject.AddComponent<AudioSource>();
                Debug.Log("CreatureController: Added new AudioSources.");
            }
        }

        footstepAudioSource.playOnAwake = false;
        footstepAudioSource.loop = false;
        footstepAudioSource.spatialBlend = 1.0f;

        vocalAudioSource.playOnAwake = false;
        vocalAudioSource.loop = false;
        vocalAudioSource.spatialBlend = 1.0f;

        _footstepTimer = 0f;
        _currentVocalInterval = Random.Range(minVocalInterval, maxVocalInterval);
        _vocalTimer = 0f;
        Debug.Log("CreatureController: Audio sources setup complete.");
    }
    private void HandleFootsteps()
    {
        if (_currentState == MonsterState.Idle || _navMeshAgent == null || !_navMeshAgent.isOnNavMesh || _navMeshAgent.velocity.magnitude < 0.1f)
        {
            _footstepTimer = 0f;
            return;
        }

        float interval;
        AudioClip[] clips;

        if (_currentState == MonsterState.Chasing)
        {
            interval = footstepIntervalChase;
            clips = chaseFootstepClips;
        }
        else // Patrolling или ReturningToSpawn
        {
            interval = footstepIntervalPatrol;
            clips = patrolFootstepClips;
        }

        _footstepTimer -= Time.deltaTime;

        if (_footstepTimer <= 0)
        {
            PlayRandomClip(footstepAudioSource, clips);
            _footstepTimer = interval;
        }
    }

    /// <summary>
    /// Обрабатывает логику случайного воспроизведения звуков рычания.
    /// </summary>
    private void HandleVocalSounds()
    {
        if (vocalClips == null || vocalClips.Length == 0) return;

        if (_currentState == MonsterState.Idle) return;

        _vocalTimer += Time.deltaTime;

        if (_vocalTimer >= _currentVocalInterval)
        {
            PlayRandomClip(vocalAudioSource, vocalClips);
            _vocalTimer = 0f;
            _currentVocalInterval = Random.Range(minVocalInterval, maxVocalInterval);
            Debug.Log($"CreatureController: Playing vocal sound. Next in {_currentVocalInterval:F1}s.");
        }
    }

    /// <summary>
    /// Воспроизводит один случайный клип из массива.
    /// </summary>
    /// <param name="source">Источkик звука.</param>
    /// <param name="clips">Массив клипов.</param>
    private void PlayRandomClip(AudioSource source, AudioClip[] clips)
    {
        if (source != null && clips != null && clips.Length > 0)
        {
            int index = Random.Range(0, clips.Length);
            source.PlayOneShot(clips[index]);
        }
        else
        {
            Debug.LogWarning("CreatureController: Cannot play audio clip. Source or clips array is null/empty.");
        }
    }
    private void HandleRedirectedState()
    {
        if (CheckForPlayerDetection())
        {
            _redirectTimer = 0f;
            StartChasing();
            Debug.Log("CreatureController: Player detected during redirect, starting chase.");
            return;
        }

        if (_navMeshAgent.remainingDistance < waypointTolerance && !_navMeshAgent.pathPending)
        {
            if (!_navMeshAgent.isStopped)
            {
                _navMeshAgent.isStopped = true;
                Debug.Log("CreatureController: Monster reached redirect target, waiting.");
            }
        }
    }

    private void EndRedirectedState()
    {
        Debug.Log("CreatureController: Monster redirect time is over. Resuming previous state or resetting.");

        if (_currentStateBeforeRedirect == MonsterState.Patrolling ||
            _currentStateBeforeRedirect == MonsterState.Chasing ||
            _currentStateBeforeRedirect == MonsterState.ReturningToSpawn)
        {
            StartReturningToSpawn();
        }
        else
        {
            ResetTimer();
        }
    }
    public void RedirectMonster(Transform targetLocation, float duration)
    {
        if (targetLocation == null || _currentState == MonsterState.Redirected)
        {
            Debug.LogWarning("CreatureController: RedirectMonster failed. Target null or already redirected.");
            return;
        }

        // Если монстр был в Idle, его нужно активировать, как при StartSearch!
        bool wasIdle = _currentState == MonsterState.Idle;

        _currentStateBeforeRedirect = _currentState;
        _currentState = MonsterState.Redirected;
        _redirectTimer = duration;

        monsterVisuals.SetActive(true);

        if (_navMeshAgent != null && !_navMeshAgent.enabled)
        {
            _navMeshAgent.enabled = true;
            Debug.Log("CreatureController: NavMeshAgent re-enabled for Redirect.");
        }

        if (_catchCollider != null && !_catchCollider.enabled)
        {
            _catchCollider.enabled = true;
            Debug.Log("CreatureController: Catch Collider re-enabled for Redirect.");
        }

        _navMeshAgent.isStopped = false;
        _navMeshAgent.speed = patrolSpeed;
        _navMeshAgent.SetDestination(targetLocation.position);

        if (wasIdle && playerController != null)
        {
            playerController.SetScreenDangerWarningState(false);
            Debug.Log("CreatureController: Redirect canceled Idle warning.");
        }

        Debug.Log($"CreatureController: Monster is redirected to {targetLocation.name} for {duration:F1} seconds. Previous state: {_currentStateBeforeRedirect}.");
    }

    public void BlockSpawnPoint(Transform spawnPointToBlock)
    {
        if (spawnPointToBlock != null && spawnPoints.Contains(spawnPointToBlock) && !blockedSpawnPoints.Contains(spawnPointToBlock))
        {
            blockedSpawnPoints.Add(spawnPointToBlock);
            // Fix: Corrected string concatenation and removed extra 'Point'
            Debug.Log($"CreatureController: Spawn Point {spawnPointToBlock.name} has been blocked.");
        }
        else
        {
            Debug.LogWarning("CreatureController: Attempted to block a null, non-existent or already blocked spawn point.");
        }
    }

    private void UpdateMonsterAnimationSpeed()
    {
        if (animationController != null && _navMeshAgent != null && _navMeshAgent.isOnNavMesh)
        {
            animationController.SetSpeed(_navMeshAgent.velocity.magnitude);
        }
        else
        {
            // Debug.LogWarning("CreatureController: Cannot update animation speed. animationController or NavMeshAgent is null/not on NavMesh.");
        }
    }

    public void PlayPunchAnimation()
    {
        if (animationController != null)
        {
            animationController.StartPunch();
            Debug.Log("CreatureController: Playing Punch animation.");
        }
        else
        {
            Debug.LogWarning("CreatureController: MonsterAnimationController not assigned, cannot play punch animation.");
        }
    }

    public void StopPunchAnimation()
    {
        if (animationController != null)
        {
            animationController.StopPunch();
            Debug.Log("CreatureController: Stopping Punch animation.");
        }
        else
        {
            Debug.LogWarning("CreatureController: MonsterAnimationController not assigned, cannot stop punch animation.");
        }
    }
}