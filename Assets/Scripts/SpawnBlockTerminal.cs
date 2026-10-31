using UnityEngine;
using System.Linq; // Для использования LINQ

public class SpawnBlockTerminal : MonoBehaviour
{
    [Header("Terminal Settings")]
    public CreatureController creatureController;
    [Tooltip("Максимальное расстояние для взаимодействия (используется PlayerInteraction).")]
    public float maxInteractDistance = 3f;

    [Header("Generator & Monitor State")] // НОВЫЙ РАЗДЕЛ
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
        if (creatureController == null)
        {
            creatureController = FindAnyObjectByType<CreatureController>();
            if (creatureController == null)
            {
                Debug.LogError("CreatureController not found for SpawnBlockTerminal!", this);
                enabled = false;
            }
        }
        if (generatorLever == null)
        {
            generatorLever = FindAnyObjectByType<GeneratorLever>();
        }
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
        if (_hasBeenUsed || creatureController == null) return;

        if (!IsOperational)
        {
            if (generatorLever == null || !generatorLever.isGeneratorOn)
            {
                Debug.Log("SpawnBlockTerminal: Невозможно взаимодействовать. Генератор выключен.");
            }
            return;
        }

        Transform playerTransform = FindAnyObjectByType<PlayerController>()?.transform;
        if (playerTransform == null) return;

        Transform[] allSpawnPoints = creatureController.spawnPoints;
        if (allSpawnPoints == null || allSpawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points configured in CreatureController.");
            return;
        }

        // Находим ближайшую *доступную* (не заблокированную) точку спавна
        Transform nearestAvailableSpawn = allSpawnPoints
            .Where(p => p != null && !creatureController.blockedSpawnPoints.Contains(p))
            .OrderBy(p => Vector3.Distance(playerTransform.position, p.position))
            .FirstOrDefault();

        if (nearestAvailableSpawn != null)
        {
            creatureController.BlockSpawnPoint(nearestAvailableSpawn);
            _hasBeenUsed = true;
            UpdateMonitorVisuals();
            Debug.Log($"SpawnBlockTerminal used! Blocked the nearest available spawn point: {nearestAvailableSpawn.name}.");
            // GetComponent<Collider>().enabled = false; // Отключаем терминал
        }
        else
        {
            Debug.Log("No unblocked spawn points remaining to block.");
        }
    }
}