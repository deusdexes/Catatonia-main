using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LightController : MonoBehaviour
{
    public static LightController Instance { get; private set; }

    [Header("Lights to Control")]
    [Tooltip("Список всех основных источников света (люстр), которые будут управляться генератором.")]
    public List<Light> managedLights = new List<Light>();

    [Tooltip("Список источников света (например, красных аварийных ламп), работающих от резервного питания.")]
    public List<Light> backupLights = new List<Light>();

    [Header("Flicker Settings")]
    [Tooltip("Должен ли свет мигать, когда генератор включен?")]
    public bool enableFlicker = true;
    [Tooltip("Минимальная задержка между миганиями (в секундах).")]
    public float minFlickerDelay = 5f;
    [Tooltip("Максимальная задержка между миганиями (в секундах).")]
    public float maxFlickerDelay = 15f;
    [Tooltip("Длительность одного мигания (свет выключен) в секундах.")]
    public float flickerDuration = 0.5f;

    // Сделаем _isGeneratorOn приватным и инициализируем его как false
    private bool _isGeneratorOn = false;
    private bool _isFlickering = false;
    private Coroutine flickerCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        // Изначально все ОСНОВНЫЕ лампы должны быть выключены
        TurnManagedLightsOff();

        TurnBackupLightsOn();

        _isGeneratorOn = false; // Убеждаемся, что генератор выключен по логике контроллера
    }

    // Вызывается GeneratorLever для установки состояния генератора
    public void SetGeneratorState(bool on)
    {
        if (_isGeneratorOn == on) return; // Состояние не изменилось

        _isGeneratorOn = on;
        Debug.Log($"LightController: Генератор переключен в состояние: {_isGeneratorOn}");

        if (_isGeneratorOn)
        {
            // 1. Включаем ОСНОВНОЕ освещение
            TurnManagedLightsOn();
            // 2. Выключаем РЕЗЕРВНОЕ освещение
            TurnBackupLightsOff();

            if (enableFlicker)
            {
                flickerCoroutine = StartCoroutine(FlickerLightsRoutine());
            }
        }
        else
        {
            // 1. Выключаем ОСНОВНОЕ освещение
            TurnManagedLightsOff();
            // 2. Включаем РЕЗЕРВНОЕ освещение
            TurnBackupLightsOn();

            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
                flickerCoroutine = null;
            }
            // Также убедимся, что свет не мигает после выключения генератора
            _isFlickering = false;
        }
    }


    // Переименованы для ясности, чтобы не путать с резервным питанием
    void TurnManagedLightsOn()
    {
        SetLightsState(managedLights, true);
    }

    void TurnManagedLightsOff()
    {
        SetLightsState(managedLights, false);
    }


    void TurnBackupLightsOn()
    {
        SetLightsState(backupLights, true);
    }

    void TurnBackupLightsOff()
    {
        SetLightsState(backupLights, false);
    }

    private void SetLightsState(List<Light> lights, bool state)
    {
        for (int i = 0; i < lights.Count; i++)
        {
            if (lights[i] != null)
            {
                lights[i].enabled = state;
            }
        }
    }

    IEnumerator FlickerLightsRoutine()
    {
        while (_isGeneratorOn) // Продолжаем мигать, пока генератор включен
        {
            float delay = Random.Range(minFlickerDelay, maxFlickerDelay);
            yield return new WaitForSeconds(delay);

            if (!_isGeneratorOn) break; // Проверка на случай, если генератор выключили во время ожидания

            _isFlickering = true;
            TurnManagedLightsOff(); // Выключаем свет (основной)
            yield return new WaitForSeconds(flickerDuration);

            if (!_isGeneratorOn) break; // Проверка после мигания

            TurnManagedLightsOn(); // Включаем свет обратно (основной)
            _isFlickering = false;
        }
        _isFlickering = false; // Убедиться, что флаг сброшен, если корутина завершилась
    }
}