using UnityEngine;
using UnityEngine.UI;
using TMPro; // Для TextMeshProUGUI

public class OptionsController : MonoBehaviour
{
    [Header("UI элементы настроек")]
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private TextMeshProUGUI mouseSensitivityValueText;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TextMeshProUGUI masterVolumeValueText;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI sfxVolumeValueText;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TextMeshProUGUI musicVolumeValueText;

    [Header("Диапазоны значений")]
    [SerializeField] private float minMouseSensitivity = 0.1f;
    [SerializeField] private float maxMouseSensitivity = 10.0f;
    [SerializeField] private float minVolume = 0.0001f; // Практически 0, чтобы избежать Log10(0)
    [SerializeField] private float maxVolume = 1.0f;

    void Start()
    {
        // Добавляем слушателей к слайдерам, чтобы обновлять значения в реальном времени
        if (mouseSensitivitySlider != null) mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
    }

    void OnDestroy()
    {
        // Отписываемся от слушателей
        if (mouseSensitivitySlider != null) mouseSensitivitySlider.onValueChanged.RemoveListener(OnMouseSensitivityChanged);
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
        if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
    }

    // Этот метод вызывается из MainMenuManager, когда панель настроек открывается
    public void OpenOptionsUI(float currentMouseSens, float currentMasterVol, float currentSfxVol, float currentMusicVol)
    {
        gameObject.SetActive(true); // Активируем панель настроек

        // Загружаем текущие значения настроек и устанавливаем их в UI
        SetUISliderAndText(mouseSensitivitySlider, mouseSensitivityValueText, currentMouseSens, minMouseSensitivity, maxMouseSensitivity);
        SetUISliderAndText(masterVolumeSlider, masterVolumeValueText, currentMasterVol, minVolume, maxVolume);
        SetUISliderAndText(sfxVolumeSlider, sfxVolumeValueText, currentSfxVol, minVolume, maxVolume);
        SetUISliderAndText(musicVolumeSlider, musicVolumeValueText, currentMusicVol, minVolume, maxVolume);
    }

    // Этот метод вызывается из MainMenuManager, когда панель настроек закрывается
    public void CloseOptionsUI()
    {
        Debug.Log("OptionsController: CloseOptionsUI() called.");
        // ... остальная логика

        if (GameSettingsManager.Instance != null)
        {
            Debug.Log("OptionsController: GameSettingsManager.Instance is available.");
            // ... ваш существующий код
            GameSettingsManager.Instance.SetMouseSensitivity(mouseSensitivitySlider.value);
            GameSettingsManager.Instance.SetMasterVolume(masterVolumeSlider.value);
            GameSettingsManager.Instance.SetSFXVolume(sfxVolumeSlider.value);
            GameSettingsManager.Instance.SetMusicVolume(musicVolumeSlider.value);
        }
        else
        {
            Debug.LogError("OptionsController: GameSettingsManager.Instance is NULL when closing options UI.");
        }
    }

    // Вспомогательный метод для установки значений в UI
    private void SetUISliderAndText(Slider slider, TextMeshProUGUI text, float value, float min, float max)
    {
        if (slider != null)
        {
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
        }
        if (text != null)
        {
            text.text = value.ToString("F2"); // Форматируем до двух знаков после запятой
        }
    }

    // Методы, вызываемые при изменении слайдеров
    void OnMouseSensitivityChanged(float value)
    {
        if (mouseSensitivityValueText != null) mouseSensitivityValueText.text = value.ToString("F2");
        if (GameSettingsManager.Instance != null) GameSettingsManager.Instance.SetMouseSensitivity(value);
    }

    void OnMasterVolumeChanged(float value)
    {
        if (masterVolumeValueText != null) masterVolumeValueText.text = value.ToString("F2");
        if (GameSettingsManager.Instance != null) GameSettingsManager.Instance.SetMasterVolume(value);
    }

    void OnSFXVolumeChanged(float value)
    {
        if (sfxVolumeValueText != null) sfxVolumeValueText.text = value.ToString("F2");
        if (GameSettingsManager.Instance != null) GameSettingsManager.Instance.SetSFXVolume(value);
    }

    void OnMusicVolumeChanged(float value)
    {
        if (musicVolumeValueText != null) musicVolumeValueText.text = value.ToString("F2");
        if (GameSettingsManager.Instance != null) GameSettingsManager.Instance.SetMusicVolume(value);
    }
}