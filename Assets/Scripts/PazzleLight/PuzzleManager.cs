using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Для использования LINQ-запросов (например, ToDictionary)

/// <summary>
/// Управляет логикой головоломки с рычагами и лампочками.
/// Отслеживает состояния, их взаимосвязи и проверяет завершение головоломки.
/// </summary>
public class PuzzleManager : MonoBehaviour
{
    [Tooltip("Массив всех лампочек, участвующих в головоломке.")]
    public Lamp[] allLamps;

    [Tooltip("Массив всех рычагов, участвующих в головоломке.")]
    public Lever[] allLevers;

    [Header("Puzzle Configuration")]
    [Tooltip("Карта, определяющая, какие лампочки переключает каждый рычаг. " +
             "Ключ: LeverID (string), Значение: Массив LampID (int[]).")]
    public LeverEffectConfiguration[] leverEffectConfigs; // Используем новую структуру для настройки в инспекторе

    [Tooltip("Звуковой клип, воспроизводимый при успешном решении головоломки.")]
    public AudioClip puzzleSolvedSound;

    private AudioSource audioSource;

    public bool IsPuzzleSolved { get; private set; } = false; // Добавлено: публичное свойство для статуса головоломки

    // Словари для быстрого доступа к объектам по их ID
    private Dictionary<int, Lamp> lampsByID = new Dictionary<int, Lamp>();
    private Dictionary<string, Lever> leversByID = new Dictionary<string, Lever>();
    private Dictionary<string, int[]> leverEffects = new Dictionary<string, int[]>();

    /// <summary>
    /// Структура для удобной настройки эффектов рычагов в инспекторе.
    /// </summary>
    [System.Serializable]
    public struct LeverEffectConfiguration
    {
        public string leverID;
        public int[] affectedLampIDs;
    }

    /// <summary>
    /// Инициализация менеджера головоломки: заполнение словарей,
    /// подписка на события рычагов и установка начальных состояний.
    /// </summary>
    void Awake()
    {
        Debug.Log("PuzzleManager: Начало инициализации PuzzleManager.");

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            Debug.Log("PuzzleManager: AudioSource добавлен к PuzzleManager.");
        }

        // Заполнение словарей для быстрого доступа к лампочкам
        if (allLamps != null && allLamps.Length > 0)
        {
            foreach (Lamp lamp in allLamps)
            {
                if (lamp == null)
                {
                    Debug.LogWarning("PuzzleManager: В массиве allLamps обнаружен NULL-элемент.");
                    continue;
                }
                if (!lampsByID.ContainsKey(lamp.lampID))
                {
                    lampsByID.Add(lamp.lampID, lamp);
                    Debug.Log($"PuzzleManager: Лампочка с ID {lamp.lampID} добавлена в словарь.");
                }
                else
                {
                    Debug.LogWarning($"PuzzleManager: Обнаружена дублирующаяся лампочка с ID: {lamp.lampID}. Проверьте массив allLamps.");
                }
            }
        }
        else
        {
            Debug.LogError("PuzzleManager: Массив allLamps не назначен или пуст! Лампочки не будут работать.");
        }

        // Заполнение словарей для быстрого доступа к рычагам и подписка на их события
        if (allLevers != null && allLevers.Length > 0)
        {
            foreach (Lever lever in allLevers)
            {
                if (lever == null)
                {
                    Debug.LogWarning("PuzzleManager: В массиве allLevers обнаружен NULL-элемент.");
                    continue;
                }
                if (!leversByID.ContainsKey(lever.leverID))
                {
                    leversByID.Add(lever.leverID, lever);
                    lever.OnLeverToggled.AddListener(OnLeverToggled); // Подписываемся на событие переключения каждого рычага
                    Debug.Log($"PuzzleManager: Рычаг с ID {lever.leverID} добавлен в словарь и подписан на событие.");
                }
                else
                {
                    Debug.LogWarning($"PuzzleManager: Обнаружен дублирующийся рычаг с ID: {lever.leverID}. Проверьте массив allLevers.");
                }
            }
        }
        else
        {
            Debug.LogError("PuzzleManager: Массив allLevers не назначен или пуст! Рычаги не будут работать.");
        }

        // Заполнение карты эффектов рычагов из инспектора
        if (leverEffectConfigs != null && leverEffectConfigs.Length > 0)
        {
            foreach (var config in leverEffectConfigs)
            {
                if (string.IsNullOrEmpty(config.leverID))
                {
                    Debug.LogWarning("PuzzleManager: Обнаружена LeverEffectConfiguration с пустым leverID.");
                    continue;
                }
                if (!leverEffects.ContainsKey(config.leverID))
                {
                    leverEffects.Add(config.leverID, config.affectedLampIDs);
                    string affectedLamps = config.affectedLampIDs != null ? string.Join(", ", config.affectedLampIDs) : "нет";
                    Debug.Log($"PuzzleManager: Конфигурация для рычага '{config.leverID}' добавлена. Влияет на лампы: [{affectedLamps}].");
                }
                else
                {
                    Debug.LogWarning($"PuzzleManager: Дублирующаяся конфигурация для рычага ID: {config.leverID}. Будет использована первая.");
                }
            }
        }
        else
        {
            Debug.LogError("PuzzleManager: leverEffectConfigs не настроен или пуст! Эффекты рычагов не будут применяться.");
        }

