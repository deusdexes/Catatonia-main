// NoteManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq; 
using TMPro; 
using UnityEngine.UI; 


public class NoteManager : MonoBehaviour
{
    // Singleton-инстанс для легкого доступа из других скриптов
    public static NoteManager Instance { get; private set; }

    [Header("UI Elements")]
    [Tooltip("Основная панель UI, отображающая содержимое записки.")]
    public GameObject notePanel;
    [Tooltip("Текстовый компонент TextMeshProUGUI для отображения содержимого записки.")]
    public TextMeshProUGUI noteText;
    [Tooltip("Панель UI журнала, содержащая список всех собранных записок.")]
    public GameObject journalPanel;
    [Tooltip("Родительский объект внутри ScrollView журнала, куда будут добавляться элементы записей.")]
    public Transform journalContentParent;
    [Tooltip("Префаб UI-элемента для одной записи в журнале (например, кнопка с заголовком).")]
    public GameObject journalEntryPrefab;

    [Header("Player Control References")]
    [Tooltip("Ссылка на скрипт движения игрока (например, PlayerMovement).")]
    public MonoBehaviour playerMovementScript;
    [Tooltip("Отключать ли скрипт движения игрока при открытии UI записки/журнала.")]
    public bool playerMovementRequiresDisable = false;
    [Tooltip("Управлять ли видимостью и блокировкой курсора при открытии UI записки/журнала.")]
    public bool cursorRequiresToggle = true;
    [Header("Audio")]
    [Tooltip("Аудиоклип, который будет воспроизводиться при открытии журнала.")]
    public AudioClip journalOpenSound;

    // Приватные списки и ссылки
    private List<NoteData> collectedNotes = new List<NoteData>();
    private NoteData currentlyDisplayedNote;
    private GameObject notePickupToDeactivate; // Ссылка на GameObject NotePickup, который нужно деактивировать после прочтения
    private AudioSource audioSource;

    [Header("All Available Notes")]
    [Tooltip("Список всех ScriptableObject 'NoteData', которые существуют в игре. Заполните в инспекторе!")]
    public List<NoteData> allGameNotes = new List<NoteData>();

    // Константы для сохранения в PlayerPrefs
    private const string CollectedNotesPlayerPrefsKey = "CollectedNotesIDs";
    private const char NoteIDDelimiter = ','; // Разделитель для ID в строке PlayerPrefs

    private void Awake()
    {
        // Реализация Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Убедимся, что все панели скрыты при старте игры
        if (notePanel != null) notePanel.SetActive(false);
        if (journalPanel != null) journalPanel.SetActive(false);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
    }

    private void Start()
    {
        // Проверка заполнения allGameNotes
        if (allGameNotes == null || allGameNotes.Count == 0)
        {
            Debug.LogWarning("NoteManager: allGameNotes пуст! Убедитесь, что вы заполнили его в инспекторе или через код. Сохранения могут работать некорректно.");
        }

        // Загружаем собранные записки при старте игры
        LoadCollectedNotes();
    }

    // --- Методы Сохранения/Загрузки ---

    // Метод для сохранения ID собранных записок в PlayerPrefs
    private void SaveCollectedNotes()
    {
        if (collectedNotes == null || !collectedNotes.Any())
        {
            PlayerPrefs.DeleteKey(CollectedNotesPlayerPrefsKey); // Удаляем ключ, если нет записок
            PlayerPrefs.Save();
            Debug.Log("NoteManager: Collected notes list is empty. Removed save key.");
            return;
        }

        // Преобразуем список собранных NoteData в список их noteID (строк)
        List<string> collectedIDs = collectedNotes.Select(n => n.noteID).ToList();
        // Объединяем ID в одну строку, разделенную разделителем
        string idsToSave = string.Join(NoteIDDelimiter.ToString(), collectedIDs);

        PlayerPrefs.SetString(CollectedNotesPlayerPrefsKey, idsToSave);
        PlayerPrefs.Save(); // Сохраняем изменения на диск
        Debug.Log("NoteManager: Collected notes saved.");
    }

