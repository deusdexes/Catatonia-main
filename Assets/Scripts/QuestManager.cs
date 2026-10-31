using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // Для использования LINQ

namespace GameSystem
{
    // --- ЕДИНАЯ СТРУКТУРА ДЛЯ ВСЕХ ЗАДАЧ ---
    [System.Serializable]
    public class QuestObjective
    {
        public string objectiveID;     // Уникальный ID (например, "OPEN_DOOR_CAVE")
        public string displayName;     // Отображаемое имя (например, "Открыть дверь")
        public bool isCompleted = false;

        [Header("Grouping")] // НОВОЕ ПОЛЕ
        [Tooltip("Идентификатор группы квестов (например, 'ROBOT_ASSEMBLY').")]
        public string groupID; // Например, "ROBOT_ASSEMBLY"
        [Tooltip("Отображаемый заголовок для группы (отобразится только один раз).")]
        public string groupTitle; // Например, "Собрать робота"
    }

    // Менеджер, который управляет логикой квестов и генерирует события
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Tooltip("Список всех задач, которые будут отображаться в UI.")]
        public List<QuestObjective> activeObjectives = new List<QuestObjective>();

        // --- СОБЫТИЯ ---
        public event Action<QuestObjective> OnObjectiveCompleted;
        public event Action<QuestObjective> OnObjectiveActivated;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            // Активируем ТОЛЬКО первую незавершенную задачу при запуске.
            QuestObjective firstObjective = GetNextActiveObjective();
            if (firstObjective != null)
            {
                OnObjectiveActivated?.Invoke(firstObjective);
            }
        }

        /// <summary>
        /// Метод, вызываемый скриптами для завершения задачи.
        /// </summary>
        public void CompleteObjective(string objectiveID)
        {
            // Находим задачу по ID
            QuestObjective objective = activeObjectives.Find(o => o.objectiveID == objectiveID);

            if (objective != null && !objective.isCompleted)
            {
                objective.isCompleted = true;
                Debug.Log($"[QuestManager] Задача выполнена: {objective.displayName} (ID: {objectiveID})");

                // Генерируем событие завершения (UI подхватит его для вычеркивания и исчезновения)
                OnObjectiveCompleted?.Invoke(objective);
            }
            else if (objective == null)
            {
                Debug.LogWarning($"[QuestManager] Попытка завершить несуществующую задачу с ID: {objectiveID}");
            }
        }

        // Публичный метод, который UI будет вызывать после анимации исчезновения
        public void ActivateNextObjectiveIfAvailable()
        {
            QuestObjective nextObjective = GetNextActiveObjective();
            if (nextObjective != null)
            {
                Debug.Log($"[QuestManager] UI запустил активацию следующей задачи: {nextObjective.displayName}");
                OnObjectiveActivated?.Invoke(nextObjective);
            }
        }

        // Вспомогательный метод для получения ПЕРВОЙ незавершенной задачи
        public QuestObjective GetNextActiveObjective()
        {
            // Возвращает первую незавершенную задачу в списке
            return activeObjectives.FirstOrDefault(o => !o.isCompleted);
        }
    }
}