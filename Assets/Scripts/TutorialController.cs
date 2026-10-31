using UnityEngine;
using UnityEngine.InputSystem; // Для использования InputSystem, если требуется (хотя здесь он больше для общего контекста)

public class TutorialController : MonoBehaviour
{
    [Tooltip("Ссылка на UI-панель с подсказками (с крестиком для закрытия)")]
    public GameObject tutorialPanel;

    private bool isTutorialActive = false;

    // ВАЖНО: Предполагается, что в вашей игре в активном режиме курсор скрыт и заблокирован
    private CursorLockMode _previousLockMode;
    private bool _previousCursorVisibility;

    void Start()
    {
        // Убедимся, что панель назначена
        if (tutorialPanel == null)
        {
            Debug.LogError("TutorialController: Панель обучения не назначена в инспекторе!", this);
            return;
        }

        // Запоминаем текущее состояние курсора (на случай, если мы не в главном меню)
        _previousLockMode = Cursor.lockState;
        _previousCursorVisibility = Cursor.visible;

        // Показываем обучение в начале сессии
        ShowTutorial();
    }

    /// <summary>
    /// Показывает панель обучения и ставит игру на паузу.
    /// Этот метод вызывается либо из Start(), либо из PlayerInputHandler.
    /// </summary>
    public void ShowTutorial()
    {
        if (isTutorialActive) return;

        isTutorialActive = true;

        // 1. Пауза сцены
        SetGamePause(true);

        // 2. Показ UI
        tutorialPanel.SetActive(true);

        // 3. Управление курсором (включаем курсор для UI)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;


    }

    /// <summary>
    /// Закрывает панель обучения и снимает игру с паузы.
    /// Этот метод должен вызываться с помощью Unity Event с кнопки "крестик" на UI.
    /// </summary>
    public void CloseTutorial()
    {
        if (!isTutorialActive) return;

        isTutorialActive = false;

        // 1. Снимаем паузу
        SetGamePause(false);

        // 2. Скрываем UI
        tutorialPanel.SetActive(false);

        // 3. Восстанавливаем состояние курсора (предполагаем, что это будет LockMode.Locked)
        // Мы используем запомненные ранее значения на случай, если игра начиналась с меню и т.п.
        Cursor.lockState = _previousLockMode;
        Cursor.visible = _previousCursorVisibility;
    }

    /// <summary>
    /// Устанавливает или снимает паузу, управляя Time.timeScale.
    /// </summary>
    /// <param name="isPaused">Если true, ставит игру на паузу (Time.timeScale = 0f).</param>
    private void SetGamePause(bool isPaused)
    {
        // 0f - пауза, 1f - нормальное время
        Time.timeScale = isPaused ? 0f : 1f;

        // Оповещение других систем о паузе (опционально, но полезно)
        // Пример: EventManager.Trigger("OnGamePause", isPaused);
        Debug.Log($"Состояние игры изменено. Пауза: {isPaused}. Time.timeScale: {Time.timeScale}");
    }

    // Метод для внешнего использования: позволяет проверить, активно ли сейчас обучение.
    public bool IsTutorialActive()
    {
        return isTutorialActive;
    }
}