using UnityEngine;
using GameSystem;

public class RobotPartItem : Item
{
    [Header("Robot Part Settings")]
    public string partIdentifier; // Например, "LeftArm", "Head"

    [Tooltip("Масштаб, который будет применен к предмету, когда он будет в руке.")]
    public float equippedScale = 0.15f; // Настройте это значение в Инспекторе

    private Rigidbody rb;
    private ItemPickup itemPickup; // Ссылка на компонент ItemPickup на ЭТОМ объекте

    // Сохраняем оригинальные трансформации для восстановления при выбрасывании
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private Transform originalParent;

    [Header("Quest Integration")] // Новый заголовок
    [Tooltip("Ссылка на CreatureController в сцене.")]
    public CreatureController creatureController; // Ссылка на контроллер монстра
    [Tooltip("Конфигурация сокращения таймера при подборе этой части.")]
    public TimerReductionConfig pickupReductionConfig; // Конфигурация для подбора

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        itemPickup = GetComponent<ItemPickup>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;

        // Попытка найти CreatureController, если не назначен вручную
        if (creatureController == null)
        {
            creatureController = FindAnyObjectByType<CreatureController>();
            if (creatureController == null)
            {
                Debug.LogWarning($"RobotPartItem: CreatureController не назначен и не найден для {itemName}. Сокращение таймера при подборе работать не будет.");
            }
        }
        if (pickupReductionConfig == null)
        {
            Debug.LogWarning($"RobotPartItem: Pickup Reduction Config не назначен для {itemName}. Сокращение таймера при подборе не будет настроено.");
        }
    }

    public override void Use()
    {
        Debug.Log($"Используется часть робота: {itemName}. Особое действие пока не определено.");
    }

    public override void StopUse()
    {
        Debug.Log($"Прекращено использование части робота: {itemName}.");
    }

    // Логика, когда предмет экипирован в руку игрока
    public override void OnEquip(Transform hand)
    {

        base.OnEquip(hand);


        transform.localScale = Vector3.one * equippedScale;

        // Отключаем физику, когда предмет в руке
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Включаем рендерер, чтобы предмет был виден в руке
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
        }

        // Отключаем коллайдер, чтобы не было случайных взаимодействий
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        Debug.Log($"{itemName} экипирована в руку с масштабом: {transform.localScale}.");
    }

    public override void OnPickup()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalScale = transform.localScale;
        originalParent = transform.parent;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }

        gameObject.SetActive(false);

        Debug.Log($"Подобрана часть робота: {itemName} ({partIdentifier}). Теперь она скрыта.");

        if (QuestManager.Instance != null)
        {
            // ID будет "PICKUP_ПраваяРука"
            string objectiveID = "PICKUP_" + partIdentifier.Replace(" ", "").ToUpper();

            // Сообщаем менеджеру о выполнении под-цели
            QuestManager.Instance.CompleteObjective(objectiveID);
        }

        if (creatureController != null && pickupReductionConfig != null)
        {
            float reductionAmount = Random.Range(pickupReductionConfig.minReduction, pickupReductionConfig.maxReduction);
            creatureController.ReduceTimer(reductionAmount);
        }
        else if (creatureController == null)
        {
            Debug.LogWarning($"CreatureController не назначен для {itemName}, таймер монстра не сокращен при подборе.");
        }
        else if (pickupReductionConfig == null)
        {
            Debug.LogWarning($"Pickup Reduction Config не назначен для {itemName}, таймер монстра не сокращен при подборе.");
        }
    }


    public override void OnDrop()
    {
        gameObject.SetActive(true);

        transform.SetParent(originalParent);
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        transform.localScale = originalScale; // Восстанавливаем оригинальный масштаб

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = true;
        }

        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
        }

        if (itemPickup != null)
        {
            itemPickup.ResetPickup();
        }
        StopUse();

        Debug.Log($"Выброшена часть робота: {itemName} ({partIdentifier}). Восстановлено исходное состояние.");
    }
}