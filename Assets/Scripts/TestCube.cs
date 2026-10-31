using UnityEngine;

public class TestCube : Item
{
    [Header("Cube Settings")]
    public float rotationSpeed = 90f;
    private bool isRotating = false;
    private Rigidbody rb;

    // Сохраняем оригинальные трансформации
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private Transform originalParent;
    private ItemPickup itemPickup; // Добавляем ссылку на ItemPickup

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        itemPickup = GetComponent<ItemPickup>(); // Получаем компонент ItemPickup

        // Убедимся, что есть Rigidbody для физики
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
    }

    public override void Use()
    {
        // Включаем вращение кубика
        isRotating = true;
        Debug.Log("Using test cube - it's now rotating!");
    }

    public override void StopUse()
    {
        // Выключаем вращение
        isRotating = false;
        Debug.Log("Stopped using test cube");
    }

    public override void OnEquip(Transform hand)
    {
        // Настраиваем позицию и вращение в руке
        transform.SetParent(hand);
        transform.localPosition = new Vector3(0.2f, -0.1f, 0.4f);
        transform.localEulerAngles = new Vector3(0, 45, 0);
        transform.localScale = Vector3.one * 0.2f;

        // Отключаем физику когда предмет в руке
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // Включаем рендерер
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
        }

        Debug.Log("Cube equipped in hand");
    }

    public override void OnPickup()
    {
        // Сохраняем оригинальные трансформации
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalScale = transform.localScale;
        originalParent = transform.parent;

        // Отключаем физику
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // Отключаем коллайдер
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        // Отключаем рендерер (делаем невидимым)
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }

        Debug.Log($"Picked up test cube: {itemName}. Saved position: {originalPosition}");
    }

    public override void OnDrop()
    {
        // Сбрасываем родителя
        transform.SetParent(originalParent);

        // Восстанавливаем оригинальные трансформации
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        transform.localScale = originalScale;

        // Включаем физику
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        // Включаем коллайдер
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = true;
        }

        // Включаем рендерер
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
        }

        // СБРАСЫВАЕМ СОСТОЯНИЕ ПОДБОРА В ITEMPICKUP
        if (itemPickup != null)
        {
            itemPickup.ResetPickup();
        }

        StopUse();
        Debug.Log($"Dropped test cube: {itemName} at original position: {originalPosition}, scale: {originalScale}");
    }

    void Update()
    {
        if (isRotating)
        {
            // Вращаем кубик когда он используется
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            transform.Rotate(Vector3.right, rotationSpeed * 0.7f * Time.deltaTime);
        }
    }
}