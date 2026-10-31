using UnityEngine;
using UnityEngine.Audio;
// Убедись, что GameSettings класс определен где-то в твоем проекте.
// Например, в файле GameData.cs или отдельном GameSettings.cs.
// [System.Serializable]
// public class GameSettings
// {
//     public float mouseSensitivity = 1.0f;
//     public float masterVolume = 0.75f;
//     public float sfxVolume = 0.75f;
//     public float musicVolume = 0.75f;
// }

public class GameSettingsManager : MonoBehaviour
{
    [SerializeField] private AudioMixer mainAudioMixer;

    // --- Singleton ---
    public static GameSettingsManager Instance { get; private set; }

    // --- ПУБЛИЧНЫЕ СВОЙСТВА для доступа к текущим значениям настроек ---
    // MainMenuManager и OptionsController будут использовать эти свойства
    // Они возвращают значения из приватных полей.
    public float MouseSensitivity => currentMouseSensitivity;
    public float MasterVolume => currentMasterVolume;
    public float SFXVolume => currentSFXVolume;
    public float MusicVolume => currentMusicVolume;

    // Приватные поля для хранения текущих значений настроек
    // Эти поля обновляются через методы Set... и ApplySettingsFromGameData/LoadSettings
    private float currentMouseSensitivity = 1.0f; // Дефолтное значение
    private float currentMasterVolume = 0.75f;    // Дефолтное значение
    private float currentSFXVolume = 0.75f;       // Дефолтное значение
    private float currentMusicVolume = 0.75f;     // Дефолтное значение


