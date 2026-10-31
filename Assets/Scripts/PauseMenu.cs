using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems; // Добавлено для сброса фокуса UI

public class PauseMenu : MonoBehaviour
{
    [Header("Панель меню паузы")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("Кнопки меню паузы")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    [Header("Настройки")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private bool showCursorOnPause = true;

    private bool isPaused = false;
    // private CursorLockMode previousCursorLockState; // Убираем, так как это ненадежно
    // private bool previousCursorVisible;             // Убираем

    public bool IsNoteUIActive()
    {
        if (NoteManager.Instance != null &&
            (NoteManager.Instance.journalPanel.activeSelf || NoteManager.Instance.notePanel.activeSelf))
        {
            return true;
        }
        return false;
    }

    void Start()
    {
        if (pauseMenuPanel == null) Debug.LogError("Pause Menu Panel не назначен!", this);

        InitializeButtons();

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
    }

    void InitializeButtons()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (quitButton != null) quitButton.onClick.AddListener(QuitToMainMenu);
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            // Если открыты записки, пауза не срабатывает (так как Esc должен закрывать записку)
            if (IsNoteUIActive())
            {
                return;
            }

            // Дополнительная проверка: Если открыта головоломка (например, MagicSquare), 
            // то паузу, возможно, тоже не стоит открывать, или нужно обрабатывать иначе.
            // Но пока оставим как есть.

            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        // Удалено сохранение previousCursorLockState, так как оно часто сохраняет ошибочное состояние

        if (showCursorOnPause)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        // Сбрасываем выбор кнопок, чтобы при открытии не было странных хайлайтов
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        Debug.Log("Игра поставлена на паузу");
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        // --- ИСПРАВЛЕНИЕ ---
        // Вместо восстановления старого состояния, мы ЖЕСТКО задаем состояние для геймплея.
        // Мы возвращаемся в игру -> значит курсор должен быть заблокирован.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Сбрасываем фокус с UI элементов, чтобы Input System не думала, что мы все еще в меню
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        bool puzzleActive = (FindObjectOfType<MagicSquareGameManager>()?.IsGameActive() ?? false)
                 || (FindObjectOfType<AcousticPuzzleManager>()?.IsPuzzleActive() ?? false);

        if (puzzleActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        Debug.Log("Игра продолжена (Курсор заблокирован)");
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None; // Для UI перезагрузки/меню нужен курсор
        Cursor.visible = true;

        Scene currentScene = SceneManager.GetActiveScene();

        if (SpawnManager.Instance != null)
        {
            SpawnManager.Instance.nextSpawnPointId = "";
        }
        SceneManager.LoadScene(currentScene.name);
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.SaveGame();
        }

        StartCoroutine(LoadSceneAsync(mainMenuScene));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    public bool IsGamePaused()
    {
        return isPaused;
    }

    void OnDestroy()
    {
        if (resumeButton != null) resumeButton.onClick.RemoveAllListeners();
        if (restartButton != null) restartButton.onClick.RemoveAllListeners();
        if (quitButton != null) quitButton.onClick.RemoveAllListeners();

        if (Time.timeScale == 0f) Time.timeScale = 1f;
    }
}