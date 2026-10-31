using UnityEngine;

public class QuestDependentDoor : SlidingDoor
{
    public enum QuestDependency
    {
        GeneratorOn,        // Требует включения генератора
        CartQuestCompleted, // Требует завершения квеста с тележкой
        Custom              // Пользовательская зависимость (для расширения)
    }

    [Header("Quest Dependency Settings")]
    public QuestDependency dependencyType = QuestDependency.GeneratorOn;

    [Tooltip("Ссылка на GeneratorLever, если dependencyType = GeneratorOn")]
    public GeneratorLever requiredGenerator;

    [Tooltip("Ссылка на CartController, если dependencyType = CartQuestCompleted")]
    public CartController requiredCart;

    // Включать ли звук при первом открытии по квесту (по умолчанию false, так как OpenDoor() уже играет звук)
    public bool playOpenSoundOnQuestComplete = false;

    protected override void Start()
    {
        base.Start();

        // Проверяем и устанавливаем необходимые ссылки
        if (dependencyType == QuestDependency.GeneratorOn && requiredGenerator == null)
        {
            requiredGenerator = FindAnyObjectByType<GeneratorLever>();
            if (requiredGenerator == null)
            {
                Debug.LogError("QuestDependentDoor (GeneratorOn): GeneratorLever не найден/не назначен! Дверь не будет работать.", this);
            }
        }
        else if (dependencyType == QuestDependency.CartQuestCompleted && requiredCart == null)
        {
            requiredCart = FindAnyObjectByType<CartController>();
            if (requiredCart == null)
            {
                Debug.LogError("QuestDependentDoor (CartQuestCompleted): CartController не найден/не назначен! Дверь не будет работать.", this);
            }
        }

        // При старте дверь должна быть в состоянии "закрыто",
        // и внешний контроль должен быть установлен, чтобы триггеры игрока не мешали.
        SetOpenCondition(false, true);
    }

    protected new void Update()
    {
        base.Update(); // Вызываем Update базового класса для плавного движения

        bool shouldBeOpen = CheckQuestCondition();

        // Проверяем, изменилось ли условие открытия
        if (shouldBeOpen && !isExternalyForcedOpen)
        {
            Debug.Log($"QuestDependentDoor: Условие '{dependencyType}' выполнено! Открытие двери.");
            SetOpenCondition(true, true);
        }
        else if (!shouldBeOpen && isExternalyForcedOpen)
        {
            // Если условие может быть отменено (например, выключили генератор)
            Debug.Log($"QuestDependentDoor: Условие '{dependencyType}' отменено. Закрытие двери.");
            SetOpenCondition(false, true);
        }
    }

    /// <summary>
    /// Проверяет, выполнено ли условие квеста для открытия двери.
    /// </summary>
    private bool CheckQuestCondition()
    {
        switch (dependencyType)
        {
            case QuestDependency.GeneratorOn:
                return requiredGenerator != null && requiredGenerator.isGeneratorOn;

            case QuestDependency.CartQuestCompleted:
                return requiredCart != null && requiredCart.IsQuestCompletedPublic;

            case QuestDependency.Custom:
                // Здесь будет логика для пользовательского условия
                return false;

            default:
                return false;
        }
    }
}