    // Метод для загрузки собранных записок из PlayerPrefs
    private void LoadCollectedNotes()
    {
        collectedNotes.Clear(); // Очищаем текущий список перед загрузкой
        Debug.Log("NoteManager: Начинаем загрузку записок. Текущий список очищен.");

        if (PlayerPrefs.HasKey(CollectedNotesPlayerPrefsKey))
        {
            string savedIDsString = PlayerPrefs.GetString(CollectedNotesPlayerPrefsKey);
            Debug.Log($"NoteManager: Найдены сохраненные ID: '{savedIDsString}'.");

            // --- Валидация сохраненных данных ---
            if (!IsValidJournalSaveData(savedIDsString))
            {
                Debug.LogWarning($"NoteManager: Обнаружено старое или некорректное сохранение журнала по ключу '{CollectedNotesPlayerPrefsKey}'. Очищаем.");
                ClearCollectedNotesSave(); // Очищаем PlayerPrefs
                Debug.Log("NoteManager: Из-за некорректных данных журнал будет загружен пустым.");
                return; // Прерываем загрузку, так как данные были плохие
            }
            // --- Конец валидации ---

            // Если данные валидны, разбиваем строку на отдельные ID
            string[] idArray = savedIDsString.Split(NoteIDDelimiter);

            foreach (string id in idArray)
            {
                if (!string.IsNullOrEmpty(id)) // Игнорируем пустые строки
                {
                    // Находим соответствующий NoteData из списка всех доступных записок
                    NoteData note = allGameNotes.FirstOrDefault(n => n.noteID == id);
                    if (note != null && !collectedNotes.Contains(note)) // Проверяем на дубликаты
                    {
                        collectedNotes.Add(note);
                        Debug.Log($"NoteManager: Загружена записка '{note.noteTitle}' (ID: {note.noteID}).");
                    }
                    else if (note == null)
                    {
                        Debug.LogWarning($"NoteManager: Записка с ID '{id}' найдена в сохранении, но отсутствует в списке allGameNotes. Возможно, NoteData был удален или переименован.");
                    }
                }
            }
            // Сортируем загруженные записки (например, по ID)
            collectedNotes = collectedNotes.OrderBy(n => n.noteID).ToList();
            Debug.Log($"NoteManager: Загружено {collectedNotes.Count} записок.");
        }
        else
        {
            Debug.Log("NoteManager: Ключ сохранения журнала не найден. Начинаем с пустого журнала.");
        }
    }

    // Метод для валидации сохраненной строки ID записок
    private bool IsValidJournalSaveData(string data)
    {
        if (string.IsNullOrEmpty(data))
        {
            return true; // Пустая строка - валидна (нет сохраненных записок)
        }

        string[] ids = data.Split(NoteIDDelimiter);
        foreach (string id in ids)
        {
            if (string.IsNullOrEmpty(id)) continue;

            if (allGameNotes == null || allGameNotes.Count == 0)
            {
                Debug.LogWarning("NoteManager: allGameNotes пуст при валидации сохранения. Валидация может быть неточной.");
                return false; // Считаем данные невалидными, так как не можем проверить их
            }

            // Проверяем, существует ли этот ID в нашем списке всех доступных записок
            if (!allGameNotes.Any(n => n.noteID == id))
            {
                Debug.LogWarning($"NoteManager: Некорректный ID '{id}' обнаружен в сохраненных данных журнала. Он отсутствует в allGameNotes. Сохранение будет сброшено.");
                return false;
            }
        }
        return true; // Все ID валидны
    }

    // Метод для очистки сохраненных данных журнала из PlayerPrefs
    private void ClearCollectedNotesSave()
    {
        PlayerPrefs.DeleteKey(CollectedNotesPlayerPrefsKey);
        PlayerPrefs.Save();
        Debug.Log("NoteManager: Старое сохранение журнала удалено из PlayerPrefs.");
    }

    // --- Внутренние методы Обработки Ввода (теперь вызываются из Externally методов) ---

    // Обработка ввода для закрытия UI (Escape)
    private void HandleCloseUIInputInternal()
    {
        Debug.Log("NoteManager: Получен запрос на закрытие UI.");
        if (notePanel != null && notePanel.activeSelf)
        {
            CloseNotePanel();
        }
        else if (journalPanel != null && journalPanel.activeSelf)
        {
            CloseJournal();
        }
        else
        {
            Debug.Log("NoteManager: Нет активных UI записок/журнала для закрытия.");
        }
    }

    // Метод для переключения состояния журнала (открыть/закрыть)
    private void ToggleJournalInternal()
    {
        Debug.Log("NoteManager: Получен запрос на переключение журнала.");
        // Не позволяем открыть журнал, если открыта записка
        if (notePanel != null && notePanel.activeSelf)
        {
            Debug.Log("NoteManager: Журнал не может быть открыт, так как панель записки активна.");
            return;
        }

        if (journalPanel != null && journalPanel.activeSelf)
        {
            CloseJournal();
        }
        else
        {
            OpenJournal();
        }
    }

    // --- Public методы для вызова из PlayerInputHandler ---
    // (Эти методы будут вызываться PlayerInputHandler'ом или другими скриптами)
    public void ToggleJournalExternally()
    {
        ToggleJournalInternal();
    }

    public void CloseUIExternally()
    {
        HandleCloseUIInputInternal();
    }

    // --- Методы Управления Панелью Записки ---

