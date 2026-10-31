// Файл: RotatingSafeDoor.cs (ФИНАЛЬНАЯ ВЕРСИЯ)
using UnityEngine;
using System.Collections;

public class RotatingSafeDoor : MonoBehaviour
{
    [Header("Puzzle Dependency")]
    [Tooltip("ID головоломки из PuzzleDatabaseManager, которая открывает этот сейф.")]
    public int requiredPuzzleID;

    [Header("Rotation Settings")]
    [Tooltip("Угол, на который нужно повернуть дверцу по оси Z для открытия.")]
    public float openAngleZ = -90.0f;

    [Tooltip("Скорость вращения дверцы.")]
    public float rotationSpeed = 2.0f;

    [Header("Audio Settings")]
    [Tooltip("Звук открытия дверцы сейфа.")]
    public AudioClip openSound;
    private AudioSource audioSource;

    // Состояние, чтобы дверь не открывалась повторно
    private bool isUnlocked = false;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    void Start()
    {
        // Инициализация AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Сохраняем начальное и рассчитываем целевое вращение
        closedRotation = transform.rotation;
        openRotation = closedRotation * Quaternion.Euler(0, 0, openAngleZ);

        // Подписываемся на событие ЕДИНОГО менеджера
        if (AcousticPuzzleManager.Instance != null)
        {
            AcousticPuzzleManager.Instance.OnPuzzleSolved += HandlePuzzleSolved;
        }
        else
        {
            Debug.LogError($"RotatingSafeDoor '{gameObject.name}': Не найден AcousticPuzzleManager в сцене!", this);
        }
    }

    /// <summary>
    /// Этот метод вызывается, когда ЛЮБАЯ акустическая головоломка решена.
    /// </summary>
    private void HandlePuzzleSolved(int solvedPuzzleID)
    {
        // Если сейф уже открыт или это не та головоломка, которую мы ждем, выходим.
        if (isUnlocked || solvedPuzzleID != requiredPuzzleID)
        {
            return;
        }

        // Устанавливаем флаг, чтобы не реагировать на будущие события
        isUnlocked = true;
        Debug.Log($"Сейф '{gameObject.name}' разблокирован головоломкой ID {solvedPuzzleID}. Начинаю открытие.");

        // Запускаем корутину для плавного вращения
        StartCoroutine(RotateDoorCoroutine());
    }

    /// <summary>
    /// Корутина для плавного вращения двери.
    /// </summary>
    private IEnumerator RotateDoorCoroutine()
    {
        // Воспроизводим звук
        if (audioSource != null && openSound != null)
        {
            audioSource.PlayOneShot(openSound);
        }

        float journey = 0f;
        Quaternion startingRotation = transform.rotation;

        // Плавно поворачиваем дверь от текущего положения к целевому
        while (journey < 1.0f)
        {
            journey += Time.deltaTime * rotationSpeed;
            transform.rotation = Quaternion.Slerp(startingRotation, openRotation, journey);
            yield return null; // Ждем следующего кадра
        }

        // По завершении цикла гарантированно ставим дверь в конечное положение
        transform.rotation = openRotation;
    }

    // Обязательно отписываемся от события при уничтожении объекта
    void OnDestroy()
    {
        if (AcousticPuzzleManager.Instance != null)
        {
            AcousticPuzzleManager.Instance.OnPuzzleSolved -= HandlePuzzleSolved;
        }
    }
}