using UnityEngine;
using UnityEngine.Events; // Для использования UnityEvent

/// <summary>
/// Базовый скрипт для интерактивного рычага.
/// Обрабатывает визуальное переключение, звук и оповещает слушателей об изменении состояния.
/// </summary>
public class Lever : MonoBehaviour
{
    // Перечисление для обозначения текущего положения рычага
    public enum LeverPosition { Up, Down }

    [Tooltip("Уникальный идентификатор для этого рычага (например, 'A', 'B').")]
    public string leverID;

    [Tooltip("Начальное положение рычага при старте игры.")]
    public LeverPosition currentPosition = LeverPosition.Down;

    [Tooltip("Событие, которое срабатывает при переключении рычага. Передает leverID.")]
    public UnityEvent<string> OnLeverToggled = new UnityEvent<string>();

    [Tooltip("Звуковой клип, воспроизводимый при переключении рычага.")]
    public AudioClip toggleSound;

    [Header("Rotation Settings")]
    [Tooltip("Углы Эйлера (X, Y, Z) для положения рычага 'Вверх'. Настройте в инспекторе.")]
    public Vector3 upRotationAngles;

    [Tooltip("Углы Эйлера (X, Y, Z) для положения рычага 'Вниз'. Настройте в инспекторе.")]
    public Vector3 downRotationAngles;

    private AudioSource audioSource;
    private bool isInitialized = false; // Флаг для контроля инициализации

    /// <summary>
    /// Инициализация компонента AudioSource и установка начального визуального положения рычага.
    /// </summary>
    void Awake()
    {
        if (string.IsNullOrEmpty(leverID))
        {
            Debug.LogError($"Lever: Рычаг '{gameObject.name}' не имеет назначенного leverID! Это может привести к ошибкам.");
            // Можно сгенерировать временный ID или отключить компонент, если ID обязателен
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false; // Не воспроизводить звук при старте
            Debug.Log($"Lever {leverID}: AudioSource добавлен к рычагу.");
        }

        UpdateLeverAppearance(); // Устанавливаем начальное визуальное положение
        isInitialized = true;
        Debug.Log($"Lever {leverID}: Инициализация завершена. Текущая позиция: {currentPosition}.");
    }

    /// <summary>
    /// Переключает положение рычага между Up и Down.
    /// Этот метод вызывается внешними системами (например, PlayerInteraction).
    /// </summary>
    public void OnInteract()
    {
        if (!isInitialized)
        {
            Debug.LogWarning($"Lever {leverID}: Попытка взаимодействовать до полной инициализации.");
            return;
        }
        Debug.Log($"Lever {leverID}: Метод OnInteract() вызван.");
        ToggleLever();
    }

    /// <summary>
    /// Основная логика переключения рычага: меняет состояние, обновляет вид,
    /// воспроизводит звук и вызывает событие.
    /// </summary>
    private void ToggleLever()
    {
        Debug.Log($"Lever {leverID}: Метод ToggleLever() вызван. Старая позиция: {currentPosition}.");

        if (currentPosition == LeverPosition.Up)
        {
            currentPosition = LeverPosition.Down;
        }
        else
        {
            currentPosition = LeverPosition.Up;
        }

        Debug.Log($"Lever {leverID}: Новая позиция: {currentPosition}.");

        UpdateLeverAppearance(); // Обновляем визуальное представление рычага
        PlayToggleSound();      // Воспроизводим звук переключения

        // Оповещаем всех слушателей об изменении состояния рычага, передавая его ID
        OnLeverToggled.Invoke(leverID);
        Debug.Log($"Lever {leverID}: Событие OnLeverToggled вызвано для ID: {leverID}.");
    }

    /// <summary>
    /// Обновляет визуальное представление рычага, устанавливая его Local Euler Angles.
    /// </summary>
    private void UpdateLeverAppearance()
    {
        if (!isInitialized) return; // Защита от вызова до Awake

        Vector3 targetRotation;
        if (currentPosition == LeverPosition.Up)
        {
            targetRotation = upRotationAngles;
        }
        else
        {
            targetRotation = downRotationAngles;
        }
        transform.localEulerAngles = targetRotation;
        Debug.Log($"Lever {leverID}: Визуальное обновление. Установлены углы Эйлера: {targetRotation} для позиции {currentPosition}.");
    }

    /// <summary>
    /// Воспроизводит звук переключения, если он назначен.
    /// </summary>
    private void PlayToggleSound()
    {
        if (toggleSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(toggleSound);
            Debug.Log($"Lever {leverID}: Воспроизведен звук переключения.");
        }
        else
        {
            Debug.LogWarning($"Lever {leverID}: Звук переключения или AudioSource не назначены/найдены.");
        }
    }

    /// <summary>
    /// Возвращает текущее положение рычага.
    /// </summary>
    public LeverPosition GetCurrentPosition()
    {
        return currentPosition;
    }
}