    // Метод для отображения записки на UI
    // associatedNotePickup - это GameObject NotePickup, который вызвал отображение
    public void DisplayNote(NoteData note, GameObject associatedNotePickup = null)
    {
        Debug.Log($"NoteManager: DisplayNote вызван для '{note.noteTitle}'.");

        if (notePanel == null || noteText == null)
        {
            Debug.LogError("NoteManager: UI-элементы notePanel или noteText не назначены! Не могу отобразить записку.");
            return;
        }

        // Если журнал открыт, закроем его перед показом записки
        if (journalPanel != null && journalPanel.activeSelf)
        {
            CloseJournal();
        }

        currentlyDisplayedNote = note;
        noteText.text = note.noteContent;
        notePanel.SetActive(true);

        // Приостанавливаем игру, чтобы игрок мог спокойно читать записку
        Time.timeScale = 0f;
        SetPlayerControl(false); // Отключаем управление игроком и показываем курсор

        // Сохраняем ссылку на NotePickup для последующей деактивации
        notePickupToDeactivate = associatedNotePickup;
        Debug.Log($"NoteManager: Записка '{note.noteTitle}' отображена. Связанный NotePickup: {notePickupToDeactivate?.name ?? "нет"}.");

        // Добавляем записку в журнал, если её там ещё нет
        AddNoteToJournal(note);
    }

    // Метод для закрытия панели записки
    public void CloseNotePanel()
    {
        Debug.Log("NoteManager: Закрываем панель записки.");

        if (notePanel != null) notePanel.SetActive(false);
        currentlyDisplayedNote = null;

        // Возобновляем игру
        Time.timeScale = 1f;
        SetPlayerControl(true); // Включаем управление игроком и скрываем курсор

        // Деактивируем NotePickup, если он был связан с открытой запиской
        if (notePickupToDeactivate != null)
        {
            string deactivatedNoteName = notePickupToDeactivate.name;
            notePickupToDeactivate.SetActive(false); // Полностью деактивируем GameObject
            notePickupToDeactivate = null; // Очищаем ссылку
            Debug.Log($"NoteManager: NotePickup '{deactivatedNoteName}' деактивирован после прочтения.");
        }
        else
        {
            Debug.Log("NoteManager: Нет связанного NotePickup для деактивации.");
        }
    }

    // Этот метод можно привязать к кнопке "Закрыть" в UI панели записки
    public void OnCloseNoteButtonClicked()
    {
        CloseNotePanel();
    }

    // --- Методы Управления Журналом ---

    // Метод для добавления записки в список собранных
    public void AddNoteToJournal(NoteData note)
    {
        Debug.Log($"NoteManager: AddNoteToJournal: Попытка добавить записку '{note.noteTitle}' (ID: {note.noteID}). Всего записок до: {collectedNotes.Count}");

        // Проверяем, что такой записки ещё нет в журнале, используя переопределенные Equals/GetHashCode
        if (!collectedNotes.Contains(note))
        {
            collectedNotes.Add(note);
            // Сортируем записки, например, по их noteID для порядка в журнале
            collectedNotes = collectedNotes.OrderBy(n => n.noteID).ToList();
            Debug.Log($"NoteManager: Note '{note.noteTitle}' добавлен в журнал. Всего записок после: {collectedNotes.Count}");
            SaveCollectedNotes(); // Сохраняем данные после добавления новой записки
        }
        else
        {
            Debug.LogWarning($"NoteManager: Note '{note.noteTitle}' (ID: {note.noteID}) уже есть в журнале. Добавление пропущено.");
        }
    }

    // Метод для открытия панели журнала
    public void OpenJournal()
    {
        Debug.Log("NoteManager: Попытка открыть журнал.");
        // Не позволяем открыть журнал, если открыта записка
        if (notePanel != null && notePanel.activeSelf)
        {
            Debug.Log("NoteManager: Журнал не может быть открыт, так как панель записки активна.");
            return;
        }

        if (journalPanel == null || journalContentParent == null || journalEntryPrefab == null)
        {
            Debug.LogError("NoteManager: UI-элементы журнала (journalPanel, journalContentParent или journalEntryPrefab) не назначены! Не могу открыть журнал.");
            return;
        }

        if (journalOpenSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(journalOpenSound);
            Debug.Log("NoteManager: Воспроизведение звука открытия журнала.");
        }

        journalPanel.SetActive(true);
        Debug.Log("NoteManager: Устанавливаем journalPanel.SetActive(true).");
        UpdateJournalUI(); // Обновляем список записей в журнале при его открытии
        Debug.Log("NoteManager: Вызван UpdateJournalUI().");


        // Приостанавливаем игру
        Time.timeScale = 0f;
        SetPlayerControl(false); // Отключаем управление игроком и показываем курсор
        Debug.Log("NoteManager: Журнал открыт. Time.timeScale = 0, управление игроком отключено.");
    }

