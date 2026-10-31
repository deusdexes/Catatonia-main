using UnityEngine;
using GameSystem;
#if UNITY_EDITOR
using UnityEditor; // Для Handles.Label в OnDrawGizmosSelected
#endif

public class GeneratorLever : MonoBehaviour
{
    [Header("Lever Settings")]
    [Tooltip("Transform рычага, который будет вращаться.")]
    public Transform leverPivot;
    [Tooltip("Локальный X-угол, когда генератор выключен.")]
    public float leverOffLocalRotationX = 0f;
    [Tooltip("Локальный X-угол, когда генератор включен.")]
    public float leverOnLocalRotationX = -90f; // Пример: повернуть вниз на 90 градусов
    [Tooltip("Скорость поворота рычага.")]
    public float leverRotateSpeed = 5f;

    [Header("Generator State")]
    public bool isGeneratorOn = false;

    [Header("Audio Settings")]
    public AudioClip activateSound; // Звук при включении генератора
    public AudioClip deactivateSound; // Звук при выключении генератора
    public AudioClip leverMoveSound; // Звук движения рычага
    [Tooltip("Зацикленный звук работающего генератора.")]
    public AudioClip runningLoopSound;
    [Range(0f, 1f)]
    public float volume = 1f;

    private AudioSource audioSource;
    private Quaternion leverOffRotation;
    private Quaternion leverOnRotation;
    private PuzzleManager puzzleManager; // Ссылка на PuzzleManager

    void Start()
    {
        if (leverPivot == null)
        {
            leverPivot = transform; // Если не назначен, используем сам объект
            Debug.LogWarning("GeneratorLever: Lever Pivot не назначен, используется сам GameObject.", this);
        }

        // Инициализация AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.volume = volume;
        audioSource.playOnAwake = false;
        audioSource.loop = false; // Звук движения рычага не должен зацикливаться
        audioSource.spatialBlend = 1.0f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.maxDistance = 30f;
        // Найдем PuzzleManager в сцене
        puzzleManager = FindAnyObjectByType<PuzzleManager>();
        if (puzzleManager == null)
        {
            Debug.LogError("GeneratorLever: PuzzleManager не найден в сцене! Рычаг генератора не сможет проверить статус головоломки.");
        }

        // Задаем целевые повороты по оси X
        leverOffRotation = Quaternion.Euler(leverOffLocalRotationX, leverPivot.localRotation.eulerAngles.y, leverPivot.localRotation.eulerAngles.z);
        leverOnRotation = Quaternion.Euler(leverOnLocalRotationX, leverPivot.localRotation.eulerAngles.y, leverPivot.localRotation.eulerAngles.z);

        // Устанавливаем начальное положение рычага
        leverPivot.localRotation = isGeneratorOn ? leverOnRotation : leverOffRotation;

        if (isGeneratorOn && runningLoopSound != null)
        {
            audioSource.clip = runningLoopSound;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    void Update()
    {
        // Плавное вращение рычага
        Quaternion targetRotation = isGeneratorOn ? leverOnRotation : leverOffRotation;
        leverPivot.localRotation = Quaternion.Slerp(leverPivot.localRotation, targetRotation, Time.deltaTime * leverRotateSpeed);
    }

    // Этот метод будет вызван PlayerInteraction
    public void OnInteract()
    {
        // Добавляем условие: можно взаимодействовать, только если головоломка решена
        if (puzzleManager != null && !puzzleManager.IsPuzzleSolved)
        {
            Debug.Log("GeneratorLever: Головоломка еще не решена! Невозможно взаимодействовать с рычагом генератора.");
            return; // Выходим из метода, не переключая рычаг
        }

        isGeneratorOn = !isGeneratorOn; // Переключаем состояние генератора
        Debug.Log($"GeneratorLever: Генератор переключен в состояние: {isGeneratorOn}");

        // Воспроизводим звук движения рычага
        if (leverMoveSound != null)
        {
            // Используем PlayOneShot, чтобы не прерывать потенциальный звук работы
            audioSource.PlayOneShot(leverMoveSound, volume);
        }

        // --- ЛОГИКА УПРАВЛЕНИЯ ЗВУКОМ РАБОТЫ ГЕНЕРАТОРА ---
        if (isGeneratorOn)
        {
            // Генератор включен: проигрываем звук активации и запускаем цикл
            if (activateSound != null)
            {
                audioSource.PlayOneShot(activateSound, volume);
            }

            if (runningLoopSound != null)
            {
                // Настраиваем AudioSource для зацикленного звука работы
                audioSource.clip = runningLoopSound;
                audioSource.loop = true;
                // Не используем PlayOneShot, а Play(), чтобы запустить цикл
                audioSource.Play();
            }
        }
        else
        {
            // Генератор выключен: проигрываем звук деактивации и останавливаем цикл
            if (deactivateSound != null)
            {
                audioSource.PlayOneShot(deactivateSound, volume);
            }

            if (runningLoopSound != null && audioSource.clip == runningLoopSound)
            {
                // Останавливаем зацикленный звук, если он играет
                audioSource.Stop();
                audioSource.loop = false; // Сбрасываем loop, на всякий случай
            }
        }
        // --- КОНЕЦ ЛОГИКИ УПРАВЛЕНИЯ ЗВУКОМ РАБОТЫ ГЕНЕРАТОРА ---
        if (isGeneratorOn && QuestManager.Instance != null)
        {
            // Квест завершается, как только генератор включается
            QuestManager.Instance.CompleteObjective("QUEST_START_GENERATOR");
        }
        // Уведомляем LightController об изменении состояния генератора
        if (LightController.Instance != null)
        {
            LightController.Instance.SetGeneratorState(isGeneratorOn);
        }
        else
        {
            Debug.LogError("GeneratorLever: LightController.Instance не найден! Убедитесь, что LightController присутствует в сцене.", this);
        }
    }

    // Для Editor Gizmos (опционально, для удобства настройки)
    void OnDrawGizmosSelected()
    {
        if (leverPivot == null) leverPivot = transform;

        Gizmos.color = Color.yellow;
        Vector3 currentPos = leverPivot.position;

        // Draw 'Off' position
        Quaternion offRot = Quaternion.Euler(leverOffLocalRotationX, leverPivot.localRotation.eulerAngles.y, leverPivot.localRotation.eulerAngles.z);
        Vector3 offDir = offRot * Vector3.up; // Используем Up, так как обычно рычаг поворачивается вверх/вниз относительно своей локальной оси

        Gizmos.DrawLine(currentPos, currentPos + leverPivot.TransformDirection(offDir).normalized * 0.5f);
        Gizmos.DrawSphere(currentPos + leverPivot.TransformDirection(offDir).normalized * 0.5f, 0.05f);
#if UNITY_EDITOR
        Handles.Label(currentPos + leverPivot.TransformDirection(offDir).normalized * 0.6f, "OFF");
#endif

        // Draw 'On' position
        Gizmos.color = Color.green;
        Quaternion onRot = Quaternion.Euler(leverOnLocalRotationX, leverPivot.localRotation.eulerAngles.y, leverPivot.localRotation.eulerAngles.z);
        Vector3 onDir = onRot * Vector3.up;

        Gizmos.DrawLine(currentPos, currentPos + leverPivot.TransformDirection(onDir).normalized * 0.5f);
        Gizmos.DrawSphere(currentPos + leverPivot.TransformDirection(onDir).normalized * 0.5f, 0.05f);
#if UNITY_EDITOR
        Handles.Label(currentPos + leverPivot.TransformDirection(onDir).normalized * 0.6f, "ON");
#endif
    }
}