using UnityEngine;

public class MonsterRedirectTerminal : MonoBehaviour
{
    [Header("Terminal Settings")]
    public CreatureController creatureController;
    [Tooltip("Точка (GameObject Transform), в которую будет перенаправлен монстр.")]
    public Transform redirectTargetLocation;
    [Tooltip("Минимальная продолжительность перенаправления в секундах, если таймер монстра слишком мал.")]
    public float minRedirectDuration = 10f;

    [Header("Generator & Monitor State")] 
    public GeneratorLever generatorLever;
    [Tooltip("Рендерер монитора терминала.")]
    public Renderer monitorRenderer;
    [Tooltip("Материал, когда терминал неактивен/не работает.")]
    public Material inactiveMaterial;
    [Tooltip("Материал, когда терминал активен/готов к использованию.")]
    public Material activeMaterial;

    private bool _hasBeenUsed = false;
    public bool IsUsed => _hasBeenUsed;
    public bool IsOperational => generatorLever != null && generatorLever.isGeneratorOn && !_hasBeenUsed;


    void Start()
    {
        if (creatureController == null) creatureController = FindAnyObjectByType<CreatureController>();
        if (creatureController == null)
        {
            Debug.LogError("CreatureController not found for MonsterRedirectTerminal!", this);
            enabled = false;
            return;
        }
        if (redirectTargetLocation == null)
        {
            Debug.LogError("Redirect Target Location is not assigned for MonsterRedirectTerminal!", this);
            enabled = false;
        }
        if (generatorLever == null)
        {
            generatorLever = FindAnyObjectByType<GeneratorLever>();
        }

        // Инициализация материала монитора
        UpdateMonitorVisuals();
    }

    void Update()
    {
        UpdateMonitorVisuals();
    }

    private void UpdateMonitorVisuals()
    {
        if (monitorRenderer == null || inactiveMaterial == null || activeMaterial == null) return;

        if (IsOperational) // Если генератор включен и терминал не использован
        {
            monitorRenderer.material = activeMaterial;
        }
        else // Если генератор выключен ИЛИ терминал уже использован
        {
            monitorRenderer.material = inactiveMaterial;
        }
    }

    // Этот метод будет вызван из PlayerInteraction.OnInteract
    public void OnInteract()
    {
        if (_hasBeenUsed || creatureController == null || redirectTargetLocation == null) return;
       
        if (!IsOperational)
        {
            if (generatorLever == null || !generatorLever.isGeneratorOn)
            {
                Debug.Log("MonsterRedirectTerminal: Невозможно взаимодействовать. Генератор выключен.");
            }
            // Если _hasBeenUsed == true, то IsOperational будет false, и мы просто игнорируем
            return;
        }
        // Проверяем, находится ли монстр в Idle состоянии (согласно задаче, берем половину Idle времени)
        if (creatureController.CurrentMonsterState != CreatureController.MonsterState.Idle)
        {
            Debug.Log("MonsterRedirectTerminal: Redirection failed. Monster is currently active (not Idle).");
            return;
        }

        // Вычисляем продолжительность: половина оставшегося Idle таймера
        float remainingIdleTime = creatureController.RemainingTimer;
        float redirectDuration = Mathf.Max(minRedirectDuration, remainingIdleTime / 2f);

        // Вызываем новый метод перенаправления
        creatureController.RedirectMonster(redirectTargetLocation, redirectDuration);

        _hasBeenUsed = true;
        UpdateMonitorVisuals();
        Debug.Log($"MonsterRedirectTerminal used! Monster redirected for {redirectDuration:F1} seconds.");
        // GetComponent<Collider>().enabled = false; // Отключаем терминал
    }
}
