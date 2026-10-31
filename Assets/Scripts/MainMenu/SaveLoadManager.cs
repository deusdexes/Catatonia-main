using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using static ES3; // !!! ВАЖНО: используйте 'using static' для ES3 !!!

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    public GameData currentGameData;
    private string saveFileName = "playerSave.es3"; // Расширение .es3 для Easy Save

    private bool isLoadingGame = false; // Флаг для контроля процесса загрузки

    void Awake()
    {
        Debug.Log("SaveLoadManager: Awake() called.");
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("SaveLoadManager: Destroying duplicate instance on '" + gameObject.name + "'.");
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            Debug.Log("SaveLoadManager: Instance set to '" + gameObject.name + "'.");
            DontDestroyOnLoad(gameObject);
            Debug.Log("SaveLoadManager: DontDestroyOnLoad called for: " + gameObject.name);

            // Инициализируем currentGameData, если его нет
            if (currentGameData == null)
            {
                currentGameData = new GameData();
                Debug.Log("SaveLoadManager: currentGameData инициализирована новой GameData.");
            }
        }
    }

    // Этот метод вызывается, когда объект становится активным (например, при загрузке сцены)
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Этот метод вызывается, когда объект становится неактивным или уничтожается
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void SaveGame()
    {
        UpdateGameDataFromCurrentState(); // Сначала обновим данные из текущего состояния игры
        ES3.Save(saveFileName, currentGameData); // Используем ES3 для сохранения
        Debug.Log("SaveLoadManager: Игра успешно сохранена в " + saveFileName);
    }

    // Новый публичный метод для инициации загрузки игры
    public void StartLoadGameSequence()
    {
        if (isLoadingGame)
        {
            Debug.LogWarning("SaveLoadManager: StartLoadGameSequence() - Попытка запустить загрузку, когда загрузка уже идет. Игнорируем.");
            return; // Предотвращаем повторный вызов, если загрузка уже началась
        }

        isLoadingGame = true;
        Debug.Log("SaveLoadManager: StartLoadGameSequence() - Инициирован процесс загрузки игры.");

        // Проверка существования файла сохранения
        bool fileExists = ES3.FileExists(saveFileName);
        Debug.Log($"SaveLoadManager: Проверяем наличие файла сохранения '{saveFileName}'. Существует: {fileExists}");

        if (fileExists)
        {
            try
            {
                currentGameData = ES3.Load<GameData>(saveFileName); // Загружаем данные из файла
                Debug.Log($"SaveLoadManager: Данные игры успешно загружены из '{saveFileName}'. Загруженная сцена: '{currentGameData.currentSceneName}'. Позиция игрока: {currentGameData.playerPosition}.");

                string activeSceneName = SceneManager.GetActiveScene().name;
                string savedSceneName = currentGameData.currentSceneName;

                Debug.Log($"SaveLoadManager: Активная сцена: '{activeSceneName}'. Сохраненная сцена: '{savedSceneName}'.");

                // Если сохраненная сцена отличается от текущей, загружаем сохраненную сцену
                if (activeSceneName != savedSceneName && !string.IsNullOrEmpty(savedSceneName))
                {
                    Debug.Log($"SaveLoadManager: Активная сцена ('{activeSceneName}') отличается от сохраненной ('{savedSceneName}'). Загружаем сохраненную сцену.");
                    SceneManager.LoadScene(savedSceneName);
                    // Применение игровых данных произойдет в OnSceneLoaded после загрузки сцены
                }
                else if (activeSceneName == savedSceneName)
                {
                    // Если мы уже находимся на сохраненной сцене (например, из главного меню), применяем данные немедленно
                    Debug.Log($"SaveLoadManager: Уже на сохраненной сцене ('{savedSceneName}'). Применяем игровые данные.");
                    ApplyGameDataToCurrentState();
                    isLoadingGame = false; // Загрузка завершена
                }
                else
                {
                    Debug.LogWarning("SaveLoadManager: currentSceneName в данных сохранения пуст или невалиден. Невозможно загрузить сцену. Применяем только общие данные.");
                    ApplyGameDataToCurrentState(); // Применяем то, что можно
                    isLoadingGame = false; // Загрузка завершена
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"SaveLoadManager: Ошибка при загрузке данных сохранения: {e.Message}");
                isLoadingGame = false; // Загрузка завершена с ошибкой
            }
        }
        else
        {
            Debug.LogWarning("SaveLoadManager: Файл сохранения не найден при попытке загрузки. Нечего загружать.");
            isLoadingGame = false; // Загрузка завершена, так как файла нет
        }
    }


    public void DeleteSaveFile()
    {
        if (ES3.FileExists(saveFileName)) // Проверяем существование файла с помощью ES3
        {
            ES3.DeleteFile(saveFileName); // Удаляем файл
            Debug.Log("SaveLoadManager: Файл сохранения '" + saveFileName + "' удален.");
            currentGameData = new GameData(); // Сбрасываем текущие данные на новые
        }
        else
        {
            Debug.Log("SaveLoadManager: Файл сохранения '" + saveFileName + "' не найден для удаления.");
        }
    }

    // Вспомогательный метод для получения имени файла сохранения
    public string GetSaveFileName()
    {
        return saveFileName;
    }

    // Обновляет currentGameData данными из текущего состояния игры
    private void UpdateGameDataFromCurrentState()
    {
        currentGameData.currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"SaveLoadManager: Сохраняем имя активной сцены: '{currentGameData.currentSceneName}'");

        // --- Сохраняем настройки игры через GameSettingsManager (если он есть) ---
        if (GameSettingsManager.Instance != null && currentGameData != null)
        {
            // Используем новые публичные свойства
            currentGameData.gameSettings.masterVolume = GameSettingsManager.Instance.MasterVolume;
            currentGameData.gameSettings.sfxVolume = GameSettingsManager.Instance.SFXVolume;
            currentGameData.gameSettings.musicVolume = GameSettingsManager.Instance.MusicVolume;
            currentGameData.gameSettings.mouseSensitivity = GameSettingsManager.Instance.MouseSensitivity;

            Debug.Log("SaveLoadManager: Обновлены настройки в currentGameData из GameSettingsManager.");
        }
        else
        {
            Debug.LogWarning("SaveLoadManager: Не удалось обновить настройки в currentGameData, т.к. GameSettingsManager.Instance или currentGameData равно NULL.");
        }

        // --- Получаем и сохраняем позицию игрока ---
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            currentGameData.playerPosition = player.transform.position;
            // Здесь могут быть другие данные игрока (здоровье, инвентарь и т.д.)
            // Например:
            // PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            // if (playerHealth != null) currentGameData.playerHealth = playerHealth.CurrentHealth;
            Debug.Log($"SaveLoadManager: Сохранена позиция игрока: {currentGameData.playerPosition}");
        }
        else
        {
            Debug.LogWarning("SaveLoadManager: GameObject с тегом 'Player' не найден для сохранения его позиции. Убедитесь, что у вашего игрока есть тег 'Player'.");
        }

        // Добавьте здесь сохранение других игровых данных (здоровье, инвентарь и т.д.)
    }

    // Применяет все загруженные данные к текущему состоянию игры
    private void ApplyGameDataToCurrentState()
    {
        Debug.Log("SaveLoadManager: ApplyGameDataToCurrentState() - Применяем все загруженные игровые данные.");

        // --- Применяем настройки игры через GameSettingsManager ---
        if (GameSettingsManager.Instance != null && currentGameData.gameSettings != null)
        {
            GameSettingsManager.Instance.SetMasterVolume(currentGameData.gameSettings.masterVolume);
            GameSettingsManager.Instance.SetSFXVolume(currentGameData.gameSettings.sfxVolume);
            GameSettingsManager.Instance.SetMusicVolume(currentGameData.gameSettings.musicVolume);
            GameSettingsManager.Instance.SetMouseSensitivity(currentGameData.gameSettings.mouseSensitivity);
            Debug.Log("SaveLoadManager: Настройки игры применены.");
        }
        else
        {
            Debug.LogWarning("SaveLoadManager: GameSettingsManager.Instance или currentGameData.gameSettings отсутствуют. Настройки игры не могут быть применены.");
        }

        // --- Применяем данные, специфичные для игрока (позиция, здоровье и т.д.) ---
        ApplyPlayerSpecificData();
    }

    // Обработчик события загрузки сцены
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Применяем данные только если мы в процессе загрузки игры (isLoadingGame = true)
        // И если загруженная сцена соответствует сохраненной
        if (isLoadingGame && scene.name == currentGameData.currentSceneName)
        {
            Debug.Log($"SaveLoadManager: Сцена '{scene.name}' успешно загружена по данным сохранения. Применяем игровые данные.");
            ApplyGameDataToCurrentState(); // Применяем все данные после загрузки сцены
            isLoadingGame = false; // Завершаем процесс загрузки
        }
        else if (isLoadingGame && scene.name != currentGameData.currentSceneName)
        {
            Debug.LogWarning($"SaveLoadManager: Сцена '{scene.name}' загружена, но она не соответствует сохраненной сцене '{currentGameData.currentSceneName}'. Игровые данные НЕ БУДУТ применены, возможно, произошла ошибка или несоответствие.");
            isLoadingGame = false; // Завершаем процесс загрузки, так как сцена не совпала
        }
        // Если isLoadingGame = false, значит это обычная загрузка сцены, не связанная с продолжением игры, и мы не применяем данные.
    }

    // Применяет данные, специфичные для игрока
    private void ApplyPlayerSpecificData()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = currentGameData.playerPosition;
            // Здесь могут быть другие данные игрока (здоровье, инвентарь и т.д.)
            // Пример:
            // PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            // if (playerHealth != null) playerHealth.CurrentHealth = currentGameData.playerHealth;
            Debug.Log($"SaveLoadManager: Применена позиция игрока: {currentGameData.playerPosition}");
        }
        else
        {
            Debug.LogWarning("SaveLoadManager: GameObject с тегом 'Player' не найден на сцене для применения данных. Убедитесь, что у вашего игрока есть тег 'Player' и он присутствует на сцене.");
        }
    }
}