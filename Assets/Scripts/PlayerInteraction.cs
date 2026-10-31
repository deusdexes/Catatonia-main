using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("Максимальное расстояние, на котором игрок может взаимодействовать с объектами.")]
    public float interactionRange = 3f;
    [Tooltip("Слой(и), на котором(ых) находятся взаимодействующие объекты.")]
    public LayerMask interactableLayer;

    [Header("Component References")]
    [Tooltip("Ссылка на инвентарь игрока. Если не задано, будет найдено автоматически.")]
    public Inventory playerInventory;

    [Tooltip("Текстовый компонент TextMeshProUGUI для отображения подсказки о взаимодействии.")]
    public TextMeshProUGUI interactionPromptText;

    private Camera playerCamera;
    private GameObject currentInteractableObject; // Ссылка на текущий объект, с которым можно взаимодействовать
    private OutlineEffect currentOutlineEffect; // Ссылка на компонент OutlineEffect текущего интерактивного объекта

    void Start()
    {
        Debug.Log("PlayerInteraction: Start.");
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogError("PlayerInteraction: Камера игрока не найдена ни как дочерний объект, ни как главная камера в сцене! Убедитесь, что камера имеет тег 'MainCamera'.");
            }
        }
        Debug.Log($"PlayerInteraction: Камера игрока найдена: {playerCamera?.name ?? "NULL"}.");

        if (playerInventory == null)
        {
            playerInventory = FindAnyObjectByType<Inventory>();
            if (playerInventory == null)
            {
                Debug.LogWarning("PlayerInteraction: Инвентарь игрока не назначен и не найден в сцене! Пожалуйста, назначьте его или убедитесь, что в сцене есть компонент Inventory.");
            }
        }
        Debug.Log($"PlayerInteraction: Инвентарь игрока: {playerInventory?.name ?? "NULL"}.");


        if (interactionPromptText != null)
        {
            interactionPromptText.gameObject.SetActive(false);
            Debug.Log("PlayerInteraction: Подсказка взаимодействия скрыта при старте.");
        }
        else
        {
            Debug.LogWarning("PlayerInteraction: interactionPromptText не назначен. Подсказка взаимодействия не будет отображаться.");
        }
    }

    void Update()
    {
        CheckForInteractable();
    }

    private void CheckForInteractable()
    {
        RaycastHit hit;
        if (playerCamera == null)
        {
            return;
        }

        GameObject newInteractableObject = null;
        OutlineEffect newOutlineEffect = null;

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, interactionRange, interactableLayer))
        {
            bool isUIPanelCurrentlyOpen = IsUIPanelOpen();

            if (!isUIPanelCurrentlyOpen)
            {
                // Получение всех потенциальных компонентов
                NotePickup note = hit.collider.GetComponent<NotePickup>();
                ItemPickup item = hit.collider.GetComponent<ItemPickup>();
                RobotAssemblyController robot = hit.collider.GetComponent<RobotAssemblyController>();
                KeycardDoor keycardDoor = hit.collider.GetComponent<KeycardDoor>();
                GeneratorLever generatorLever = hit.collider.GetComponent<GeneratorLever>();
                MagicSquareMonitor monitor = hit.collider.GetComponent<MagicSquareMonitor>();
                SpawnBlockTerminal spawnTerminal = hit.collider.GetComponent<SpawnBlockTerminal>();
                MonsterRedirectTerminal redirectTerminal = hit.collider.GetComponent<MonsterRedirectTerminal>();
                MovableObstacle movableObstacle = hit.collider.GetComponent<MovableObstacle>();
                LevelTransitionInteractable levelTransition = hit.collider.GetComponent<LevelTransitionInteractable>();
                Lever lever = hit.collider.GetComponent<Lever>();
                // --- НОВОЕ: Проверка на InteractablePuzzleTrigger ---
                InteractablePuzzleTrigger puzzleTrigger = hit.collider.GetComponent<InteractablePuzzleTrigger>();

                if (lever == null && hit.collider.transform.parent != null)
                {
                    lever = hit.collider.transform.parent.GetComponent<Lever>();
                }


                // Если найден хоть один интерактивный компонент
                if (note != null || item != null || robot != null || keycardDoor != null || generatorLever != null || monitor != null || spawnTerminal != null || redirectTerminal != null || movableObstacle != null || lever != null || puzzleTrigger != null || levelTransition != null)
                {
                    newInteractableObject = hit.collider.gameObject;
                    newOutlineEffect = newInteractableObject.GetComponent<OutlineEffect>();

                    if (currentInteractableObject != newInteractableObject)
                    {
                        // Сбросить обводку с предыдущего объекта, если он был
                        if (currentOutlineEffect != null)
                        {
                            currentOutlineEffect.DisableOutline();
                            Debug.Log($"PlayerInteraction: Отключена обводка с {currentInteractableObject.name}.");
                        }

                        currentInteractableObject = newInteractableObject;
                        currentOutlineEffect = newOutlineEffect;

                        if (currentOutlineEffect != null)
                        {
                            currentOutlineEffect.EnableOutline();
                            Debug.Log($"PlayerInteraction: Включена обводка на {currentInteractableObject.name}.");
                        }

                        // Логирование обнаружения (для дебага)
                        string detectedName = currentInteractableObject.name;
                        if (generatorLever != null) Debug.Log($"PlayerInteraction: Обнаружен GeneratorLever: {detectedName}.");
                        else if (keycardDoor != null) Debug.Log($"PlayerInteraction: Обнаружена KeycardDoor: {detectedName}.");
                        else if (movableObstacle != null) Debug.Log($"PlayerInteraction: Обнаружено MovableObstacle: {detectedName}.");
                        else if (lever != null) Debug.Log($"PlayerInteraction: Обнаружен Lever: {detectedName}.");
                        // --- НОВОЕ: Логирование InteractablePuzzleTrigger ---
                        else if (puzzleTrigger != null) Debug.Log($"PlayerInteraction: Обнаружен InteractablePuzzleTrigger: {detectedName}.");
                        else Debug.Log($"PlayerInteraction: Обнаружен новый интерактивный объект: {detectedName}.");
                    }

                    if (interactionPromptText != null && !interactionPromptText.gameObject.activeSelf)
                    {
                        string promptText = "Нажмите E для взаимодействия"; // Базовый текст

                        // 1. ПРИОРИТЕТ: MovableObstacle (Толкание - требует кнопку 'Push')
                        if (movableObstacle != null && !movableObstacle.IsMoved)
                        {
                            promptText = "Нажмите 'Толкать' (Push) для сдвига препятствия";
                        }
                        // 2. ПРИОРИТЕТ: Генератор
                        else if (generatorLever != null)
                        {
                            promptText = $"Нажмите E для {(generatorLever.isGeneratorOn ? "выключения" : "включения")} генератора";
                        }
                        // 3. ПРИОРИТЕТ: Терминалы (только если не использовались)
                        else if (spawnTerminal != null && spawnTerminal.IsUsed == false) // Используем IsUsed
                        {
                            promptText = "Нажмите E для блокировки точки спавна";
                        }
                        else if (redirectTerminal != null && redirectTerminal.IsUsed == false) // Используем IsUsed
                        {
                            promptText = "Нажмите E для перенаправления монстра";
                        }
                        // 4. ПРИОРИТЕТ: Дверь (только если заперта)
                        else if (keycardDoor != null && !keycardDoor.unlocked)
                        {
                            promptText = $"Нажмите E для открытия двери (ID: {keycardDoor.requiredKeycardID})";
                        }
                        // 5. Lever (без других специфических условий)
                        else if (lever != null)
                        {
                            promptText = "Нажмите E для переключения рычага";
                        }
                        // --- НОВОЕ: Подсказка для InteractablePuzzleTrigger ---
                        else if (puzzleTrigger != null)
                        {
                            promptText = "Нажмите E для взаимодействия с загадкой";
                        }
                        // 6. Все остальные интерактивные объекты (включая использованные терминалы) используют базовый текст

                        interactionPromptText.text = promptText;
                        interactionPromptText.gameObject.SetActive(true);
                        // Debug.Log("PlayerInteraction: Подсказка взаимодействия отображена."); // Закомментировано для уменьшения спама в логах
                    }
                    // Важно: если нашли объект, выходим, чтобы не сбросить состояние обводки
                    return;
                }
            }
        }

        // Если интерактивный объект не найден ИЛИ UI открыт, сбрасываем обводку и подсказку
        if (currentInteractableObject != null)
        {
            if (currentOutlineEffect != null)
            {
                currentOutlineEffect.DisableOutline();
                Debug.Log($"PlayerInteraction: Отключена обводка с {currentInteractableObject.name}.");
            }
            Debug.Log($"PlayerInteraction: Интерактивный объект {currentInteractableObject.name} покинул зону видимости/действия или UI открыт.");
            currentInteractableObject = null;
            currentOutlineEffect = null;
        }
        if (interactionPromptText != null && interactionPromptText.gameObject.activeSelf)
        {
            interactionPromptText.gameObject.SetActive(false);
            // Debug.Log("PlayerInteraction: Подсказка взаимодействия скрыта."); // Закомментировано для уменьшения спама в логах
        }
    }


    private bool IsUIPanelOpen()
    {
        // Предполагаем, что NoteManager.Instance существует и имеет нужные поля.
        AcousticPuzzleManager acousticManager = FindObjectOfType<AcousticPuzzleManager>();
        if (acousticManager != null && acousticManager.IsPuzzleActive())
        {
            return true;
        }

        if (NoteManager.Instance == null) return false;

        bool journalActive = NoteManager.Instance.journalPanel != null && NoteManager.Instance.journalPanel.activeSelf;
        bool noteActive = NoteManager.Instance.notePanel != null && NoteManager.Instance.notePanel.activeSelf;

        return journalActive || noteActive;
    }

    public void OnInteract()
    {
        Debug.Log("PlayerInteraction: OnInteract вызван (нажатие E).");

        bool isUIPanelCurrentlyOpen = IsUIPanelOpen();

        if (isUIPanelCurrentlyOpen)
        {
            Debug.Log("PlayerInteraction: UI журнала/записки/головоломки открыт, взаимодействие заблокировано.");
            return; // Блокируем взаимодействие, если UI открыт
        }

        if (playerCamera == null)
        {
            Debug.LogError("PlayerInteraction: Невозможно выполнить взаимодействие, камера игрока не найдена.");
            return;
        }

        GameObject interactedObject = null;

        if (currentInteractableObject != null)
        {
            interactedObject = currentInteractableObject;
            Debug.Log("PlayerInteraction: Попытка взаимодействия с объектом (из кэша): " + interactedObject.name);
        }
        else
        {
            // Повторный рейкаст, если объект пропал из кэша
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactionRange, interactableLayer))
            {
                interactedObject = hit.collider.gameObject;
            }
            else
            {
                Debug.Log("PlayerInteraction: Интерактивный объект в радиусе действия не найден при нажатии кнопки.");
                return; // Ничего не найдено, выходим
            }
        }

        if (interactedObject != null)
        {
            LevelTransitionInteractable levelTransition = interactedObject.GetComponent<LevelTransitionInteractable>();
            if (levelTransition != null)
            {
                levelTransition.OnInteract();
                return; // Если это переход на уровень, выходим
            }

            InteractablePuzzleTrigger puzzleTrigger = interactedObject.GetComponent<InteractablePuzzleTrigger>();
            if (puzzleTrigger != null)
            {
                puzzleTrigger.Interact(); // Вызываем новый метод
                return;
            }

            // 1. ItemPickup
            ItemPickup pickup = interactedObject.GetComponent<ItemPickup>();
            if (pickup != null)
            {
                if (playerInventory != null)
                {
                    pickup.OnInteract();
                    currentInteractableObject = null; // Сброс, так как предмет обычно исчезает
                    if (currentOutlineEffect != null) { currentOutlineEffect.DisableOutline(); currentOutlineEffect = null; }
                    return;
                }
            }

            // 2. RobotAssemblyController
            RobotAssemblyController robotController = interactedObject.GetComponent<RobotAssemblyController>();
            if (robotController != null)
            {
                if (playerInventory != null)
                {
                    robotController.OnInteract(playerInventory);
                    return;
                }
            }

            // 3. NotePickup
            NotePickup notePickup = interactedObject.GetComponent<NotePickup>();
            if (notePickup != null)
            {
                if (notePickup.noteData == null || NoteManager.Instance == null) return;

                NoteManager.Instance.AddNoteToJournal(notePickup.noteData);
                NoteManager.Instance.DisplayNote(notePickup.noteData, notePickup.gameObject);
                currentInteractableObject = null; // Сброс, так как записка обычно исчезает
                if (currentOutlineEffect != null) { currentOutlineEffect.DisableOutline(); currentOutlineEffect = null; }
                return;
            }

            // 4. KeycardDoor
            KeycardDoor keycardDoor = interactedObject.GetComponent<KeycardDoor>();
            if (keycardDoor != null)
            {
                if (playerInventory != null)
                {
                    keycardDoor.TryOpenWithKeycard(playerInventory);
                    return;
                }
            }

            // 5. GeneratorLever
            GeneratorLever generatorLever = interactedObject.GetComponent<GeneratorLever>();
            if (generatorLever != null)
            {
                generatorLever.OnInteract();
                return;
            }

            // 6. MagicSquareMonitor
            MagicSquareMonitor monitor = interactedObject.GetComponent<MagicSquareMonitor>();
            if (monitor != null)
            {
                monitor.OnInteract();
                return;
            }

            // 7. SpawnBlockTerminal
            SpawnBlockTerminal spawnTerminal = interactedObject.GetComponent<SpawnBlockTerminal>();
            if (spawnTerminal != null && spawnTerminal.IsUsed == false) // Проверка на использование
            {
                spawnTerminal.OnInteract();
                currentInteractableObject = null; // Сброс, если терминал одноразовый
                if (currentOutlineEffect != null) { currentOutlineEffect.DisableOutline(); currentOutlineEffect = null; }
                return;
            }

            // 8. MonsterRedirectTerminal
            MonsterRedirectTerminal redirectTerminal = interactedObject.GetComponent<MonsterRedirectTerminal>();
            if (redirectTerminal != null && redirectTerminal.IsUsed == false) // Проверка на использование
            {
                redirectTerminal.OnInteract();
                currentInteractableObject = null; // Сброс, если терминал одноразовый
                if (currentOutlineEffect != null) { currentOutlineEffect.DisableOutline(); currentOutlineEffect = null; }
                return;
            }

            // 9. Lever
            Lever lever = interactedObject.GetComponent<Lever>();
            if (lever == null && interactedObject.transform.parent != null) // Если не нашли на объекте, ищем на родителе
            {
                lever = interactedObject.transform.parent.GetComponent<Lever>();
            }

            if (lever != null)
            {
                Debug.Log("PlayerInteraction: Найден Lever. Вызываем OnInteract().");
                lever.OnInteract(); // Вызываем OnInteract из Lever
                return;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactionRange);
        }
    }
}