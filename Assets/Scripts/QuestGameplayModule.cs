using UnityEngine;
using System.Collections.Generic; 
using System.Linq; 
using GameSystem;

// Класс для связывания ID квеста с конфигурацией сокращения таймера
[System.Serializable]
public class QuestTimerEffect
{
    [Tooltip("ID задачи, на которую влияет этот эффект (должен совпадать с QuestManager).")]
    public string objectiveID;

    [Tooltip("Конфигурация сокращения таймера для этой задачи.")]
    public TimerReductionConfig timerReductionConfig;
}

public class QuestGameplayModule : MonoBehaviour
{
    // Важно: Структура QuestObjective берется из файла QuestManager.cs, 
    // поэтому этот скрипт должен быть в состоянии ее "видеть" (что обычно происходит по умолчанию в Unity).

    [Header("Monster Timer Settings")]
    [Tooltip("Ссылка на CreatureController в сцене.")]
    public CreatureController creatureController;

    [Tooltip("Список эффектов сокращения таймера, привязанных к ID задач.")]
    public List<QuestTimerEffect> timerEffects = new List<QuestTimerEffect>();

    void Start()
    {
        // Находим CreatureController, если он не назначен
        if (creatureController == null)
        {
            creatureController = FindAnyObjectByType<CreatureController>();
            if (creatureController == null)
            {
                Debug.LogError("QuestGameplayModule: CreatureController not found in scene! Quest effects cannot be processed.", this);
            }
        }

        // Подписываемся на событие завершения квеста из QuestManager
        if (QuestManager.Instance != null)
        {
            // Здесь используется QuestObjective из QuestManager
            QuestManager.Instance.OnObjectiveCompleted += HandleObjectiveCompleted;
        }
        else
        {
            Debug.LogError("QuestGameplayModule: QuestManager not found! Gameplay effects will not work.", this);
        }
    }

    private void OnDestroy()
    {
        // Отписываемся от события при уничтожении объекта
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnObjectiveCompleted -= HandleObjectiveCompleted;
        }
    }

    // Метод, который срабатывает при выполнении задачи
    private void HandleObjectiveCompleted(QuestObjective completedObjective) // Использование QuestObjective из QuestManager
    {
        // Ищем соответствующий эффект сокращения таймера
        QuestTimerEffect effect = timerEffects.FirstOrDefault(e => e.objectiveID == completedObjective.objectiveID);

        if (effect != null && effect.timerReductionConfig != null && creatureController != null)
        {
            // Применяем сокращение таймера
            float reductionAmount = UnityEngine.Random.Range(effect.timerReductionConfig.minReduction, effect.timerReductionConfig.maxReduction);

            // Предполагаем, что у CreatureController есть метод ReduceTimer(float amount)
            creatureController.ReduceTimer(reductionAmount);

            Debug.Log($"[QuestGameplayModule] Timer reduced by {reductionAmount:F1}s for objective: {completedObjective.displayName}");
        }
    }
}