        InitializeLampStates(); // Устанавливаем начальное состояние лампочек
        Debug.Log("PuzzleManager: Инициализация PuzzleManager завершена.");
    }

    /// <summary>
    /// Устанавливает начальные состояния всех лампочек в соответствии с
    /// начальными положениями рычагов.
    /// </summary>
    void InitializeLampStates()
    {
        Debug.Log("PuzzleManager: Инициализация начальных состояний ламп.");

        // Сначала выключаем все лампочки
        foreach (Lamp lamp in allLamps)
        {
            if (lamp != null)
            {
                lamp.SetState(false);
                Debug.Log($"PuzzleManager: Лампочка {lamp.lampID} изначально выключена.");
            }
        }

        // Применяем эффекты рычагов, которые изначально находятся в положении "Up"
        foreach (Lever lever in allLevers)
        {
            if (lever != null)
            {
                if (lever.currentPosition == Lever.LeverPosition.Up)
                {
                    ApplyLeverEffect(lever.leverID);
                    Debug.Log($"PuzzleManager: Применен начальный эффект от рычага {lever.leverID} (был Up).");
                }
                else
                {
                    Debug.Log($"PuzzleManager: Рычаг {lever.leverID} изначально в положении Down, начальный эффект не применяется.");
                }
            }
        }

        CheckPuzzleCompletion(); // Проверяем, не решена ли головоломка случайно при старте
        Debug.Log("PuzzleManager: Инициализация начальных состояний ламп завершена.");
    }

    /// <summary>
    /// Обработчик события, когда любой рычаг был переключен.
    /// </summary>
    /// <param name="leverID">ID переключенного рычага.</param>
    void OnLeverToggled(string leverID)
    {
        Debug.Log($"PuzzleManager: *** Событие OnLeverToggled получено от рычага: {leverID} ***");
        ApplyLeverEffect(leverID); // Применяем эффект рычага, который был переключен
        CheckPuzzleCompletion();   // Проверяем, решена ли головоломка после переключения
    }

    /// <summary>
    /// Применяет эффект от переключения рычага к соответствующим лампочкам.
    /// </summary>
    /// <param name="leverID">ID рычага, эффект которого нужно применить.</param>
    void ApplyLeverEffect(string leverID)
    {
        Debug.Log($"PuzzleManager: Применяем эффект для рычага: {leverID}");
        if (leverEffects.ContainsKey(leverID))
        {
            if (leverEffects[leverID] == null || leverEffects[leverID].Length == 0)
            {
                Debug.LogWarning($"PuzzleManager: Для рычага '{leverID}' настроены эффекты, но список affectedLampIDs пуст.");
                return;
            }

            foreach (int lampID in leverEffects[leverID])
            {
                Debug.Log($"PuzzleManager: Попытка переключить лампочку с ID: {lampID} для рычага {leverID}.");
                if (lampsByID.ContainsKey(lampID))
                {
                    lampsByID[lampID].Toggle(); // Переключаем состояние лампочки
                    Debug.Log($"PuzzleManager: Лампочка {lampID} успешно переключена методом Toggle(). Текущее состояние: {lampsByID[lampID].IsOn}");
                }
                else
                {
                    Debug.LogWarning($"PuzzleManager: Не найдена лампочка с ID {lampID} в словаре lampsByID для рычага {leverID}. Проверьте LeverEffectConfiguration и allLamps.");
                }
            }
        }
        else
        {
            Debug.LogWarning($"PuzzleManager: Для рычага '{leverID}' не определены эффекты в leverEffectConfigs. Убедитесь, что ID рычага корректен и настроен в инспекторе.");
        }
    }

    /// <summary>
    /// Проверяет, все ли лампочки включены, что означает решение головоломки.
    /// </summary>
    void CheckPuzzleCompletion()
    {
        Debug.Log("PuzzleManager: Проверка завершения головоломки...");
        bool allOn = true;
        if (allLamps == null || allLamps.Length == 0)
        {
            Debug.LogWarning("PuzzleManager: Нет лампочек для проверки завершения головоломки.");
            return;
        }

        foreach (Lamp lamp in allLamps)
        {
            if (lamp == null)
            {
                Debug.LogWarning("PuzzleManager: В массиве allLamps обнаружен NULL-элемент при проверке завершения.");
                allOn = false; // Считаем, что не решена, если есть null-лампы
                break;
            }
            if (!lamp.IsOn)
            {
                allOn = false;
                Debug.Log($"PuzzleManager: Лампочка {lamp.lampID} выключена. Головоломка не решена.");
                break;
            }
            else
            {
                Debug.Log($"PuzzleManager: Лампочка {lamp.lampID} включена.");
            }
        }

        if (allOn)
        {
            Debug.Log("PuzzleManager: Головоломка решена! Все лампочки включены.");
            IsPuzzleSolved = true; // Устанавливаем в true, когда головоломка решена
            PlayPuzzleSolvedSound(); // Воспроизводим звук победы

            // Здесь можно добавить дополнительную логику завершения:
            // - Открытие двери
            // - Активация следующей части игры
            // - Показ сообщения игроку
            // - Отключение этого менеджера, чтобы предотвратить дальнейшие действия
            enabled = false;
            Debug.Log("PuzzleManager: Компонент PuzzleManager деактивирован.");
        }
        else
        {
            IsPuzzleSolved = false; // Если головоломка не решена (или перестала быть решена), устанавливаем в false
            Debug.Log("PuzzleManager: Головоломка пока не решена.");
        }
    }

    /// <summary>
    /// Воспроизводит звук решения головоломки, если он назначен.
    /// </summary>
    private void PlayPuzzleSolvedSound()
    {
        if (puzzleSolvedSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(puzzleSolvedSound);
            Debug.Log("PuzzleManager: Воспроизведен звук решения головоломки.");
        }
        else
        {
            Debug.LogWarning("PuzzleManager: Звук решения головоломки или AudioSource не назначены.");
        }
    }
}