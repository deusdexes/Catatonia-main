using UnityEngine;
using System.Collections;

public class MonsterMusicController : MonoBehaviour
{
    // Ссылка на контроллер монстра, чтобы считывать его состояние
    [Header("Controller Reference")]
    public CreatureController monsterController;

    [Header("Audio Sources")]
    [Tooltip("Источник звука для фоновой музыки (когда монстра НЕТ).")]
    public AudioSource idleAudioSource;
    [Tooltip("Источник звука для музыки преследования/поиска (когда монстр ЕСТЬ).")]
    public AudioSource activeAudioSource;

    // Сэмплы для каждого состояния
    [Header("Music Clips")]
    [Tooltip("Основной фоновый трек (для состояний Idle/ReturningToSpawn)")]
    public AudioClip idleMusicClip;
    [Tooltip("Трек активности/поиска (для состояний Patrolling/Chasing/Redirected)")]
    public AudioClip activeMusicClip;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [Tooltip("Максимальная громкость фонового трека.")]
    public float idleMaxVolume = 0.5f; 
    [Range(0f, 1f)]
    [Tooltip("Максимальная громкость активного трека.")]
    public float activeMaxVolume = 1.0f; 

    [Header("Fading Settings")]
    [Tooltip("Время (в секундах), за которое происходит плавное изменение громкости (кроссфейд)")]
    public float crossfadeDuration = 3.0f;

    // Приватные поля для отслеживания состояний
    private CreatureController.MonsterState _currentTrackState = CreatureController.MonsterState.Idle; // Текущее состояние, под которое играет музыка
    private Coroutine _fadeCoroutine = null; // Чтобы гарантировать, что одновременно идет только один кроссфейд

    void Start()
    {
        if (monsterController == null)
        {
            // Пытаемся найти контроллер на том же объекте или в сцене
            monsterController = GetComponent<CreatureController>();
            if (monsterController == null)
            {
                Debug.LogError("MonsterMusicController requires a reference to the CreatureController.", this);
                enabled = false;
                return;
            }
        }

        // Базовая настройка источников звука
        SetupAudioSources();

        // Запускаем начальную музыку
        StartIdleMusic();
    }

    void Update()
    {
        bool isMonsterActive = monsterController.CurrentMonsterState != CreatureController.MonsterState.Idle;
        bool isPlayerInArea = monsterController.IsPlayerInWarningArea();

        // Получаем текущее состояние монстра
        CreatureController.MonsterState newTrackState;

        // Определяем, какое состояние монстра должно вызвать "Активную" музыку
    if (isMonsterActive && isPlayerInArea)
    {
        // Если оба условия выполнены -> включаем активную музыку.
        newTrackState = CreatureController.MonsterState.Patrolling; // Используем Patrolling как флаг "Активной" музыки
    }
    else
    {
        // Если монстр спит ИЛИ игрок далеко -> включаем спокойную музыку.
        newTrackState = CreatureController.MonsterState.Idle;
    }

        // Проверяем, изменилось ли состояние музыки
        if (newTrackState != _currentTrackState)
        {
            _currentTrackState = newTrackState;

            if (_currentTrackState == CreatureController.MonsterState.Idle)
            {
                // Переход от Активного к Идл: (Уходящий, Приходящий, Макс. громкость приходящего)
                StartCrossfade(activeAudioSource, idleAudioSource, idleMaxVolume, crossfadeDuration);
                Debug.Log("Music State Change: Transition to IDLE/CALM music.");
            }
            else
            {
                // Переход от Идл к Активному: (Уходящий, Приходящий, Макс. громкость приходящего)
                StartCrossfade(idleAudioSource, activeAudioSource, activeMaxVolume, crossfadeDuration);
                Debug.Log("Music State Change: Transition to ACTIVE/TENSION music.");
            }
        }
    }

    // Вспомогательный метод для настройки источников
    private void SetupAudioSources()
    {
        if (idleAudioSource == null || activeAudioSource == null)
        {
            // Если не назначены, добавляем их автоматически
            AudioSource[] existingSources = GetComponents<AudioSource>();

            if (existingSources.Length < 2)
            {
                idleAudioSource = gameObject.AddComponent<AudioSource>();
                activeAudioSource = gameObject.AddComponent<AudioSource>();
            }
            else
            {
                // Если два уже есть, используем их
                idleAudioSource = existingSources[0];
                activeAudioSource = existingSources[1];
            }
        }

        // Настраиваем общие параметры
        idleAudioSource.clip = idleMusicClip;
        idleAudioSource.loop = true;
        idleAudioSource.volume = 0f;
        idleAudioSource.playOnAwake = false;

        activeAudioSource.clip = activeMusicClip;
        activeAudioSource.loop = true;
        activeAudioSource.volume = 0f;
        activeAudioSource.playOnAwake = false;

        // Если сэмплы не назначены, выводим предупреждение
        if (idleMusicClip == null || activeMusicClip == null)
        {
            Debug.LogWarning("MonsterMusicController: One or both music clips are not assigned!", this);
        }
    }

    // Начальная загрузка и запуск музыки
    private void StartIdleMusic()
    {
        if (idleAudioSource.clip != null)
        {
            idleAudioSource.volume = idleMaxVolume; // Устанавливаем максимальную громкость
            idleAudioSource.Play();
            activeAudioSource.Stop(); // Гарантируем, что активный не играет
        }
    }

    // Запуск кроссфейда (с добавлением maxVolumeIn)
    private void StartCrossfade(AudioSource sourceToFadeOut, AudioSource sourceToFadeIn, float maxVolumeIn, float duration)
    {
        // Останавливаем предыдущую корутину, чтобы не было конфликтов
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        // Запускаем новую корутину
        _fadeCoroutine = StartCoroutine(CrossfadeCoroutine(sourceToFadeOut, sourceToFadeIn, maxVolumeIn, duration));
    }

    // Корутина для плавного кроссфейда (с добавлением maxVolumeIn)
    private IEnumerator CrossfadeCoroutine(AudioSource fadeOutSource, AudioSource fadeInSource, float maxVolumeIn, float duration)
    {
        float timer = 0f;
        float startVolumeOut = fadeOutSource.volume;
        float startVolumeIn = fadeInSource.volume;

        // Запускаем входящий трек, если он еще не играет
        if (!fadeInSource.isPlaying)
        {
            fadeInSource.volume = 0f;
            fadeInSource.Play();
        }

        while (timer < duration)
        {
            // Вычисляем прогресс (от 0 до 1)
            float progress = timer / duration;

            // Плавно уменьшаем громкость исходящего источника (всегда до 0)
            fadeOutSource.volume = Mathf.Lerp(startVolumeOut, 0f, progress);

            // Плавно увеличиваем громкость входящего источника (до maxVolumeIn)
            fadeInSource.volume = Mathf.Lerp(startVolumeIn, maxVolumeIn, progress);

            timer += Time.deltaTime;
            yield return null;
        }

        // Устанавливаем финальные значения
        fadeOutSource.volume = 0f;
        fadeInSource.volume = maxVolumeIn;

        // Останавливаем исходящий трек
        fadeOutSource.Stop();

        _fadeCoroutine = null; // Сброс корутины
    }
}