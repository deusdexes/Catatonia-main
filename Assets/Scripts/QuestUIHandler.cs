using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Для LINQ
using GameSystem;
using TMPro;
using UnityEngine.UI;

public class QuestUIHandler : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Контейнер, в который будут добавляться маркеры квестов.")]
    public RectTransform questListContainer;
    [Tooltip("Префаб ОДНОЙ СТРОКИ маркера квеста.")]
    public GameObject questMarkerPrefab;

    [Header("Animation Settings")]
    public float fadeInTime = 0.5f;
    public float fadeOutTime = 0.3f;
    public float completedDuration = 1.0f;
    public Color completedColor = new Color(0.6f, 0.6f, 0.6f, 1f); // Серый цвет
    public float subObjectiveIndent = 20f; // Отступ для под-целей

    // --- Внутренние Классы ---
    private class UIMarkerBase
    {
        public QuestObjective objectiveData; // Ссылка на первую задачу в группе
        public GameObject rootObject;        // Корень, который исчезает (контейнер или маркер)
        public CanvasGroup canvasGroup;
    }

    private UIMarkerBase currentActiveMarker;
    private Coroutine activeAnimationRoutine;

    private void Awake()
    {
        // QuestManager.Instance уже будет доступен, т.к. его Awake тоже выполнился
        if (QuestManager.Instance != null)
        {
            // Подписываемся на события здесь, ДО того, как QuestManager вызовет их в своем Start()
            QuestManager.Instance.OnObjectiveActivated += HandleObjectiveActivated;
            QuestManager.Instance.OnObjectiveCompleted += HandleObjectiveCompleted;
        }
        else
        {
            Debug.LogError("Критическая ошибка: QuestManager.Instance не найден в Awake()! UI не будет работать.");
        }
    }

    void Start()
    {
        if (QuestManager.Instance == null || questMarkerPrefab == null || questListContainer == null)
        {
            Debug.LogError("QuestUIHandler: Не назначены все необходимые ссылки!");
            enabled = false;
            return;
        }

        // Проверяем, что QuestMarkerPrefab содержит необходимые компоненты
        if (questMarkerPrefab.GetComponentInChildren<TextMeshProUGUI>() == null)
        {
            Debug.LogError("QuestMarkerPrefab должен содержать TextMeshProUGUI в дочерних элементах!");
            enabled = false;
            return;
        }
        QuestObjective currentObjective = QuestManager.Instance.GetNextActiveObjective();
        if (currentObjective != null && currentActiveMarker == null) // Если есть квест, а UI еще не создан
        {
            Debug.Log("UI пропустил событие, запускаем отображение из Start()");
            HandleObjectiveActivated(currentObjective);
        }
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnObjectiveActivated -= HandleObjectiveActivated;
            QuestManager.Instance.OnObjectiveCompleted -= HandleObjectiveCompleted;
        }
    }

    // --- ОБРАБОТКА АКТИВАЦИИ (ЗАПУСК АНИМАЦИИ ПОЯВЛЕНИЯ) ---
    private void HandleObjectiveActivated(QuestObjective objective)
    {
        // Если уже есть активный маркер, мы должны его сначала убрать
        if (currentActiveMarker != null && activeAnimationRoutine != null)
        {

            return;
        }

        // Начинаем перестроение списка (или отображение одного маркера)
        activeAnimationRoutine = StartCoroutine(RebuildQuestList());
    }

    // --- ОБРАБОТКА ЗАВЕРШЕНИЯ (ЗАПУСК АНИМАЦИИ ИСЧЕЗНОВЕНИЯ/ОБНОВЛЕНИЯ) ---
    private void HandleObjectiveCompleted(QuestObjective completedObjective)
    {
        // 1. Проверяем, является ли завершенная цель частью активной группы
        if (currentActiveMarker != null && !string.IsNullOrEmpty(currentActiveMarker.objectiveData.groupID) && currentActiveMarker.objectiveData.groupID == completedObjective.groupID)
        {
            UpdateGroupUI(completedObjective.groupID);

            // Проверяем, завершена ли вся группа (Все ли цели с этим GroupID завершены?)
            bool allGroupObjectivesCompleted = QuestManager.Instance.activeObjectives
                .Where(o => o.groupID == completedObjective.groupID)
                .All(o => o.isCompleted);

            if (allGroupObjectivesCompleted)
            {
                // Если да, то запускаем исчезновение всей группы
                activeAnimationRoutine = StartCoroutine(AnimateMarkerOut(currentActiveMarker));
            }
        }
        // 2. Если это простая цель, запускаем исчезновение
        else if (currentActiveMarker != null && string.IsNullOrEmpty(currentActiveMarker.objectiveData.groupID) && currentActiveMarker.objectiveData.objectiveID == completedObjective.objectiveID)
        {
            activeAnimationRoutine = StartCoroutine(AnimateMarkerOut(currentActiveMarker));
        }
    }



    // --- МЕТОДЫ ПОСТРОЕНИЯ UI И АНИМАЦИИ ---

    // Полностью перестраивает список, основываясь на текущем состоянии QuestManager
    IEnumerator RebuildQuestList()
    {
        // 1. Ожидаем завершения предыдущей анимации
        if (activeAnimationRoutine != null) yield return activeAnimationRoutine;

        // 2. Получаем список задач для отображения
        QuestObjective currentObjective = QuestManager.Instance.GetNextActiveObjective();

        if (currentObjective == null)
        {
            // Очистка контейнера, если квестов больше нет
            foreach (Transform child in questListContainer) Destroy(child.gameObject);
            yield break;
        }

        List<QuestObjective> objectivesToDisplay;

        if (string.IsNullOrEmpty(currentObjective.groupID))
        {
            // Если это ПРОСТОЙ квест, отображаем только его
            objectivesToDisplay = new List<QuestObjective> { currentObjective };
        }
        else
        {
            // Если это ГРУППОВОЙ квест, отображаем все квесты этой группы
            objectivesToDisplay = QuestManager.Instance.activeObjectives
                .Where(o => o.groupID == currentObjective.groupID)
                .ToList();
        }

        // 3. Создаем UI для всех задач в objectivesToDisplay
        activeAnimationRoutine = StartCoroutine(AnimateGroupIn(objectivesToDisplay));
    }

    // Создание и анимация появления группы (или одной цели)
    IEnumerator AnimateGroupIn(List<QuestObjective> objectives)
    {
        if (objectives.Count == 0) yield break;

        // Сначала очищаем контейнер
        foreach (Transform child in questListContainer) Destroy(child.gameObject);

        // 1. Создаем общий контейнер для всех элементов группы
        GameObject containerObj = new GameObject("QuestGroup_Container", typeof(RectTransform), typeof(CanvasGroup), typeof(VerticalLayoutGroup));
        containerObj.transform.SetParent(questListContainer, false);
        CanvasGroup canvasGroup = containerObj.GetComponent<CanvasGroup>();

        // Настройка Vertical Layout Group (для компактности)
        var vlg = containerObj.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(5, 5, 5, 5);
        vlg.spacing = 8;
        vlg.childAlignment = TextAnchor.UpperLeft;

        currentActiveMarker = new UIMarkerBase
        {
            objectiveData = objectives.FirstOrDefault(o => !o.isCompleted),
            rootObject = containerObj,
            canvasGroup = canvasGroup
        };

        // --- 2.1. Добавляем ЗАГОЛОВОК ГРУППЫ (один раз) ---
        string groupTitle = objectives.FirstOrDefault(o => !string.IsNullOrEmpty(o.groupTitle))?.groupTitle;
        if (!string.IsNullOrEmpty(groupTitle))
        {
            GameObject titleObj = Instantiate(questMarkerPrefab, containerObj.transform);
            TextMeshProUGUI textComponent = titleObj.GetComponentInChildren<TextMeshProUGUI>();
            Image iconComponent = titleObj.GetComponentInChildren<Image>();

            // Установка стилей для заголовка
            textComponent.text = groupTitle;
            textComponent.color = Color.white;
            textComponent.fontStyle = FontStyles.Bold;
            iconComponent.gameObject.SetActive(false); // Скрываем иконку

            // Настраиваем отступ для под-целей, которые будут идти ниже
            vlg.padding.left = (int)subObjectiveIndent;

            // Делаем заголовок невидимым для логики вычеркивания в UpdateGroupUI
            titleObj.tag = "QuestGroupTitle";
        }
        else
        {
            vlg.padding.left = 0; // Нет заголовка, нет отступа
        }

        // --- 2.2. Создаем маркеры для КАЖДОЙ ПОД-ЦЕЛИ ---
        // Нам нужны только те, у которых есть GroupID
        var subObjectives = objectives.Where(o => o.groupID == currentActiveMarker.objectiveData.groupID).ToList();

        foreach (var objective in subObjectives)
        {
            GameObject markerObj = Instantiate(questMarkerPrefab, containerObj.transform);
            TextMeshProUGUI textComponent = markerObj.GetComponentInChildren<TextMeshProUGUI>();
            Image iconComponent = markerObj.GetComponentInChildren<Image>();

            // Это всегда под-цель в этом блоке
            textComponent.fontStyle = FontStyles.Normal;

            textComponent.text = objective.displayName;
            iconComponent.gameObject.SetActive(objective.isCompleted);

            if (objective.isCompleted)
            {
                textComponent.text = "<s>" + objective.displayName + "</s>";
                textComponent.color = completedColor;
            }
            else
            {
                textComponent.color = Color.white;
            }
        }

        // 3. Анимация FadeIn
        float timer = 0f;
        canvasGroup.alpha = 0f;
        while (timer < fadeInTime)
        {
            float progress = timer / fadeInTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);
            timer += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1f;
        activeAnimationRoutine = null; // Анимация завершена
    }

    void UpdateGroupUI(string groupID)
    {
        if (currentActiveMarker == null || currentActiveMarker.rootObject == null) return;

        // 1. Находим все задачи этой группы
        var objectives = QuestManager.Instance.activeObjectives
            .Where(o => o.groupID == groupID)
            .ToList();

        // 2. Находим все текстовые компоненты, ИГНОРИРУЯ компонент заголовка
        var allTextComponents = currentActiveMarker.rootObject.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true)
            .Where(t => t.gameObject.tag != "QuestGroupTitle") // Игнорируем заголовок
            .ToList();

        // 3. Перебираем под-цели
        foreach (var textComponent in allTextComponents)
        {
            // Ищем соответствующую задачу по displayName (без тегов вычеркивания)
            string cleanedText = textComponent.text.Replace("<s>", "").Replace("</s>", "").Trim();
            var objective = objectives.FirstOrDefault(o => o.displayName == cleanedText);

            if (objective != null && objective.isCompleted)
            {
                // Найдена цель, которая должна быть вычеркнута
                textComponent.text = "<s>" + objective.displayName + "</s>";
                textComponent.color = completedColor;

                // Ищем Image (галочку)
                var iconComponent = textComponent.GetComponentInParent<Image>();
                if (iconComponent != null)
                {
                    iconComponent.gameObject.SetActive(true);
                }
            }
        }
    }

    // Анимация исчезновения
    IEnumerator AnimateMarkerOut(UIMarkerBase marker)
    {
        if (marker == null) yield break;

        // 1. Вычеркивание и пауза
        // Если это группа, она уже обновлена. Если это одиночная цель, вычеркиваем ее сейчас.
        if (string.IsNullOrEmpty(marker.objectiveData.groupID))
        {
            var textComponent = marker.rootObject.GetComponentInChildren<TextMeshProUGUI>();
            var iconComponent = marker.rootObject.GetComponentInChildren<Image>();

            textComponent.text = "<s>" + marker.objectiveData.displayName + "</s>";
            textComponent.color = completedColor;
            if (iconComponent != null) iconComponent.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(completedDuration);

        // 2. Анимация: Плавное и быстрое исчезновение сверху вниз
        float timer = 0f;
        RectTransform rectTransform = marker.rootObject.GetComponent<RectTransform>();
        float initialHeight = rectTransform.sizeDelta.y;

        while (timer < fadeOutTime)
        {
            float progress = timer / fadeOutTime;

            // Плавное уменьшение прозрачности
            marker.canvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);

            // Уменьшение высоты (создает эффект "схлопывания" сверху-вниз)
            float currentHeight = Mathf.Lerp(initialHeight, 0f, progress);
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, currentHeight);

            timer += Time.deltaTime;
            LayoutRebuilder.ForceRebuildLayoutImmediate(questListContainer);
            yield return null;
        }

        // 3. Очистка
        Destroy(marker.rootObject);
        currentActiveMarker = null;

        // 4. Активация следующей задачи!
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.ActivateNextObjectiveIfAvailable();
        }
        activeAnimationRoutine = null;
    }
}