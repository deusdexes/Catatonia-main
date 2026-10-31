using UnityEngine;

public class KeycardItem : Item
{
    [Header("Keycard Specifics")]
    public KeycardData keycardData; // Ссылка на ScriptableObject с данными ключ-карты
    [Tooltip("Если 3D-модель или спрайт на дочернем объекте, перетащите его Renderer сюда.")]
    public Renderer itemRenderer; // НОВОЕ ПОЛЕ

    void Awake()
    {
        // Присваиваем свойства из ScriptableObject, если они заданы
        if (keycardData != null)
        {
            itemName = keycardData.keycardName;
            itemIcon = keycardData.keycardIcon;
            itemDescription = keycardData.description;
        }
        // Если itemRenderer не назначен, попробуем найти его на этом GameObject
        if (itemRenderer == null)
        {
            itemRenderer = GetComponent<Renderer>();
        }
        // Если все еще null, ищите на дочерних объектах (но это менее эффективно, лучше назначать вручную)
        if (itemRenderer == null)
        {
            itemRenderer = GetComponentInChildren<Renderer>();
        }
    }

    public override void Use()
    {
        Debug.Log($"Keycard {itemName} is being examined. ID: {keycardData?.keycardID}");

    }

    public override void StopUse()
    {
        Debug.Log($"Stopped examining keycard {itemName}.");
    }

    public override void OnEquip(Transform hand)
    {
        base.OnEquip(hand);

        if (itemRenderer != null)
        {
            itemRenderer.enabled = true; // Включаем рендерер, чтобы ключ-карта была видна
            Debug.Log($"KeycardItem: Renderer '{itemRenderer.name}' включен при экипировке.");
        }
        else
        {
            Debug.LogWarning($"KeycardItem: Нет назначенного Renderer для включения на '{gameObject.name}' при экипировке.");
        }

        // Устанавливаем положение и поворот предмета относительно руки
        transform.SetParent(hand);
        transform.localPosition = Vector3.zero; // Или настройте нужную позицию
        transform.localRotation = Quaternion.identity; // Или настройте нужный поворот
        transform.localScale = Vector3.one; // Или настройте нужный размер

        // Также можно включить коллайдер, если он нужен для взаимодействия в руке (например, для физики)
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true; // Включаем коллайдер
            Debug.Log($"KeycardItem: Collider включен при экипировке на '{gameObject.name}'.");
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Делаем кинематическим, чтобы он не выпадал из руки
            rb.useGravity = false;
        }
    }

    public override void OnPickup()
    {
        base.OnPickup();
        Debug.Log($"KeycardItem: OnPickup вызван для '{itemName}'. Отключение рендерера и коллайдера на GameObject '{gameObject.name}'.");

        // При подборе в инвентарь (НЕ экипировке), мы хотим, чтобы она исчезла из мира
        if (itemRenderer != null)
        {
            itemRenderer.enabled = false; // Отключаем рендерер
            Debug.Log($"KeycardItem: Renderer '{itemRenderer.name}' отключен при подборе.");
        }
        else
        {
            Debug.LogWarning($"KeycardItem: Нет назначенного Renderer для отключения на '{gameObject.name}' или его дочерних объектах.");
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false; // Отключаем коллайдер
            Debug.Log($"KeycardItem: Collider отключен при подборе на '{gameObject.name}'.");
        }
        else
        {
            Debug.LogWarning($"KeycardItem: Нет Collider на '{gameObject.name}' для отключения.");
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            Debug.Log($"KeycardItem: Rigidbody настроен на kinematic для '{gameObject.name}'.");
        }
        else
        {
            Debug.LogWarning($"KeycardItem: Нет Rigidbody на '{gameObject.name}'.");
        }
    }

    public override void OnDrop()
    {
        base.OnDrop();
        // При выбрасывании включаем рендерер и коллайдер, чтобы предмет снова был видим в мире
        // Очищаем родителя, чтобы предмет выпал
        transform.SetParent(null); // Важно!
        Debug.Log($"KeycardItem: Отменена привязка к родителю при выбрасывании '{gameObject.name}'.");

        if (itemRenderer != null)
        {
            itemRenderer.enabled = true;
            Debug.Log($"KeycardItem: Renderer '{itemRenderer.name}' включен при выбрасывании.");
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
            Debug.Log($"KeycardItem: Collider включен при выбрасывании на '{gameObject.name}'.");
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            Debug.Log($"KeycardItem: Rigidbody настроен на не-kinematic и гравитацию для '{gameObject.name}'.");
        }
    }
}