    // Метод для закрытия панели журнала
    public void CloseJournal()
    {
        Debug.Log("NoteManager: Закрываем журнал.");
        if (journalPanel != null) journalPanel.SetActive(false);

        // Возобновляем игру
        Time.timeScale = 1f;
        SetPlayerControl(true); // Включаем управление игроком и скрываем курсор
        Debug.Log("NoteManager: Журнал закрыт. Time.timeScale = 1, управление игроком включено.");
    }

    // Обновление UI журнала: создание элементов для каждой собранной записки
    private void UpdateJournalUI()
    {
        Debug.Log($"NoteManager: Начало обновления UI журнала. Всего собранных записок: {collectedNotes.Count}");

        if (journalContentParent == null || journalEntryPrefab == null)
        {
            Debug.LogError("NoteManager: journalContentParent или journalEntryPrefab не назначены! Не могу обновить UI журнала.");
            return;
        }

        // Сначала очищаем все старые записи, чтобы не было дубликатов
        int initialChildCount = journalContentParent.childCount;
        for (int i = initialChildCount - 1; i >= 0; i--) // Итерация в обратном порядке для безопасного удаления
        {
            Destroy(journalContentParent.GetChild(i).gameObject);
        }
        Debug.Log($"NoteManager: Старые записи очищены. Было {initialChildCount} дочерних объектов, осталось {journalContentParent.childCount}.");

        // Создаем новый UI-элемент для каждой записки в collectedNotes
        if (collectedNotes.Count == 0)
        {
            Debug.LogWarning("NoteManager: Список собранных записок пуст, UI журнала не будет обновлен новыми элементами.");
        }

        foreach (NoteData note in collectedNotes)
        {
            Debug.Log($"NoteManager: Создаем элемент для записки '{note.noteTitle}' (ID: {note.noteID}).");

            GameObject entry = Instantiate(journalEntryPrefab, journalContentParent);
            entry.name = $"JournalEntry_{note.noteID}"; // Даем осмысленное имя в иерархии
            Debug.Log($"NoteManager: Элемент '{entry.name}' создан под {journalContentParent.name}.");

            // Настраиваем текстовый компонент элемента журнала
            TextMeshProUGUI entryText = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (entryText != null)
            {
                entryText.text = note.noteTitle;
                Debug.Log($"NoteManager: Текст кнопки установлен: '{note.noteTitle}'.");

                // Добавляем слушателя к кнопке элемента журнала
                Button entryButton = entry.GetComponent<Button>();
                if (entryButton != null)
                {
                    entryButton.onClick.RemoveAllListeners(); // Убеждаемся, что нет старых слушателей
                    entryButton.onClick.AddListener(() => DisplayNote(note)); // Вызываем DisplayNote без GameObject
                    Debug.Log($"NoteManager: Слушатель для кнопки '{note.noteTitle}' добавлен.");
                }
                else
                {
                    Debug.LogError($"NoteManager: Префаб JournalEntryPrefab '{journalEntryPrefab.name}' не содержит компонента Button! Не могу добавить слушателя.");
                }
            }
            else
            {
                Debug.LogError($"NoteManager: В префабе JournalEntryPrefab '{journalEntryPrefab.name}' не найден компонент TextMeshProUGUI (для заголовка)! Проверьте префаб и его дочерние объекты.");
            }
        }
        Debug.Log($"NoteManager: Обновление UI журнала завершено. Создано {collectedNotes.Count} элементов.");
    }

    // --- Управление состоянием игрока (движение/курсор) ---
    private void SetPlayerControl(bool enable)
    {
        Debug.Log($"NoteManager: SetPlayerControl вызван с enable = {enable}.");
        // Управление скриптом движения игрока
        if (playerMovementRequiresDisable && playerMovementScript != null)
        {
            playerMovementScript.enabled = enable;
            Debug.Log($"NoteManager: playerMovementScript.enabled установлено в {enable}.");
        }
        else if (playerMovementRequiresDisable && playerMovementScript == null)
        {
            Debug.LogWarning("NoteManager: playerMovementScript не назначен, не могу управлять движением игрока.");
        }

        // Управление видимостью и блокировкой курсора
        if (cursorRequiresToggle)
        {
            Cursor.lockState = enable ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !enable;
            Debug.Log($"NoteManager: Cursor.lockState установлено в {Cursor.lockState}, Cursor.visible в {Cursor.visible}.");
        }
        else
        {
            Debug.Log("NoteManager: Управление курсором отключено (cursorRequiresToggle = false).");
        }
    }
}