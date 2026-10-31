using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Кнопки и информация")]
    public GameObject continueButton; // Ссылка на кнопку "Продолжить" в инспекторе
    public TextMeshProUGUI lastSaveInfoText; // Текст для информации о последнем сохранении

    [Header("Панели меню")]
    public GameObject mainButtonsPanel; // Панель с основными кнопками
    public GameObject optionPanel;      // Панель настроек
    public GameObject creditsPanel;     // Панель с титрами (если есть)

    void Start()
    {
        Cursor.visible = true; // Делаем системный курсор видимым
        Cursor.lockState = CursorLockMode.None; // Разблокируем курсор, чтобы он мог свободно двигаться
        Debug.Log("MainMenuManager: Start() called.");
        // --- 1. Проверка и инициализация SaveLoadManager ---
        // SaveLoadManager должен быть создан заранее и пережить DontDestroyOnLoad
        if (SaveLoadManager.Instance == null)
        {
            Debug.LogError("MainMenuManager: SaveLoadManager.Instance не найден! Убедитесь, что SaveLoadManager существует в сцене и имеет DontDestroyOnLoad.");
            // Если менеджер абсолютно отсутствует, можно попробовать создать его, но это не идеальное решение для Singleton.
            // GameObject saveLoadGO = new GameObject("SaveLoadManager_RuntimeCreated");
            // saveLoadGO.AddComponent<SaveLoadManager>();
            // // После создания Awake() у SaveLoadManager вызовется и установит Instance.
        }

        // --- 1.1 Загрузка настроек при старте меню ---
        if (GameSettingsManager.Instance != null)
        {
            GameSettingsManager.Instance.LoadSettings();
            Debug.Log("MainMenuManager: Настройки загружены.");
        }
        else
        {
            Debug.LogWarning("MainMenuManager: GameSettingsManager.Instance не найден. Настройки не будут загружены.");
        }


        // --- 2. Четкое управление видимостью панелей при старте ---
        if (mainButtonsPanel != null)
        {
            mainButtonsPanel.SetActive(true); // Основная панель активна
            Debug.Log("MainMenuManager: MainButtonsPanel активирована.");
        }
        else
        {
            Debug.LogError("MainMenuManager: Ссылка на 'mainButtonsPanel' не установлена в инспекторе!");
        }

        // Все остальные панели неактивны
        if (optionPanel != null) optionPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);

        // --- 3. Обновление статуса кнопки "Продолжить" ---
        UpdateContinueButtonStatus();
    }

    // Обновляет состояние кнопки "Продолжить" в зависимости от наличия файла сохранения
    void UpdateContinueButtonStatus()
    {
        // Убедимся, что SaveLoadManager.Instance точно доступен
        if (SaveLoadManager.Instance == null)
        {
            Debug.LogError("MainMenuManager: SaveLoadManager.Instance отсутствует при попытке обновить статус кнопки сохранения! Кнопка 'Продолжить' будет неактивна.");
            if (continueButton != null) continueButton.SetActive(false);
            if (lastSaveInfoText != null) lastSaveInfoText.text = "Начать новую игру (ошибка)";
            return;
        }

        string actualSaveFileName = SaveLoadManager.Instance.GetSaveFileName();
        Debug.Log($"MainMenuManager: UpdateContinueButtonStatus() - Запрашиваемое имя файла сохранения: '{actualSaveFileName}'");

        bool saveFileExists = ES3.FileExists(actualSaveFileName);
        Debug.Log($"MainMenuManager: UpdateContinueButtonStatus() - ES3.FileExists('{actualSaveFileName}') вернул: {saveFileExists}");

        if (continueButton != null)
        {
            continueButton.SetActive(true); // Убеждаемся, что кнопка видна
            Button continueBtnComponent = continueButton.GetComponent<Button>();
            if (continueBtnComponent != null)
            {
                continueBtnComponent.interactable = saveFileExists; // Интерактивность зависит от наличия сохранения
            }
            Debug.Log($"MainMenuManager: Кнопка 'Продолжить' {(saveFileExists ? "активна" : "неактивна")} для взаимодействия.");
        }
        else
        {
            Debug.LogWarning("MainMenuManager: Ссылка на 'continueButton' не установлена в инспекторе!");
        }

        if (lastSaveInfoText != null)
        {
            if (saveFileExists)
            {
                try
                {
                    GameData tempGameData = ES3.Load<GameData>(actualSaveFileName);
                    lastSaveInfoText.text = $"Продолжить игру (Сцена: {tempGameData.currentSceneName})";
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"MainMenuManager: Ошибка при загрузке данных сохранения для отображения: {e.Message}");
                    lastSaveInfoText.text = "Продолжить игру (сохранение найдено, но данные не прочитаны)";
                }
            }
            else
            {
                lastSaveInfoText.text = "Начать новую игру (нет сохранения)";
            }
        }
        else
        {
            Debug.LogWarning("MainMenuManager: Ссылка на 'lastSaveInfoText' не установлена в инспекторе!");
        }
    }

    // Начинает новую игру
    public void NewGame()
    {
        Debug.Log("MainMenuManager: Начинаем новую игру.");
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.DeleteSaveFile(); // Удаляем старое сохранение, если есть
            SaveLoadManager.Instance.currentGameData = new GameData(); // Сбрасываем данные игры на новые
        }
        else
        {
            Debug.LogWarning("MainMenuManager: SaveLoadManager.Instance не найден. Невозможно удалить файл сохранения или сбросить данные.");
        }

        // Вместо прямой загрузки сцены, устанавливаем сцену для загрузки через LoadingScreenManager
        LoadingScreenManager.sceneToLoad = "1Level"; // Имя первой сцены игры
        SceneManager.LoadScene("LoadingScreen");     // Загружаем сцену загрузки
    }

    // Продолжает игру из последнего сохранения
    public void ContinueGame()
    {
        Debug.Log("MainMenuManager: Попытка продолжить игру.");
        if (SaveLoadManager.Instance != null)
        {
            string saveFileName = SaveLoadManager.Instance.GetSaveFileName();
            if (ES3.FileExists(saveFileName))
            {
                // Загружаем данные из сохранения, чтобы узнать, какую сцену загружать
                try
                {
                    GameData tempGameData = ES3.Load<GameData>(saveFileName);
                    LoadingScreenManager.sceneToLoad = tempGameData.currentSceneName; // Имя сцены из сохранения
                    SceneManager.LoadScene("LoadingScreen"); // Загружаем сцену загрузки
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"MainMenuManager: Ошибка при загрузке данных сохранения для продолжения: {e.Message}");
                    // В случае ошибки, можно начать новую игру или вывести сообщение
                    NewGame();
                }
            }
            else
            {
                Debug.LogWarning("MainMenuManager: Кнопка 'Продолжить' была нажата, но файл сохранения не найден. Начинаем новую игру.");
                NewGame();
            }
        }
        else
        {
            Debug.LogError("MainMenuManager: SaveLoadManager.Instance не найден. Невозможно загрузить игру. Начинаем новую игру.");
            NewGame();
        }
    }

    // Выход из игры
    public void ExitGame()
    {
        Debug.Log("MainMenuManager: Выход из игры.");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ----- Методы для управления панелями меню -----
    public void ShowOptionsPanel()
    {
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        if (optionPanel != null) optionPanel.SetActive(true);
        if (creditsPanel != null) creditsPanel.SetActive(false);

        // Опционально: Если OptionsController управляет UI панели настроек
        // и он находится на optionPanel, можно вызвать метод для обновления UI
        OptionsController optionsController = optionPanel.GetComponent<OptionsController>();
        if (optionsController != null && GameSettingsManager.Instance != null)
        {
            optionsController.OpenOptionsUI(
                GameSettingsManager.Instance.MouseSensitivity,
                GameSettingsManager.Instance.MasterVolume,
                GameSettingsManager.Instance.SFXVolume,
                GameSettingsManager.Instance.MusicVolume
            );
        }
        else if (optionsController == null)
        {
            Debug.LogWarning("MainMenuManager: OptionsController не найден на optionPanel.");
        }
        else if (GameSettingsManager.Instance == null)
        {
            Debug.LogError("MainMenuManager: GameSettingsManager.Instance не найден при открытии панели настроек!");
        }

        Debug.Log("MainMenuManager: Открыта панель настроек.");
    }

    public void HideOptionsPanel()
    {
        if (optionPanel != null) optionPanel.SetActive(false);
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
        if (creditsPanel != null) creditsPanel.SetActive(false);

        // Опционально: Если OptionsController должен сохранить настройки при закрытии
        OptionsController optionsController = optionPanel.GetComponent<OptionsController>();
        if (optionsController != null)
        {
            optionsController.CloseOptionsUI(); // Вызываем метод для сохранения настроек
        }

        Debug.Log("MainMenuManager: Закрыта панель настроек.");
    }

    public void ShowCreditsPanel()
    {
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        if (optionPanel != null) optionPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);
        Debug.Log("MainMenuManager: Открыта панель титров.");
    }

    public void HideCreditsPanel()
    {
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
        if (optionPanel != null) optionPanel.SetActive(false);
        Debug.Log("MainMenuManager: Закрыта панель титров.");
    }

    // ----- Методы для применения настроек (если GameSettingsManager не используется напрямую) -----
    // Эти методы должны взаимодействовать с GameSettingsManager, если он существует.
    // Обратите внимание, что при использовании OptionsController эти методы могут быть не нужны напрямую
    // и настройки будут обновляться через него. Я оставил их для полноты, но при вашей текущей
    // структуре они могут быть избыточными или вызываться из OptionsController.
    public void SetMasterVolume(float volume)
    {
        if (GameSettingsManager.Instance != null) GameSettingsManager.Instance.SetMasterVolume(volume);
    }
    public void SetSfxVolume(float volume)
    {
        if (GameSettingsManager.Instance != null) GameSettingsManager.Instance.SetSFXVolume(volume);
    }
    public void SetMusicVolume(float volume)
    {
        if (GameSettingsManager.Instance != null) GameSettingsManager.Instance.SetMusicVolume(volume);
    }
    public void SetMouseSensitivity(float sensitivity)
    {
        if (GameSettingsManager.Instance != null) GameSettingsManager.Instance.SetMouseSensitivity(sensitivity);
    }
}