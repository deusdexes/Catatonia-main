using UnityEngine;
using UnityEngine.InputSystem;

public class TransitionTrigger : MonoBehaviour
{
    [Header("Transition Settings")]
    public string targetSceneName;

    [Header("Input Settings")]
    public InputActionReference interactionAction;
    public string interactionBindingPath = "<Keyboard>/e";

    [Header("Visual Feedback")]
    public GameObject interactionPrompt;

    [Header("Debug")]
    public bool showDebugGizmos = true;

    private bool playerInTrigger = false;
    private PlayerController playerController;
    private Collider triggerCollider;
    private InputAction interactionInputAction;

    private void Start()
    {
        Debug.Log($"TransitionTrigger started on {gameObject.name}");

        triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            Debug.LogError("No Collider found on TransitionTrigger!");
            return;
        }

        if (!triggerCollider.isTrigger)
        {
            Debug.LogError("Collider is not set as Trigger! Please enable 'Is Trigger'");
        }

        Debug.Log($"Trigger bounds: {triggerCollider.bounds}, Center: {triggerCollider.bounds.center}");

        // Настройка системы ввода
        SetupInputSystem();

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
        else
            Debug.LogWarning("InteractionPrompt not assigned!");

        // Поиск игрока в сцене для диагностики
        FindPlayerForDebug();
    }

    private void FindPlayerForDebug()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Debug.Log($"Found player: {player.name}, Position: {player.transform.position}");
            Debug.Log($"Distance to player: {Vector3.Distance(transform.position, player.transform.position)}");

            // Проверяем коллайдеры игрока
            Collider[] playerColliders = player.GetComponents<Collider>();
            Debug.Log($"Player has {playerColliders.Length} colliders:");
            foreach (Collider col in playerColliders)
            {
                Debug.Log($"- {col.GetType().Name}, IsTrigger: {col.isTrigger}, Enabled: {col.enabled}");
            }
        }
        else
        {
            Debug.LogError("No GameObject with tag 'Player' found in scene!");
        }
    }

    private void SetupInputSystem()
    {
        if (interactionAction == null)
        {
            interactionInputAction = new InputAction("TransitionInteraction", InputActionType.Button);
            interactionInputAction.AddBinding(interactionBindingPath);
            interactionInputAction.Enable();
            Debug.Log("Created default Input Action for interaction");
        }
        else
        {
            interactionInputAction = interactionAction.action;
            if (!interactionInputAction.enabled)
                interactionInputAction.Enable();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"OnTriggerEnter: {other.gameObject.name}, Tag: '{other.tag}', Layer: {other.gameObject.layer}, Type: {other.GetType()}");

        // Проверяем все возможные условия с более широкими критериями
        bool isPlayer = other.CompareTag("Player");
        bool isPlayerChild = other.transform.parent != null && other.transform.parent.CompareTag("Player");
        bool hasPlayerController = other.GetComponent<PlayerController>() != null;
        bool hasPlayerControllerInParent = other.GetComponentInParent<PlayerController>() != null;

        Debug.Log($"Player check - Direct: {isPlayer}, Child: {isPlayerChild}, HasController: {hasPlayerController}, HasParentController: {hasPlayerControllerInParent}");

        if (isPlayer || isPlayerChild || hasPlayerController || hasPlayerControllerInParent)
        {
            playerInTrigger = true;
            playerController = other.GetComponent<PlayerController>();

            if (playerController == null)
            {
                playerController = other.GetComponentInParent<PlayerController>();
                Debug.Log($"Found PlayerController in parent: {playerController != null}");
            }

            if (playerController == null)
            {
                // Последняя попытка - найти в сцене
                playerController = FindFirstObjectByType<PlayerController>();
                Debug.Log($"Found PlayerController in scene: {playerController != null}");
            }

            Debug.Log($"Player entered trigger. PlayerController found: {playerController != null}");

            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(true);
                Debug.Log("Interaction prompt shown");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"OnTriggerExit: {other.gameObject.name}, Tag: '{other.tag}'");

        bool isPlayer = other.CompareTag("Player");
        bool isPlayerChild = other.transform.parent != null && other.transform.parent.CompareTag("Player");
        bool hasPlayerController = other.GetComponent<PlayerController>() != null;

        if (isPlayer || isPlayerChild || hasPlayerController)
        {
            playerInTrigger = false;
            playerController = null;

            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
                Debug.Log("Interaction prompt hidden");
            }
        }
    }

    private void Update()
    {
        // Дополнительная проверка каждый кадр
        if (playerInTrigger && playerController == null)
        {
            Debug.LogWarning("Player in trigger but PlayerController is null!");
            playerInTrigger = false;
            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);
        }

        // Проверка ввода через Input System
        if (playerInTrigger && interactionInputAction != null && interactionInputAction.triggered)
        {
            Debug.Log($"Interaction triggered via Input System");
            StartTransition();
        }

        // Ручная проверка расстояния для диагностики (каждые 2 секунды)
        if (Time.frameCount % 120 == 0)
        {
            CheckPlayerDistance();
        }
    }

    private void CheckPlayerDistance()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && !playerInTrigger)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < 5f)
            {
                Debug.Log($"Player is close ({distance:F1} units) but not in trigger. Possible collider issue.");
            }
        }
    }

    private void StartTransition()
    {
        Debug.Log("StartTransition called");

        if (playerController != null)
        {
            playerController.enabled = false;
            Debug.Log("PlayerController disabled");

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError("SceneTransitionManager not found!");
            // Возвращаем управление
            if (playerController != null)
            {
                playerController.enabled = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            Gizmos.color = playerInTrigger ? Color.green : new Color(1, 1, 0, 0.5f);

            if (collider is BoxCollider boxCollider)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
                Gizmos.DrawCube(boxCollider.center, boxCollider.size);
            }
            else if (collider is SphereCollider sphereCollider)
            {
                Gizmos.DrawWireSphere(transform.position + sphereCollider.center, sphereCollider.radius);
            }
        }

        // Дополнительная визуализация - линия к игроку
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Gizmos.color = playerInTrigger ? Color.green : Color.white;
            Gizmos.DrawLine(transform.position, player.transform.position);
        }
    }
}