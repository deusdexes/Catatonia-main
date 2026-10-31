using UnityEngine;
using UnityEngine.UI; // Для работы с компонентом Image
using System.Collections.Generic;
using System.Linq;

// Структура для удобного назначения ID и картинки в Инспекторе
[System.Serializable]
public class HintData
{
    [Tooltip("Уникальный ID для этой головоломки/подсказки.")]
    public int id;
    [Tooltip("Изображение (Sprite), которое будет показано.")]
    public Sprite hintImage;
}

public class PuzzleHintManager : MonoBehaviour
{
    // Singleton (Одиночка) для легкого доступа из других скриптов
    public static PuzzleHintManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Панель/Контейнер, который нужно активировать для отображения подсказки.")]
    public GameObject hintUIContainer;
    [Tooltip("Компонент Image на этой панели, в который будет загружена картинка.")]
    public Image hintImageComponent;

    [Header("Hint Configuration")]
    [Tooltip("Список всех подсказок: ID + Изображение.")]
    public HintData[] hintDatas;

    // Хранилище для отслеживания уже показанных ID (сохраняется в сессии)
    private HashSet<int> shownHints = new HashSet<int>();

    // Ключ для PlayerPrefs для сохранения состояния между сессиями
    private const string ShownHintsKey = "PuzzledShownHints";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Optionally keep this object across scenes if needed, but for a single level game, it's not strictly necessary.
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (hintUIContainer != null)
        {
            hintUIContainer.SetActive(false);
        }

        LoadShownHints(); // Загружаем состояние при старте
    }

    /// <summary>
    /// Пытается показать подсказку с заданным ID.
    /// </summary>
    /// <param name="hintID">Уникальный ID подсказки.</param>
    public void ShowHint(int hintID)
    {
        // 1. Проверка на единоразовость
        if (shownHints.Contains(hintID))
        {
            Debug.Log($"PuzzleHintManager: Подсказка с ID {hintID} уже была показана.");
            return;
        }

        // 2. Поиск картинки
        HintData data = hintDatas.FirstOrDefault(d => d.id == hintID);

        if (data == null)
        {
            Debug.LogError($"PuzzleHintManager: Не найдено изображение для ID {hintID} в конфигурации!");
            return;
        }

        // 3. Отображение UI и пауза
        if (hintImageComponent != null)
        {
            hintImageComponent.sprite = data.hintImage;
            hintImageComponent.SetNativeSize(); // Для корректного отображения размера
        }

        if (hintUIContainer != null)
        {
            hintUIContainer.SetActive(true);
        }

        // Пауза игры
        Time.timeScale = 0f;
        Debug.Log($"PuzzleHintManager: Игра поставлена на паузу, показана подсказка ID {hintID}.");

        // 4. Запись состояния
        shownHints.Add(hintID);
        SaveShownHints();
    }

    /// <summary>
    /// Закрывает UI подсказки и снимает игру с паузы.
    /// Вызывается из PlayerInputHandler по нажатию Esc.
    /// </summary>
    public void CloseHint()
    {
        if (hintUIContainer != null && hintUIContainer.activeSelf)
        {
            hintUIContainer.SetActive(false);

            // Снятие игры с паузы
            // Важно: Проверяйте, что Time.timeScale не установлен в 0 другими системами паузы.
            // В рамках данного примера просто ставим 1.
            Time.timeScale = 1f;
            Debug.Log("PuzzleHintManager: Подсказка закрыта, игра снята с паузы.");
        }
    }

    /// <summary>
    /// Проверяет, активна ли панель подсказок.
    /// </summary>
    public bool IsHintActive()
    {
        return hintUIContainer != null && hintUIContainer.activeSelf;
    }

    // --- Методы для сохранения состояния (PlayerPrefs) ---

    private void SaveShownHints()
    {
        // Преобразуем HashSet в строку для сохранения (например, "1,5,10")
        string ids = string.Join(",", shownHints.Select(id => id.ToString()).ToArray());
        PlayerPrefs.SetString(ShownHintsKey, ids);
        PlayerPrefs.Save();
    }

    private void LoadShownHints()
    {
        if (PlayerPrefs.HasKey(ShownHintsKey))
        {
            string idsString = PlayerPrefs.GetString(ShownHintsKey);
            if (!string.IsNullOrEmpty(idsString))
            {
                string[] idArray = idsString.Split(',');
                foreach (string idStr in idArray)
                {
                    if (int.TryParse(idStr, out int id))
                    {
                        shownHints.Add(id);
                    }
                }
            }
            Debug.Log($"PuzzleHintManager: Загружено показанных подсказок: {shownHints.Count}");
        }
    }

    // Метод для отладки, если нужно сбросить все показанные подсказки
    public void ClearAllHintsState()
    {
        PlayerPrefs.DeleteKey(ShownHintsKey);
        shownHints.Clear();
        Debug.Log("PuzzleHintManager: Состояние всех показанных подсказок сброшено.");
    }
}