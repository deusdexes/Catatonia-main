// Файл: PuzzleDependentDoor.cs (ФИНАЛЬНАЯ ВЕРСИЯ)
using UnityEngine;

public class PuzzleDependentDoor : SlidingDoor
{
    [Header("Puzzle Dependency")]
    [Tooltip("ID головоломки из PuzzleDatabaseManager, которая открывает эту дверь.")]
    public int requiredPuzzleID;

    // --- НАЧАЛО ВОЗВРАЩЕННОГО КОДА ---
    [Header("Door Frame Models (Каркас)")]
    [Tooltip("Модель РАЗБЛОКИРОВАННОГО каркаса двери (например, с зеленой лампой).")]
    public GameObject unlockedFrameModel;

    [Tooltip("Модель ЗАБЛОКИРОВАННОГО каркаса двери (например, с красной лампой).")]
    public GameObject lockedFrameModel;
    // --- КОНЕЦ ВОЗВРАЩЕННОГО КОДА ---

    [Header("Audio")]
    [Tooltip("Звук, который проигрывается один раз при разблокировке двери.")]
    public AudioClip unlockSound;

    private bool isUnlocked = false;
    public bool IsCurrentlyUnlocked => isUnlocked;


    protected override void Start()
    {
        base.Start();

        // Подписываемся на событие ЕДИНОГО менеджера
        if (AcousticPuzzleManager.Instance != null)
        {
            AcousticPuzzleManager.Instance.OnPuzzleSolved += HandlePuzzleSolved;
        }
        else
        {
            Debug.LogError($"PuzzleDependentDoor '{gameObject.name}': Не найден AcousticPuzzleManager в сцене!", this);
        }

        // --- ИЗМЕНЕНИЕ: Проверяем начальное состояние головоломки ---
        // Это полезно, если вы захотите реализовать систему сохранений,
        // где головоломка может быть уже решена при загрузке.
        // Для этого нужно будет добавить проверку в PuzzleDatabaseManager, но основа заложена.
        isUnlocked = false; // По умолчанию дверь заперта
        UpdateFrameModels(); // Устанавливаем правильную модель каркаса при старте
    }

    private void HandlePuzzleSolved(int solvedPuzzleID)
    {
        // Если дверь уже открыта или это не наша головоломка, ничего не делаем
        if (isUnlocked || solvedPuzzleID != requiredPuzzleID)
        {
            return;
        }

        isUnlocked = true;
        Debug.Log($"Дверь '{gameObject.name}' разблокирована головоломкой ID {solvedPuzzleID}.");

        // --- ИЗМЕНЕНИЕ: Обновляем модели и проигрываем звук ---
        UpdateFrameModels();
        PlaySound(unlockSound);
    }

    // --- НОВЫЙ МЕТОД, КОТОРЫЙ МЫ ВЕРНУЛИ ---
    /// <summary>
    /// Включает/выключает модели каркаса в зависимости от состояния isUnlocked.
    /// </summary>
    private void UpdateFrameModels()
    {
        if (unlockedFrameModel != null)
        {
            unlockedFrameModel.SetActive(isUnlocked);
        }
        if (lockedFrameModel != null)
        {
            lockedFrameModel.SetActive(!isUnlocked);
        }
    }

    // --- Логика триггеров остается прежней ---
    protected override void OnTriggerEnter(Collider other)
    {
        // Дверь реагирует на триггер только если она разблокирована
        if (isUnlocked)
        {
            base.OnTriggerEnter(other);
        }
    }

    protected override void OnTriggerExit(Collider other)
    {
        if (isUnlocked)
        {
            base.OnTriggerExit(other);
        }
    }

    void OnDestroy()
    {
        if (AcousticPuzzleManager.Instance != null)
        {
            AcousticPuzzleManager.Instance.OnPuzzleSolved -= HandlePuzzleSolved;
        }
    }
}