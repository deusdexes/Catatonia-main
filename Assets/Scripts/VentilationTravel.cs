using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class VentilationTravel : MonoBehaviour
{
    [Header("Настройки")]
    public PlayerController playerController;
    public Camera playerCamera;
    public Transform playerSpawnPoint; // Точка, куда игрок появится после выхода из этой вентиляции
    public VentilationTravel otherVentilation;

    public float travelDuration = 2.0f;
    public float interactionCooldown = 0.5f; // Время задержки перед повторным взаимодействием после выхода

    [Header("Настройки камеры в вентиляции")]
    public List<Transform> cameraPathPoints; // Список точек для пролета камеры внутри ЭТОЙ вентиляции
    public float cameraPathSpeed = 1.0f; // Скорость движения камеры по пути

    // Слой, на который будем временно переключать игрока
    // Укажи в инспекторе слой, который NPC игнорируют (например, "Ignore Raycast")
    public LayerMask ignoreNPCLayer;

    private InputSystem_Actions playerControls;

    private bool playerInRange = false;
    private CharacterController playerCharacterController;
    private Animator playerAnimator;

    private Transform originalCameraParent;
    private Vector3 originalCameraLocalPosition;
    private Quaternion originalCameraLocalRotation;

    private bool isTraveling = false;
    private float lastInteractionTime = -Mathf.Infinity;

    private int originalPlayerLayer; // Сохраняем исходный слой игрока
    // Позиция вне карты для игрока, чтобы NPC его не видели
    private Vector3 playerSafePosition = new Vector3(0, -1000, 0);

    void Awake()
    {
        playerControls = new InputSystem_Actions();
        playerControls.Player.Interact.performed += ctx => OnInteract();
    }

    void OnEnable()
    {
        playerControls.Enable();
    }

    void OnDisable()
    {
        playerControls.Disable();
    }

    void Start()
    {
        if (playerController == null) { Debug.LogError("PlayerController не назначен в " + gameObject.name + "!"); enabled = false; return; }
        if (playerCamera == null) { playerCamera = playerController.playerCamera; }
        if (playerCamera == null) { Debug.LogError("Player Camera не назначена в " + gameObject.name + " или не найдена в PlayerController!"); enabled = false; return; }

        if (playerSpawnPoint == null) { Debug.LogError("Player Spawn Point не назначен в " + gameObject.name + "!"); enabled = false; return; }
        if (otherVentilation == null) { Debug.LogWarning("Other Ventilation не назначена в " + gameObject.name + ". Переход будет односторонним."); }

        playerCharacterController = playerController.GetComponent<CharacterController>();
        if (playerCharacterController == null) { Debug.LogWarning("У игрока нет CharacterController. Отключаются другие коллайдеры."); }

        playerAnimator = playerController.GetComponent<Animator>();
        if (playerAnimator == null) { Debug.LogWarning("У игрока нет Animator. Анимации не будут проигрываться."); }

        if (cameraPathPoints.Count < 2) { Debug.LogWarning("Путь для камеры должен содержать минимум 2 точки для вентиляции " + gameObject.name + "."); }

        originalCameraParent = playerCamera.transform.parent;
        originalCameraLocalPosition = playerCamera.transform.localPosition;
        originalCameraLocalRotation = playerCamera.transform.localRotation;

        originalPlayerLayer = playerController.gameObject.layer;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == playerController)
        {
            if (!isTraveling && Time.time >= lastInteractionTime + interactionCooldown)
            {
                playerInRange = true;
                Debug.Log("Игрок подошел к вентиляции " + gameObject.name + ". Нажмите 'F' для взаимодействия.");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == playerController)
        {
            if (!isTraveling)
            {
                playerInRange = false;
                Debug.Log("Игрок отошел от вентиляции " + gameObject.name + ".");
            }
        }
    }

    private void OnInteract()
    {
        if (playerInRange && !isTraveling && Time.time >= lastInteractionTime + interactionCooldown)
        {
            StartCoroutine(TravelThroughVent());
        }
    }

    IEnumerator TravelThroughVent()
    {
        isTraveling = true;
        lastInteractionTime = Time.time;
        Debug.Log("Начато перемещение по вентиляции " + gameObject.name + "!");

        // --- 1. Отключаем управление игроком и камерой ---
        playerController.EnableMovement(false);
        playerController.EnableCameraLook(false);
        playerController.SetScreenDangerWarningState(false);
        playerController.StopAllFootstepSounds();

        playerInRange = false;

        // --- 2. Мгновенно отключаем коллайдер игрока и переводим его на безопасный слой ---
        if (playerCharacterController != null)
        {
            playerCharacterController.enabled = false;
        }
        else
        {
            foreach (Collider col in playerController.GetComponents<Collider>())
            {
                col.enabled = false;
            }
        }

        // Перемещаем игрока в безопасную, недоступную для NPC позицию
        playerController.transform.position = playerSafePosition;

        // Временно меняем слой игрока на тот, который NPC игнорируют
        playerController.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast"); // Убедитесь, что такой слой существует и NPC его игнорируют!

        // --- 3. Проигрываем анимацию входа в вентиляцию ---
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsEnteringVent", true);
            yield return new WaitForSeconds(playerAnimator.GetCurrentAnimatorStateInfo(0).length);
            playerAnimator.SetBool("IsEnteringVent", false);
        }

        // --- 4. Скрываем рендер игрока и готовим камеру для пролета ---
        Renderer playerRenderer = playerController.GetComponentInChildren<Renderer>();
        if (playerRenderer != null) playerRenderer.enabled = false;

        // Отвязываем камеру от игрока для пролета
        playerCamera.transform.parent = null;

        if (cameraPathPoints.Count > 1)
        {
            for (int i = 0; i < cameraPathPoints.Count - 1; i++)
            {
                Transform currentPoint = cameraPathPoints[i];
                Transform nextPoint = cameraPathPoints[i + 1];

                float segmentDuration = Vector3.Distance(currentPoint.position, nextPoint.position) / cameraPathSpeed;
                float segmentElapsedTime = 0;

                while (segmentElapsedTime < segmentDuration)
                {
                    playerCamera.transform.position = Vector3.Lerp(currentPoint.position, nextPoint.position, segmentElapsedTime / segmentDuration);
                    playerCamera.transform.rotation = Quaternion.Slerp(currentPoint.rotation, nextPoint.rotation, segmentElapsedTime / segmentDuration);
                    segmentElapsedTime += Time.deltaTime;
                    yield return null;
                }
                playerCamera.transform.position = nextPoint.position;
                playerCamera.transform.rotation = nextPoint.rotation;
            }
        }
        else
        {
            Debug.LogWarning("Недостаточно точек для пролета камеры, пропускаем пролет. Вентиляция: " + gameObject.name);
            playerCamera.transform.position = playerSpawnPoint.position;
            playerCamera.transform.rotation = playerSpawnPoint.rotation;
        }

        // --- 5. Появляем игрока в новой точке и возвращаем камеру ---
        playerController.transform.position = playerSpawnPoint.position; // Перемещаем игрока к точке появления

        // !!! ИЗМЕНЕНИЕ ЗДЕСЬ: Установка вращения в Quaternion.identity !!!
        playerController.transform.rotation = Quaternion.identity; // Устанавливает вращение в (0,0,0) по всем осям

        playerCamera.transform.parent = originalCameraParent;
        playerCamera.transform.localPosition = originalCameraLocalPosition;
        playerCamera.transform.localRotation = originalCameraLocalRotation;

        // Включаем рендер игрока
        if (playerRenderer != null) playerRenderer.enabled = true;

        // --- 6. Проигрываем анимацию выхода из вентиляции ---
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsExitingVent", true);
            yield return new WaitForSeconds(playerAnimator.GetCurrentAnimatorStateInfo(0).length);
            playerAnimator.SetBool("IsExitingVent", false);
        }

        // --- 7. Включаем коллайдер игрока, возвращаем слой и управление ---
        if (playerCharacterController != null)
        {
            playerCharacterController.enabled = true;
        }
        else
        {
            foreach (Collider col in playerController.GetComponents<Collider>())
            {
                col.enabled = true;
            }
        }

        // Возвращаем игрока на его оригинальный слой
        playerController.gameObject.layer = originalPlayerLayer;

        playerController.EnableMovement(true);
        playerController.EnableCameraLook(true);

        yield return new WaitForSeconds(interactionCooldown);

        Debug.Log("Перемещение по вентиляции " + gameObject.name + " завершено! Игрок появился в " + playerSpawnPoint.name);
        isTraveling = false;
    }
}