using UnityEngine;

/// <summary>
/// Скрипт для управления состоянием одной лампочки.
/// Переключает материалы и воспроизводит звук при изменении состояния.
/// </summary>
public class Lamp : MonoBehaviour
{
    [Tooltip("Уникальный идентификатор для этой лампочки.")]
    public int lampID;

    [Tooltip("Материал, используемый, когда лампочка выключена.")]
    public Material offMaterial;

    [Tooltip("Материал, используемый, когда лампочка включена.")]
    public Material onMaterial;

    [Tooltip("Звуковой клип, воспроизводимый при смене состояния лампочки.")]
    public AudioClip toggleSound;

    [SerializeField] // Сделаем приватное поле видимым в инспекторе для отладки
    private bool isOn = false; // Текущее состояние лампочки (включена/выключена)

    // meshRenderer теперь будет найден на родительском объекте
    private MeshRenderer meshRenderer;
    private AudioSource audioSource;
    private bool isInitialized = false; // Флаг для контроля инициализации

    /// <summary>
    /// Свойство для получения текущего состояния лампочки.
    /// </summary>
    public bool IsOn { get { return isOn; } }

    /// <summary>
    /// Инициализация компонентов и установка начального визуального состояния.
    /// </summary>
    void Awake()
    {
        // *** ИЗМЕНЕНИЕ ЗДЕСЬ: Используем GetComponentInParent для поиска MeshRenderer на родительских объектах ***
        meshRenderer = GetComponentInParent<MeshRenderer>();
        if (meshRenderer == null)
        {
            Debug.LogError($"Lamp {lampID} ({gameObject.name}): MeshRenderer не найден на объекте или его родительских. Лампочка не сможет менять свой вид!");
        }
        else
        {
            Debug.Log($"Lamp {lampID}: MeshRenderer найден на объекте: {meshRenderer.gameObject.name}");
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            Debug.Log($"Lamp {lampID}: AudioSource добавлен к лампочке.");
        }

        UpdateAppearance(); // Устанавливаем начальный вид лампочки
        isInitialized = true;
        Debug.Log($"Lamp {lampID}: Инициализация завершена. Текущее состояние: IsOn: {isOn}");
    }

    /// <summary>
    /// Устанавливает состояние лампочки (включена/выключена).
    /// Если состояние меняется, обновляет вид и воспроизводит звук.
    /// </summary>
    /// <param name="state">True для включения, False для выключения.</param>
    public void SetState(bool state)
    {
        if (!isInitialized)
        {
            Debug.LogWarning($"Lamp {lampID}: Попытка установить состояние до полной инициализации. State: {state}.");
            return;
        }

        if (isOn != state) // Только если состояние действительно изменилось
        {
            bool oldState = isOn;
            isOn = state;
            Debug.Log($"Lamp {lampID}: SetState вызван. Старое состояние: {oldState}, Новое состояние: {isOn}.");
            UpdateAppearance();
            PlayToggleSound();
        }
        else
        {
            Debug.Log($"Lamp {lampID}: SetState вызван, но состояние уже '{state}'. Изменений не произошло.");
        }
    }

    /// <summary>
    /// Переключает текущее состояние лампочки на противоположное.
    /// </summary>
    public void Toggle()
    {
        if (!isInitialized)
        {
            Debug.LogWarning($"Lamp {lampID}: Попытка переключить лампочку до полной инициализации.");
            return;
        }

        bool oldState = isOn;
        isOn = !isOn;
        Debug.Log($"Lamp {lampID}: Toggle вызван. Старое состояние: {oldState}, Новое состояние: {isOn}.");
        UpdateAppearance();
        PlayToggleSound();
    }

    /// <summary>
    /// Обновляет материал лампочки в зависимости от ее состояния (isOn).
    /// </summary>
    private void UpdateAppearance()
    {
        if (!isInitialized) return; // Защита от вызова до Awake

        if (meshRenderer != null)
        {
            Material targetMaterial = isOn ? onMaterial : offMaterial;

            if (targetMaterial != null)
            {
                // Добавлена проверка, чтобы избежать лишних операций и дать более точные логи
                Material currentMaterial = meshRenderer.material;
                if (currentMaterial.name != targetMaterial.name + " (Instance)") // Сравниваем по имени, учитывая "(Instance)"
                {
                    meshRenderer.material = targetMaterial;
                    Debug.Log($"Lamp {lampID}: Обновление вида. Устанавливаем '{targetMaterial.name}'. Был '{currentMaterial.name}'. Объект: {meshRenderer.gameObject.name}");
                }
                else
                {
                    Debug.Log($"Lamp {lampID}: Обновление вида. Материал уже '{targetMaterial.name}'. Изменений не требуется. Объект: {meshRenderer.gameObject.name}");
                }
            }
            else
            {
                Debug.LogWarning($"Lamp {lampID}: {(isOn ? "OnMaterial" : "OffMaterial")} не назначен в инспекторе! Лампочка не может {(isOn ? "включиться" : "выключиться")} визуально. Объект: {meshRenderer.gameObject.name}");
            }
        }
        else
        {
            Debug.LogWarning($"Lamp {lampID}: MeshRenderer не найден при попытке обновить внешний вид. Визуальных изменений не будет. Объект: {gameObject.name}");
        }
    }

    /// <summary>
    /// Воспроизводит звук переключения, если он назначен.
    /// </summary>
    private void PlayToggleSound()
    {
        if (toggleSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(toggleSound);
            Debug.Log($"Lamp {lampID}: Воспроизведен звук переключения.");
        }
        else
        {
            Debug.LogWarning($"Lamp {lampID}: Звук переключения или AudioSource не назначены/найдены.");
        }
    }
}