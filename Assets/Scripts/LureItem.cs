using UnityEngine;

public class LureItem : Item
{
    [Header("Lure Settings")]
    public float throwForce = 15f; // Сила броска
    public float soundRadius = 20f; // Радиус, в котором монстр услышит приманку
    public AudioClip breakSound; // Звук разбития приманки
    public LayerMask impactLayers; // Слои, при столкновении с которыми приманка разбивается (например, Default, Environment)

    private Rigidbody rb;
    private AudioSource audioSource;
    public bool IsThrown { get; private set; } = false; // Сделано публичным свойством с приватным сеттером

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("LureItem requires a Rigidbody component!");
            enabled = false;
            return;
        }
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.maxDistance = soundRadius;
            audioSource.playOnAwake = false;
        }

        rb.isKinematic = true;
        rb.useGravity = false;
        // Коллайдер будет управляться скриптом ItemPickup для возможности подбора
    }

    public override void OnEquip(Transform hand)
    {
        base.OnEquip(hand);
        rb.isKinematic = true;
        rb.useGravity = false;
        GetComponent<Collider>().enabled = false;
        IsThrown = false;
        Debug.Log($"Equipped {itemName}. Rigidbody kinematic: {rb.isKinematic}");
    }

    public override void OnPickup()
    {
        base.OnPickup();
        rb.isKinematic = true;
        rb.useGravity = false;
        GetComponent<Collider>().enabled = false;
        gameObject.SetActive(false);
        IsThrown = false;
    }

    public override void OnDrop()
    {
        base.OnDrop();
        rb.isKinematic = false;
        rb.useGravity = true;
        GetComponent<Collider>().enabled = true;
        IsThrown = false;
        Debug.Log($"Dropped {itemName}. Rigidbody kinematic: {rb.isKinematic}");
    }

    public override void Use()
    {
        if (IsThrown) return; // Предотвращаем многократный бросок

        Camera playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError("LureItem.Use: No main camera found for throwing direction!");
            return;
        }

        // 1. Помечаем предмет как брошенный
        IsThrown = true;

        // 2. Отсоединяем от родителя (руки)
        transform.SetParent(null);


        Vector3 throwStartOffset = playerCamera.transform.forward * 0.3f; // 30 см вперед от камеры
        Vector3 throwStartPos = playerCamera.transform.position + throwStartOffset;


        transform.position = throwStartPos;
        // Также ориентируем предмет по направлению камеры для более естественного броска
        transform.rotation = playerCamera.transform.rotation;

        // 4. Активируем физические компоненты
        rb.isKinematic = false;
        rb.useGravity = true;
        GetComponent<Collider>().enabled = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Для быстрых объектов

        // 5. Применяем силу броска
        Vector3 throwDirection = playerCamera.transform.forward;
        rb.AddForce(throwDirection * throwForce, ForceMode.Impulse);

        Debug.Log($"Lure {itemName} thrown with force {throwForce} from initial position {throwStartPos}.");
    }


    public override void StopUse()
    {
        if (!IsThrown)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            GetComponent<Collider>().enabled = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!IsThrown) return;

        if (((1 << collision.gameObject.layer) & impactLayers) != 0)
        {
            Debug.Log($"Lure '{itemName}' hit {collision.gameObject.name} on layer {LayerMask.LayerToName(collision.gameObject.layer)}. Breaking!");

            if (breakSound != null)
            {
                AudioSource.PlayClipAtPoint(breakSound, collision.contacts[0].point, 1f);
            }

            MonsterAI monster = FindFirstObjectByType<MonsterAI>();
            if (monster != null)
            {
                monster.ReactToLure(collision.contacts[0].point, soundRadius);
            }

            gameObject.SetActive(false);
            IsThrown = false;
        }
    }
}