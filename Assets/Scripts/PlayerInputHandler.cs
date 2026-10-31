using UnityEngine;
using UnityEngine.InputSystem; // Необходимо для CallbackContext
public class PlayerInputHandler : MonoBehaviour
{
    [Header("Component References")]
    public Inventory inventory;
    public PlayerInteraction playerInteraction;
    [Tooltip("Ссылка на контроллер обучения")]
    public TutorialController tutorialController;

    [Tooltip("Ссылка на контроллер тележки. Необходим для управления толканием.")]
    public CartController cartController;

    // Флаг, который показывает, нажата ли кнопка "Толкать"
    private bool _isPushInputHeld = false;

    // Ссылка на компонент PlayerInput
    private PlayerInput _playerInput; // Добавлено

    // Методы для вызова из Unity Events
    public NoteManager noteManager;
    public MagicSquareGameManager magicSquareManager;

    void Start()
    {
        if (cartController == null)
        {
            cartController = FindFirstObjectByType<CartController>();
            if (cartController == null)
            {
                Debug.LogWarning("PlayerInputHandler: CartController не назначен и не найден в сцене!", this);
            }
        }

        // Инициализация PlayerInput
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput == null)
        {
            Debug.LogError("PlayerInputHandler: Компонент PlayerInput не найден на этом GameObject!");
        }
    }

    public void OnSlot1()
    {
        if (inventory != null)
            inventory.OnSlot1();
    }

    public void OnSlot2()
    {
        if (inventory != null)
            inventory.OnSlot2();
    }

    public void OnSlot3()
    {
        if (inventory != null)
            inventory.OnSlot3();
    }

    public void OnDrop()
    {
        if (inventory != null)
            inventory.OnDrop();
    }

    public void OnUseItem()
    {
        if (inventory != null)
            inventory.OnUseEquippedItem();
    }

    public void OnPush(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _isPushInputHeld = true;
            Debug.Log("Push input STARTED");
        }
        else if (context.canceled)
        {
            _isPushInputHeld = false;
            Debug.Log("Push input CANCELED");
        }

    }

    public bool IsPushInputHeld()
    {
        return _isPushInputHeld;
    }

    public void OnInteract()
    {

        if (NoteManager.Instance != null && (NoteManager.Instance.journalPanel.activeSelf || NoteManager.Instance.notePanel.activeSelf))
        {
            // Если открыт журнал или отдельная записка, не позволяем взаимодействовать с миром
            Debug.Log("PlayerInputHandler: UI журнала/записки открыт, взаимодействие заблокировано.");
            return;
        }

        if (playerInteraction != null)
            playerInteraction.OnInteract();
    }

    public void OnToggleJournal()
    {
        if (noteManager != null)
        {
            noteManager.ToggleJournalExternally();
        }
        else
        {
            Debug.LogWarning("PlayerInputHandler: NoteManager не назначен или не найден. Не могу переключить журнал.");
        }
    }


    public void OnCloseActiveUI()
    {
        if (PuzzleHintManager.Instance != null && PuzzleHintManager.Instance.IsHintActive())
        {
            PuzzleHintManager.Instance.CloseHint();
            Debug.Log("PlayerInputHandler: Закрыта панель подсказок головоломки по Escape.");
            return; // Важно: выходим, чтобы не открывать/закрывать другие UI или меню паузы
        }
        if (tutorialController != null && tutorialController.IsTutorialActive())
        {
            tutorialController.CloseTutorial();
            Debug.Log("PlayerInputHandler: Закрыта панель обучения/подсказок по Escape.");
            return; // Завершаем, если закрыли UI
        }
        if (magicSquareManager != null && magicSquareManager.IsGameActive())
        {
            magicSquareManager.CloseGameExternally();
            Debug.Log("PlayerInputHandler: Закрыта головоломка 'Магический Квадрат'.");
        }

        AcousticPuzzleManager acousticManager = FindObjectOfType<AcousticPuzzleManager>();
        if (acousticManager != null && acousticManager.IsPuzzleActive())
        {
            acousticManager.ClosePuzzle();
            Debug.Log("PlayerInputHandler: Закрыта акустическая головоломка по Escape.");
            return;
        }


        if (noteManager != null)
        {
            noteManager.CloseUIExternally();
        }
        else
        {
            Debug.LogWarning("PlayerInputHandler: NoteManager не назначен или не найден. Не могу закрыть UI.");
        }
    }
    public void OnToggleHints(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (tutorialController != null)
            {

                if (!tutorialController.IsTutorialActive())
                {
                    tutorialController.ShowTutorial();
                    Debug.Log("PlayerInputHandler: Показаны подсказки.");
                }
            }
            else
            {
                Debug.LogWarning("PlayerInputHandler: TutorialController не назначен!");
            }
        }
    }


    /// <summary>
    /// Активирует указанный Action Map и деактивирует все остальные.
    /// </summary>
    /// <param name="mapName">Имя Action Map для активации (например, "Player", "UI", "Puzzle").</param>
    public void SwitchActionMap(string mapName)
    {
        if (_playerInput == null)
        {
            Debug.LogError("PlayerInputHandler: _playerInput не инициализирован.");
            return;
        }

        if (_playerInput.currentActionMap.name == mapName) return; // Уже активен, нет необходимости переключать

        _playerInput.SwitchCurrentActionMap(mapName);
        Debug.Log($"PlayerInputHandler: Переключен Action Map на '{mapName}'.");

        // Управление видимостью курсора
        if (mapName == "UI" || mapName == "Puzzle") // Предполагаем, что для UI и головоломок курсор должен быть виден
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else // Для Action Map игрока курсор должен быть заблокирован
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// Проверяет, активен ли в данный момент указанный Action Map.
    /// </summary>
    /// <param name="mapName">Имя Action Map для проверки.</param>
    /// <returns>True, если Action Map активен, иначе False.</returns>
    public bool IsActionMapActive(string mapName)
    {
        if (_playerInput == null) return false;
        return _playerInput.currentActionMap.name == mapName;
    }
}