    void Awake()
    {
        Debug.Log("GameSettingsManager: Awake() called.");
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("GameSettingsManager: Destroying duplicate instance.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log("GameSettingsManager: Instance set.");
        DontDestroyOnLoad(gameObject);
        Debug.Log("GameSettingsManager: DontDestroyOnLoad called for: " + gameObject.name);
    }

    void Start()
    {
        Debug.Log("GameSettingsManager: Start() called. Attempting to load and apply settings.");
        // Вызываем LoadSettings здесь, чтобы инициализировать все значения
        LoadSettings();
    }

    // --- Метод для загрузки и применения настроек (заменяет ApplySettingsFromGameData) ---
    // Этот метод теперь PUBLIC и вызывается из MainMenuManager.Start() и GameSettingsManager.Start()
    public void LoadSettings()
    {
        GameSettings settingsToApply;

        if (SaveLoadManager.Instance != null)
        {
            if (SaveLoadManager.Instance.currentGameData != null)
            {
                Debug.Log("GameSettingsManager: Loading settings from existing SaveLoadManager.currentGameData.");
                settingsToApply = SaveLoadManager.Instance.currentGameData.gameSettings;
            }
            else
            {
                Debug.LogWarning("GameSettingsManager: SaveLoadManager.Instance.currentGameData is NULL. Applying default settings.");
                settingsToApply = new GameSettings(); // Дефолтные настройки
            }
        }
        else
        {
            Debug.LogError("GameSettingsManager: SaveLoadManager.Instance is NULL. Applying default settings.");
            settingsToApply = new GameSettings(); // Дефолтные настройки
        }

        // Убедимся, что settingsToApply не null (хотя по логике выше не должен быть)
        if (settingsToApply == null)
        {
            Debug.LogError("GameSettingsManager: settingsToApply оказался NULL после всех проверок. Это ошибка. Применяем дефолтные.");
            settingsToApply = new GameSettings();
        }

        Debug.Log("GameSettingsManager: Применяем настройки:");
        Debug.Log($"  Mouse Sensitivity: {settingsToApply.mouseSensitivity}");
        Debug.Log($"  Master Volume: {settingsToApply.masterVolume}");
        Debug.Log($"  SFX Volume: {settingsToApply.sfxVolume}");
        Debug.Log($"  Music Volume: {settingsToApply.musicVolume}");

        // Обновляем приватные поля
        currentMouseSensitivity = settingsToApply.mouseSensitivity;
        currentMasterVolume = settingsToApply.masterVolume;
        currentSFXVolume = settingsToApply.sfxVolume;
        currentMusicVolume = settingsToApply.musicVolume;

        // Применяем к игре (обновляем AudioMixer и чувствительность игрока)
        UpdatePlayerMouseSensitivity(currentMouseSensitivity);
        SetMixerVolume("MasterVolumeParam", currentMasterVolume);
        SetMixerVolume("SFXVolumeParam", currentSFXVolume);
        SetMixerVolume("MusicVolumeParam", currentMusicVolume);
    }

    // Вспомогательный метод для установки громкости AudioMixer
    private void SetMixerVolume(string paramName, float volume)
    {
        if (mainAudioMixer != null)
        {
            // Убеждаемся, что громкость не 0 для логарифма
            float dbVolume = volume > 0.0001f ? Mathf.Log10(volume) * 20 : -80;
            mainAudioMixer.SetFloat(paramName, dbVolume);
            // Debug.Log($"GameSettingsManager: Установлена громкость {paramName} на {volume} ({dbVolume} dB)."); 
        }
        else
        {
            Debug.LogWarning($"GameSettingsManager: AudioMixer не назначен или параметр {paramName} не найден. Проверьте инспектор.");
        }
    }

    // Методы для обновления настроек (вызываются из OptionsController)
    public void SetMouseSensitivity(float sensitivity)
    {
        currentMouseSensitivity = sensitivity;
        UpdatePlayerMouseSensitivity(sensitivity);
        if (SaveLoadManager.Instance != null && SaveLoadManager.Instance.currentGameData != null)
        {
            SaveLoadManager.Instance.currentGameData.gameSettings.mouseSensitivity = sensitivity;
            // Debug.Log($"GameSettingsManager: Mouse sensitivity updated to {sensitivity} in SaveLoadManager.currentGameData.");
        }
        else
        {
            Debug.LogError("GameSettingsManager: SaveLoadManager.Instance or currentGameData is NULL when setting mouse sensitivity.");
        }
    }

    public void SetMasterVolume(float volume)
    {
        currentMasterVolume = volume;
        SetMixerVolume("MasterVolumeParam", volume);
        if (SaveLoadManager.Instance != null && SaveLoadManager.Instance.currentGameData != null)
        {
            SaveLoadManager.Instance.currentGameData.gameSettings.masterVolume = volume;
            // Debug.Log($"GameSettingsManager: Master volume updated to {volume} in SaveLoadManager.currentGameData.");
        }
        else
        {
            Debug.LogError("GameSettingsManager: SaveLoadManager.Instance or currentGameData is NULL when setting master volume.");
        }
    }

    public void SetSFXVolume(float volume)
    {
        currentSFXVolume = volume;
        SetMixerVolume("SFXVolumeParam", volume);
        if (SaveLoadManager.Instance != null && SaveLoadManager.Instance.currentGameData != null)
        {
            SaveLoadManager.Instance.currentGameData.gameSettings.sfxVolume = volume;
            // Debug.Log($"GameSettingsManager: SFX volume updated to {volume} in SaveLoadManager.currentGameData.");
        }
        else
        {
            Debug.LogError("GameSettingsManager: SaveLoadManager.Instance or currentGameData is NULL when setting SFX volume.");
        }
    }

    public void SetMusicVolume(float volume)
    {
        currentMusicVolume = volume;
        SetMixerVolume("MusicVolumeParam", volume);
        if (SaveLoadManager.Instance != null && SaveLoadManager.Instance.currentGameData != null)
        {
            SaveLoadManager.Instance.currentGameData.gameSettings.musicVolume = volume;
            // Debug.Log($"GameSettingsManager: Music volume updated to {volume} in SaveLoadManager.currentGameData.");
        }
        else
        {
            Debug.LogError("GameSettingsManager: SaveLoadManager.Instance or currentGameData is NULL when setting music volume.");
        }
    }

    // --- Метод для обновления чувствительности мыши игрока ---
    public void UpdatePlayerMouseSensitivity(float sensitivity)
    {
        // GameObject.FindGameObjectWithTag("Player") дорогая операция,
        // лучше найти игрока один раз и сохранить ссылку, или сделать игрока частью Singleton'а.
        // Но для текущей демонстрации это подойдет.
        GameObject playerGameObject = GameObject.FindGameObjectWithTag("Player");
        if (playerGameObject != null)
        {
            PlayerController playerController = playerGameObject.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.SetMouseSensitivityDirectly(sensitivity);
            }
            // else { Debug.LogWarning("GameSettingsManager: PlayerController не найден на объекте с тегом 'Player'."); }
        }
        // else { Debug.Log("GameSettingsManager: Объект с тегом 'Player' не найден на сцене."); }
    }
}