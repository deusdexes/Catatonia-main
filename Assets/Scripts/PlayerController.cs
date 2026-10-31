using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI; // Для Slider и Image
using Random = UnityEngine.Random; // Чтобы не путать с System.Random

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float crouchSpeed = 1.5f;
    public float jumpHeight = 1f;
    public float gravity = -9.81f; // Гравитация
    private Vector3 velocity; // Скорость для гравитации

    [Header("Camera")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 80f;
    public float playerHeight = 1.8f; // Player's total height, useful for hiding calculations (e.g., CharacterController height)
    public float eyeHeight = 1.6f; // Normal eye height when standing
    public Camera playerCamera;
    [Tooltip("Максимальный наклон камеры вперед при беге")]
    public float runCameraTilt = 5f; // Угол наклона камеры вперед при беге
    [Tooltip("Скорость плавного изменения наклона камеры и высоты")]
    public float cameraSmoothSpeed = 10f; // Увеличим скорость, чтобы было более отзывчиво
    [Tooltip("Высота камеры при приседании (абсолютное значение, не множитель)")]
    public float crouchCameraHeight = 0.8f; // Абсолютная высота камеры при приседании. Установите здесь желаемое значение.
    [Tooltip("Максимальная дальность прорисовки (Far Clip Plane).")]
    public float maxViewDistance = 30f; // Установите желаемую максимальную дальность видимости

    [Header("Noise Settings")]
    public float currentNoiseLevel = 0f;
    public float noiseDecayRate = 2f;
    public float crouchNoiseRange = 5f;    // Максимальная дистанция слышимости при крадущемся движении
    public float walkNoiseRange = 10f;     // Максимальная дистанция слышимости при ходьбе
    public float runNoiseRange = 20f;      // Максимальная дистанция слышимости при беге
    public float currentNoiseRange = 0f;   // Текущая дистанция слышимости

    [Header("Hiding Camera")]
    public float minViewDistance = 3f;

    private CharacterController controller;

    [Header("Hiding")]
    public bool isHiding = false;
    public HidingSpot currentHidingSpot;
    private bool canHide = false;

    // Новая система ввода
    private Vector2 movementInput;
    private Vector2 lookInput;
    private bool isSprinting = false; // Состояние нажатия кнопки спринта
    private bool isCrouching = false;
    public bool isRunning { get; private set; } // Добавлено публичное свойство для определения бега

    // Компоненты камеры
    private float xRotation = 0f; // Это будет основной угол для вертикального вращения

    // Визуальные компоненты для скрытия
    private Renderer playerRenderer;

    // Маска для лучей
    private LayerMask raycastMask;

    private bool canMove = true;
    private bool canLook = true;

    [Header("Danger Warning UI")] // Новый заголовок для UI предупреждения
    public bool ScreenDangerWarningState { get; private set; }
    [Tooltip("Ссылка на CanvasGroup UI элемента, который будет отображать предупреждение.")]
    public CanvasGroup screenDangerCanvasGroup; // Ссылка на CanvasGroup элемента UI
    [Tooltip("Максимальная прозрачность красного предупреждения на экране.")]
    [Range(0f, 1f)]
    public float maxScreenDangerAlpha = 0.5f;
    [Tooltip("Скорость плавного появления/исчезновения красного предупреждения.")]
    public float screenDangerFadeSpeed = 2f;
    private bool _isWarningActive = false; // Внутренний флаг состояния предупреждения

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float currentStamina;
    [Tooltip("Скорость расхода стамины при беге в секунду")]
    public float staminaDrainRateRun = 15f;
    [Tooltip("Скорость восстановления стамины при ходьбе в секунду")]
    public float staminaRegenRateWalk = 5f;
    [Tooltip("Скорость восстановления стамины при приседании в секунду")]
    public float staminaRegenRateCrouch = 10f;
    [Tooltip("Длительность состояния 'отдышки' в секундах, когда стамина на 0")]
    public float exhaustionDuration = 3f;
    private float _exhaustionTimer = 0f;
    private bool _isExhausted = false; // Флаг состояния "отдышки"

    [Header("Stamina UI")]
    [Tooltip("UI элемент Slider для отображения стамины.")]
    public Slider staminaSlider;
    [Tooltip("CanvasGroup для UI стамины, чтобы управлять его видимостью (опционально).")]
    public CanvasGroup staminaCanvasGroup;
    [Tooltip("Цвет заливки слайдера при нормальном уровне стамины.")]
    public Color normalStaminaColor = Color.green;
    [Tooltip("Цвет заливки слайдера при низком уровне стамины.")]
    public Color lowStaminaColor = Color.yellow;
    [Tooltip("Цвет заливки слайдера при отдышке/истощении.")]
    public Color exhaustedStaminaColor = Color.red;
    [Tooltip("Порог (от 0 до 1), ниже которого стамина считается низкой.")]
    [Range(0f, 1f)]
    public float lowStaminaThreshold = 0.2f;
    [Tooltip("Скорость плавного появления/исчезновения UI стамины.")]
    public float staminaUIFadeSpeed = 5f;

    // Новые поля для аудио
    [Header("Audio Settings")]
    public AudioSource walkAudioSource;
    public AudioSource runAudioSource;
    public AudioSource crouchAudioSource;
    public AudioClip[] walkFootstepSounds;
    public AudioClip[] runFootstepSounds;
    public AudioClip[] crouchFootstepSounds;
    [Tooltip("Интервал между шагами при ходьбе (в секундах)")]
    public float walkStepInterval = 0.5f;
    [Tooltip("Интервал между шагами при беге (в секундах)")]
    public float runStepInterval = 0.3f;
    [Tooltip("Интервал между шагами при приседании (в секундах)")]
    public float crouchStepInterval = 0.7f;
    private float _currentStepTimer = 0f;

    private float _targetCameraTiltAngle = 0f; // Целевой угол наклона вперед
    private float _currentCameraTiltAngle = 0f; // Текущий угол наклона вперед (для сглаживания)
    private float _targetCameraHeight = 0f; // Целевая высота камеры
    private float _currentCameraHeight = 0f; // Текущая высота камеры (для сглаживания)

    private float originalWalkSpeed;
    private float originalRunSpeed;
    private float originalCrouchSpeed;

    [Header("Cart Pushing")] // Новая секция для настроек толкания тележки
    [Tooltip("Множитель замедления скорости игрока при толкании тележки.")]
    public float pushSpeedMultiplier = 0.8f; // Множитель замедления
    private bool isPushingCart = false; // Флаг, указывающий, толкает ли игрок тележку
    private float cartPushSpeed = 0f; // Скорость тележки, когда игрок её толкает

    // Добавим константу для ключа PlayerPrefs, чтобы избежать опечаток и дублирования
    private const string MOUSE_SENSITIVITY_KEY = "MouseSensitivity"; // Добавлено

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        playerRenderer = GetComponent<Renderer>();

        originalWalkSpeed = walkSpeed;
        originalRunSpeed = runSpeed;
        originalCrouchSpeed = crouchSpeed;


        mouseSensitivity = PlayerPrefs.GetFloat(MOUSE_SENSITIVITY_KEY, mouseSensitivity);

        // Настраиваем маску для лучей
        raycastMask = ~((1 << LayerMask.NameToLayer("Player")) |
                       (1 << LayerMask.NameToLayer("HidingSpot")) |
                       (1 << LayerMask.NameToLayer("Ignore Raycast")));

        // Настраиваем высоту камеры
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                playerCamera = Camera.main; // Fallback к основной камере
                if (playerCamera == null)
                {
                    Debug.LogError("PlayerController: Камера игрока не назначена и не найдена!");
                }
            }
        }

        if (playerCamera != null)
        {
            // 1. Ограничиваем дальность прорисовки (Far Clip Plane)
            playerCamera.farClipPlane = maxViewDistance;

        }

        // Изначальная высота камеры
        if (playerCamera != null)
        {
            _targetCameraHeight = eyeHeight;
            _currentCameraHeight = eyeHeight; // Устанавливаем текущую высоту
            playerCamera.transform.localPosition = new Vector3(0, _currentCameraHeight, 0);
        }

        // Блокируем курсор
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Изначально скрываем UI предупреждение
        if (screenDangerCanvasGroup != null)
        {
            screenDangerCanvasGroup.alpha = 0f;
            screenDangerCanvasGroup.interactable = false; // UI не должен перехватывать ввод
            screenDangerCanvasGroup.blocksRaycasts = false; // UI не должен блокировать лучи
        }

        currentStamina = maxStamina;
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
            if (staminaSlider.fillRect != null)
            {
                Image fillImage = staminaSlider.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.color = normalStaminaColor;
                }
            }
        }
        if (staminaCanvasGroup != null)
        {
            staminaCanvasGroup.alpha = 0f;
            staminaCanvasGroup.interactable = false; // UI не должен перехватывать ввод
            staminaCanvasGroup.blocksRaycasts = false; // UI не должен блокировать лучи
        }

        // Инициализация аудио источников (если они не назначены в инспекторе)
        SetupAudioSources();
    }

    void Update()
    {
        if (!isHiding)
        {
            HandleMovement();
            HandleMouseLook();
            HandleStamina(); // Обновление стамины
            UpdateCameraState(); // Обновление наклона и высоты камеры
            HandleFootsteps(); // Обработка звуков шагов
        }
        UpdateNoiseLevel(); // Обновление уровня шума
        // Обновление прозрачности UI предупреждения
        UpdateScreenDangerWarning();
        UpdateStaminaUI(); // Обновление UI стамины
    }

    // Методы для новой системы ввода
    public void OnMove(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        isSprinting = context.ReadValueAsButton();
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        isCrouching = context.ReadValueAsButton();
    }

    public void OnHide(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            HandleHidingInput();
        }
    }

    public void OnToggleCursor(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    void HandleMovement()
    {
        if (!canMove || isHiding) return;

        Vector3 move = transform.right * movementInput.x + transform.forward * movementInput.y;

        float speed = walkSpeed;
        float targetNoiseLevel = 0f;
        float targetNoiseRange = 0f;

        // Применяем гравитацию
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Немного прижимаем к земле
        }
        velocity.y += gravity * Time.deltaTime;

        if (isPushingCart && movementInput.y > 0.1f) // Если игрок толкает тележку и движется вперед
        {
            speed = cartPushSpeed * pushSpeedMultiplier; // Скорость игрока равна скорости тележки с множителем
            targetNoiseLevel = 0.5f; // Уровень шума при толкании тележки
            targetNoiseRange = walkNoiseRange; // Range как при ходьбе, или можно сделать отдельный
            isRunning = false; // Игрок не бежит, он толкает
                               // Дополнительно: Отключаем приседание, когда толкаем, чтобы избежать конфликтов.
                               // Можно добавить логику, чтобы это было опционально.
            if (isCrouching) OnCrouch(new InputAction.CallbackContext()); // Имитация отпускания кнопки приседания
            isCrouching = false;

            _targetCameraHeight = eyeHeight; // Камера на обычной высоте
            _targetCameraTiltAngle = 0f; // Без наклона
        }
        else
        {
            // Определяем скорость и уровень шума в зависимости от состояния
            if (isCrouching)
            {
                speed = crouchSpeed;
                targetNoiseLevel = 0.35f;
                targetNoiseRange = crouchNoiseRange;
                _targetCameraHeight = crouchCameraHeight; // Используем абсолютную высоту для приседания
                _targetCameraTiltAngle = 0f; // Нет наклона при приседании
            }
            else // Стоим или бежим
            {
                bool canCurrentlySprint = isSprinting && currentStamina > 0 && !_isExhausted;

                if (canCurrentlySprint && movementInput.magnitude > 0.1f) // Игрок пытается бежать и действительно двигается
                {
                    speed = runSpeed;
                    targetNoiseLevel = 1.0f;
                    targetNoiseRange = runNoiseRange;
                    isRunning = true;
                    _targetCameraHeight = eyeHeight; // При беге камера на обычной высоте
                    _targetCameraTiltAngle = runCameraTilt; // Наклон камеры вперед при беге
                }
                else
                {
                    speed = walkSpeed;
                    targetNoiseLevel = 0.7f;
                    targetNoiseRange = walkNoiseRange;
                    isRunning = false;
                    _targetCameraHeight = eyeHeight; // При ходьбе камера на обычной высоте
                    _targetCameraTiltAngle = 0f; // Возвращаем нормальный наклон камеры
                }
            }
        }
        // Применяем движение к CharacterController
        controller.Move(move * speed * Time.deltaTime + velocity * Time.deltaTime);

        // Обновление шума в зависимости от движения
        if (move.magnitude > 0.1f && !isPushingCart)
        {
            currentNoiseLevel = targetNoiseLevel;
            currentNoiseRange = targetNoiseRange;
        }
        else if (move.magnitude <= 0.1f && !isPushingCart) // Стоим на месте, не толкаем тележку
        {
            isRunning = false;
            currentNoiseLevel = Mathf.Max(0f, currentNoiseLevel - noiseDecayRate * 2f * Time.deltaTime);
            currentNoiseRange = 0f;
            _targetCameraTiltAngle = 0f; // Возвращаем нормальный наклон камеры
            if (!isCrouching) _targetCameraHeight = eyeHeight; // Если не приседаем, то камера на обычной высоте
        }
    }

    void HandleMouseLook()
    {
        if (!canLook || Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        // Вращение игрока по горизонтали (Y-ось)
        transform.Rotate(Vector3.up * mouseX);
    }

    void UpdateCameraState()
    {
        if (playerCamera == null) return;

        // Плавноe изменение высоты камеры
        _currentCameraHeight = Mathf.Lerp(_currentCameraHeight, _targetCameraHeight, Time.deltaTime * cameraSmoothSpeed);
        playerCamera.transform.localPosition = new Vector3(0, _currentCameraHeight, 0);


        if (canLook)
        {
            // Плавноe изменение наклона камеры
            _currentCameraTiltAngle = Mathf.Lerp(_currentCameraTiltAngle, _targetCameraTiltAngle, Time.deltaTime * cameraSmoothSpeed);

            // Применяем основной поворот от мыши (xRotation) и дополнительный наклон (_currentCameraTiltAngle)
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation - _currentCameraTiltAngle, 0f, 0f);
        }
    }

    void HandleHidingInput()
    {
        if (canHide && !isHiding && currentHidingSpot != null)
        {
            HidePlayer();
        }
        else if (isHiding)
        {
            UnhidePlayer();
        }
        isRunning = false;
        isSprinting = false; // Сбросить спринт при попытке спрятаться/выйти из укрытия
    }

    void HidePlayer()
    {
        if (currentHidingSpot == null) return;

        isHiding = true;
        currentNoiseLevel = 0f;
        currentNoiseRange = 0f;
        isRunning = false;
        isSprinting = false; // Нельзя спринтить в укрытии

        // Останавливаем все звуки шагов
        StopAllFootstepSounds();

        // Temporarily disable CharacterController for direct transform manipulation
        if (controller != null)
        {
            controller.enabled = false;
        }

        // Calculate target position and rotation based on HidingSpot properties
        Vector3 targetPosition = currentHidingSpot.transform.TransformPoint(currentHidingSpot.hidingPositionOffset);
        Quaternion targetRotation = currentHidingSpot.transform.rotation * Quaternion.Euler(0, currentHidingSpot.hidingYRotationOffset, 0);
        float targetEyeHeightOverride = eyeHeight; // Используем это для камеры внутри укрытия

        if (currentHidingSpot.hidingCameraEyeHeightOverride > 0)
        {
            targetEyeHeightOverride = currentHidingSpot.hidingCameraEyeHeightOverride;
        }
        else
        {
            // Default camera height logic based on HidingType
            switch (currentHidingSpot.type)
            {
                case HidingSpot.HidingType.Underneath:
                    targetEyeHeightOverride = crouchCameraHeight / 2f; // Немного ниже, чем обычное приседание
                    break;
                case HidingSpot.HidingType.Cabinet:
                    targetEyeHeightOverride = eyeHeight; // Normal height
                    break;
                case HidingSpot.HidingType.Chest:
                    targetEyeHeightOverride = crouchCameraHeight; // Как при приседании
                    break;
            }
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;

        // Re-enable CharacterController
        if (controller != null)
        {
            controller.enabled = true;
        }

        if (playerRenderer != null)
            playerRenderer.enabled = false;

        // Update camera position and reset vertical look
        if (playerCamera != null)
        {
            _targetCameraHeight = targetEyeHeightOverride;
            _currentCameraHeight = targetEyeHeightOverride;
            _targetCameraTiltAngle = 0f;
            _currentCameraTiltAngle = 0f;

            playerCamera.transform.localPosition = new Vector3(0, _currentCameraHeight, 0);
            xRotation = 0f; // Reset vertical camera rotation
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }

        Debug.Log("Player hidden in " + currentHidingSpot.gameObject.name + " of type " + currentHidingSpot.type);

        currentHidingSpot.HidePlayer(this); // Tell the hiding spot it's occupied
        SetScreenDangerWarningState(false);
    }

    void UnhidePlayer()
    {
        if (currentHidingSpot == null) return; // Ensure there's a hiding spot to exit from

        isHiding = false;
        isRunning = false;
        isSprinting = false; // Сбросить спринт при выходе из укрытия

        if (playerRenderer != null)
            playerRenderer.enabled = true;

        // Calculate target exit position and rotation
        Vector3 targetExitPosition = currentHidingSpot.transform.TransformPoint(currentHidingSpot.exitPositionOffset);
        Quaternion targetExitRotation = currentHidingSpot.transform.rotation * Quaternion.Euler(0, currentHidingSpot.exitYRotationOffset, 0);

        // Temporarily disable CharacterController for direct transform manipulation
        if (controller != null)
        {
            controller.enabled = false;
        }

        // Teleport player to the exit point
        transform.position = targetExitPosition;
        transform.rotation = targetExitRotation;

        // Re-enable CharacterController
        if (controller != null)
        {
            controller.enabled = true;
        }

        // Reset camera height to normal eye height
        if (playerCamera != null)
        {
            _targetCameraHeight = eyeHeight;
            _targetCameraTiltAngle = 0f; // Сбросить наклон
            xRotation = 0f; // Reset vertical camera rotation
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }

        Debug.Log("Player left hiding spot: " + currentHidingSpot.gameObject.name);
        currentHidingSpot.ReleasePlayer(this);
        currentHidingSpot = null;
    }

    Vector3 CalculateOptimalViewDirection()
    {
        if (currentHidingSpot == null) return transform.forward;

        Vector3 rayOrigin = transform.position + Vector3.up * playerCamera.transform.localPosition.y; // Use actual camera height
        float currentHidingViewDistance = minViewDistance; // Could be adjusted per hiding spot type

        int rayCount = 16;
        float bestScore = -Mathf.Infinity;
        Vector3 bestDirection = transform.forward;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = i * (360f / rayCount);
            // Rotate around the player's Y axis
            Vector3 testDirection = Quaternion.Euler(0, angle, 0) * transform.forward;

            float score = EvaluateViewDirection(rayOrigin, testDirection, currentHidingViewDistance);
            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = testDirection;
            }
        }

        return bestDirection;
    }

    float EvaluateViewDirection(Vector3 fromPosition, Vector3 direction, float maxDist)
    {
        float score = 0f;

        RaycastHit hit;
        if (Physics.Raycast(fromPosition, direction, out hit, maxDist, raycastMask))
        {
            float distance = hit.distance;
            if (distance < minViewDistance) // Using a constant for minimal viewing distance
            {
                score -= 100f; // Penalize if too close to an obstacle
            }
            else
            {
                score += distance; // Reward for longer view distance
            }
        }
        else
        {
            score += maxDist * 2f; // Reward for clear view to max distance
        }

        // Check side directions for wider field of view
        Vector3[] sideDirections = {
        Quaternion.Euler(0, 30, 0) * direction,
        Quaternion.Euler(0, -30, 0) * direction,
        Quaternion.Euler(0, 15, 0) * direction,
        Quaternion.Euler(0, -15, 0) * direction
    };

        foreach (Vector3 sideDir in sideDirections)
        {
            if (Physics.Raycast(fromPosition, sideDir, out hit, maxDist, raycastMask))
            {
                float distance = hit.distance;
                if (distance > minViewDistance)
                {
                    score += distance * 0.25f;
                }
                else
                {
                    score -= 20f;
                }
            }
            else
            {
                score += maxDist * 0.25f;
            }
        }

        return score;
    }

    void UpdateNoiseLevel()
    {
        // Если игрок спрятан, шума нет
        if (isHiding)
        {
            currentNoiseLevel = 0f;
            currentNoiseRange = 0f;
        }
        // В противном случае шум генерируется и затухает в HandleMovement
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HidingSpot"))
        {
            canHide = true;
            currentHidingSpot = other.GetComponent<HidingSpot>();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("HidingSpot"))
        {
            canHide = false;
            if (!isHiding) // Если игрок не прячется, очищаем ссылку
            {
                currentHidingSpot = null;
            }
        }
    }

    public void SetMovementEnabled(bool enable)
    {
        canMove = enable;
        if (!enable) movementInput = Vector2.zero;
    }

    public void SetLookEnabled(bool enable)
    {
        canLook = enable;
    }

    // Новый публичный метод для установки состояния UI предупреждения
    public void SetScreenDangerWarningState(bool active)
    {
        _isWarningActive = active;
        ScreenDangerWarningState = active;
    }

    // Метод для плавного изменения прозрачности UI предупреждения
    void UpdateScreenDangerWarning()
    {
        if (screenDangerCanvasGroup == null) return;

        float targetAlpha = 0f;
        if (_isWarningActive && !isHiding) // Показываем предупреждение только если активно и игрок не прячется
        {
            targetAlpha = maxScreenDangerAlpha;
        }

        // Плавно изменяем прозрачность
        screenDangerCanvasGroup.alpha = Mathf.Lerp(screenDangerCanvasGroup.alpha, targetAlpha, Time.deltaTime * screenDangerFadeSpeed);
    }

    public bool IsMovingForward()
    {
        return movementInput.y > 0.1f;
    }

    void HandleStamina()
    {
        if (isHiding) // Стамина не расходуется и не восстанавливается в укрытии, остается как есть.
        {
            return;
        }

        // Обработка состояния "отдышки"
        if (_isExhausted)
        {
            _exhaustionTimer -= Time.deltaTime;
            if (_exhaustionTimer <= 0f)
            {
                _isExhausted = false;
                Debug.Log("Player is no longer exhausted.");
            }
            // В состоянии "отдышки" стамина не восстанавливается
            return;
        }

        // Если не в "отдышке", то восстанавливаем или расходуем стамину
        if (isSprinting && movementInput.magnitude > 0.1f) // Бег
        {
            currentStamina -= staminaDrainRateRun * Time.deltaTime;
            currentStamina = Mathf.Max(0f, currentStamina); // Убеждаемся, что стамина не опускается ниже нуля

            if (currentStamina <= 0f)
            {
                // Если стамина закончилась, переходим в состояние "отдышки"
                _isExhausted = true;
                _exhaustionTimer = exhaustionDuration;
                // isSprinting = false; // Нет необходимости принудительно отключать здесь, HandleMovement уже это сделает
                Debug.Log("Player exhausted! Entering recovery state.");
            }
        }
        else if (isCrouching) // Передвижение в полуприсяде или стоя в полуприсяде
        {
            currentStamina += staminaRegenRateCrouch * Time.deltaTime;
            currentStamina = Mathf.Min(maxStamina, currentStamina);
        }
        else // Ходьба или стоим на месте (не в полуприсяде)
        {
            currentStamina += staminaRegenRateWalk * Time.deltaTime;
            currentStamina = Mathf.Min(maxStamina, currentStamina);
        }
    }

    void UpdateStaminaUI()
    {
        if (staminaSlider == null) return;

        // Обновление значения слайдера
        staminaSlider.value = currentStamina;

        // Изменение цвета заливки слайдера в зависимости от состояния
        Image fillImage = staminaSlider.fillRect?.GetComponent<Image>();
        if (fillImage != null)
        {
            if (_isExhausted)
            {
                fillImage.color = exhaustedStaminaColor;
            }
            else if (currentStamina / maxStamina <= lowStaminaThreshold)
            {
                fillImage.color = lowStaminaColor;
            }
            else
            {
                fillImage.color = normalStaminaColor;
            }
        }
        if (staminaCanvasGroup != null)
        {
            float targetAlpha = 0f;

            // Логика отображения UI стамины:
            // 1. Показать, если игрок истощен.
            // 2. Показать, если стамина не полная.
            // 3. Показать, если игрок бежит (даже если стамина полная, чтобы показать расход).
            if (_isExhausted || currentStamina < maxStamina || isRunning)
            {
                targetAlpha = 1f; // Сделать видимым
            }

            // Плавно изменяем прозрачность CanvasGroup
            staminaCanvasGroup.alpha = Mathf.Lerp(staminaCanvasGroup.alpha, targetAlpha, Time.deltaTime * staminaUIFadeSpeed);

            // Опционально: отключать интерактивность и блокировку рейкастов, если UI невидимый
            if (staminaCanvasGroup.alpha <= 0.01f && (staminaCanvasGroup.interactable || staminaCanvasGroup.blocksRaycasts))
            {
                staminaCanvasGroup.interactable = false;
                staminaCanvasGroup.blocksRaycasts = false;
            }
            else if (staminaCanvasGroup.alpha > 0.01f && (!staminaCanvasGroup.interactable || !staminaCanvasGroup.blocksRaycasts))
            {

            }
        }
    }

    void SetupAudioSources()
    {
        // Если AudioSource не назначены, пытаемся их добавить как компоненты
        if (walkAudioSource == null) walkAudioSource = gameObject.AddComponent<AudioSource>();
        if (runAudioSource == null) runAudioSource = gameObject.AddComponent<AudioSource>();
        if (crouchAudioSource == null) crouchAudioSource = gameObject.AddComponent<AudioSource>();

        // Общие настройки для всех AudioSource
        AudioSource[] sources = new AudioSource[] { walkAudioSource, runAudioSource, crouchAudioSource };
        foreach (var source in sources)
        {
            source.spatialBlend = 1f; // 3D звук
            source.rolloffMode = AudioRolloffMode.Logarithmic; // Логарифмическое затухание
            source.maxDistance = runNoiseRange * 1.5f; // Максимальная дистанция для слышимости
            source.playOnAwake = false; // Не проигрывать при старте
        }
    }

    public void HandleFootsteps()
    {
        // Не воспроизводим шаги, если  игрок спрятан или не двигается
        if (isHiding || controller.velocity.magnitude < 0.1f)
        {
            StopAllFootstepSounds();
            _currentStepTimer = 0f; // Сбросить таймер, чтобы следующий шаг был вовремя
            return;
        }
        float currentInterval = 0f;
        AudioSource activeSource = null;
        AudioClip[] activeClips = null;

        if (isRunning)
        {
            currentInterval = runStepInterval;
            activeSource = runAudioSource;
            activeClips = runFootstepSounds;
        }
        else if (isCrouching)
        {
            currentInterval = crouchStepInterval;
            activeSource = crouchAudioSource;
            activeClips = crouchFootstepSounds;
        }
        else // Ходьба
        {
            currentInterval = walkStepInterval;
            activeSource = walkAudioSource;
            activeClips = walkFootstepSounds;
        }

        // Останавливаем все остальные звуки
        if (activeSource != walkAudioSource) walkAudioSource.Stop();
        if (activeSource != runAudioSource) runAudioSource.Stop();
        if (activeSource != crouchAudioSource) crouchAudioSource.Stop();


        _currentStepTimer -= Time.deltaTime;

        if (_currentStepTimer <= 0f)
        {
            if (activeSource != null && activeClips != null && activeClips.Length > 0)
            {
                // Воспроизводим случайный звук шага
                activeSource.PlayOneShot(activeClips[Random.Range(0, activeClips.Length)]);
            }
            _currentStepTimer = currentInterval; // Сбросить таймер
        }
    }

    public void StopAllFootstepSounds()
    {
        if (walkAudioSource != null) walkAudioSource.Stop();
        if (runAudioSource != null) runAudioSource.Stop();
        if (crouchAudioSource != null) crouchAudioSource.Stop();
    }

    public void SetPushingCartState(bool isPushing, float cartSpeed)
    {
        isPushingCart = isPushing;
        cartPushSpeed = cartSpeed;

        if (isPushingCart)
        {
            // Когда толкаем, устанавливаем скорости так, чтобы они соответствовали тележке
            // (с учетом множителя замедления)
            walkSpeed = cartPushSpeed * pushSpeedMultiplier;
            runSpeed = cartPushSpeed * pushSpeedMultiplier; // Бег также ограничен скоростью тележки
            crouchSpeed = cartPushSpeed * pushSpeedMultiplier; // Приседание также ограничено

            // Отключаем спринт, если игрок пытается толкать
            isSprinting = false;
            isRunning = false;
            // Отключаем приседание
            isCrouching = false;
            // Сбросить таймер шагов, чтобы звук не "застревал"
            _currentStepTimer = 0f;
            StopAllFootstepSounds();

        }
        else
        {
            // Возвращаем оригинальные скорости, когда прекращаем толкать
            walkSpeed = originalWalkSpeed;
            runSpeed = originalRunSpeed;
            crouchSpeed = originalCrouchSpeed;
            Debug.Log("Player stopped pushing cart. Speeds reset.");
        }
    }

    public void SetMouseSensitivityDirectly(float sensitivity)
    {
        mouseSensitivity = sensitivity;
        Debug.Log($"PlayerController: Чувствительность мыши установлена на {sensitivity}");
    }

    /// <summary>
    /// Включает или отключает управление движением игрока (WASD).
    /// </summary>
    /// <param name="enable">True для включения, False для отключения.</param>
    public void EnableMovement(bool enable)
    {
        canMove = enable;
        if (!enable)
        {
            movementInput = Vector2.zero; // Сбросить ввод движения, чтобы игрок не продолжал двигаться
        }
        Debug.Log($"PlayerController: Movement {(enable ? "Enabled" : "Disabled")}.");
    }

    /// <summary>
    /// Включает или отключает управление камерой игрока (мышь).
    /// </summary>
    /// <param name="enable">True для включения, False для отключения.</param>
    public void EnableCameraLook(bool enable)
    {
        canLook = enable;
        if (!enable)
        {
            // Если камера отключена, можно временно сбросить lookInput, чтобы не было "скачка" при включении
            lookInput = Vector2.zero;
        }
        Debug.Log($"PlayerController: Camera Look {(enable ? "Enabled" : "Disabled")}.");
    }

}