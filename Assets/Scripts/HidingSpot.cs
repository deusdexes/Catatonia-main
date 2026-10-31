using UnityEngine;

public class HidingSpot : MonoBehaviour
{
    public bool wasRecentlyChecked = false;
    private bool isOccupied = false;

    public enum HidingType
    {
        Underneath, // Player goes underneath (e.g., bed, bench)
        Cabinet,    // Player stands upright inside (e.g., wardrobe, locker)
        Chest       // Player crouches inside (e.g., crate, trunk)
    }

    [Header("Hiding Spot Type")]
    public HidingType type = HidingType.Cabinet;

    [Tooltip("Точка, к которой подойдет монстр для взаимодействия с укрытием.")]
    public Transform monsterInteractionPoint;

    [Tooltip("Local offset from the hiding spot's center for the player's position when hiding.")]
    public Vector3 hidingPositionOffset = Vector3.zero;

    [Tooltip("Local Y rotation offset for the player when hiding.")]
    public float hidingYRotationOffset = 0f; // E.g., 180 for facing backwards

    [Header("Door Settings (for Cabinet type)")]
    [Tooltip("Reference to the door GameObject if this is a Cabinet type hiding spot.")]
    public Transform cabinetDoor;
    [Tooltip("The local Z-axis rotation for the door when it's closed.")]
    public float doorClosedLocalRotationZ = 0f; // Начальный угол двери в закрытом состоянии
    [Tooltip("The local Z-axis rotation for the door when it's open.")]
    public float doorOpenLocalRotationZ = 90f; // Угол, на который дверь поворачивается при открытии
    [Tooltip("Speed at which the door opens and closes.")]
    public float doorMoveSpeed = 5f;

    private Quaternion doorClosedLocalRotation;
    private Quaternion doorOpenLocalRotation;
    private bool isDoorTargetOpen = true;

    [Header("Audio Settings")]
    [Tooltip("Audio clip to play when the player enters the hiding spot.")]
    public AudioClip hideSound;
    [Tooltip("Audio clip to play when the player exits the hiding spot.")]
    public AudioClip releaseSound;

    private AudioSource audioSource;

    [Header("Exit Settings")]
    [Tooltip("Local offset from the hiding spot's center for the player's position when exiting.")]
    public Vector3 exitPositionOffset = Vector3.forward * 0.5f; // Default: 0.5m forward from the spot's center
    [Tooltip("Local Y rotation offset for the player when exiting.")]
    public float exitYRotationOffset = 0f;

    [Header("View Settings")]
    public float viewHeight = 1.6f;
    [Tooltip("Override camera eye height when hiding. Set to 0 to use type-based calculation.")]
    public float hidingCameraEyeHeightOverride = 0f;


    [Header("Trigger Settings")]
    public float triggerSizeMultiplier = 1.5f;

    [Header("Check Cooldown")]
    public float checkCooldown = 15f;

    private Collider physicalCollider;
    private Collider triggerCollider;
    private float lastCheckTime = 0f;


    void Start()
    {
        SetupColliders();
        SetupDoor();
        SetupAudioSource();
        if (monsterInteractionPoint == null)
        {
            GameObject interactionPointObj = new GameObject(name + "_InteractionPoint");
            interactionPointObj.transform.SetParent(transform);
            // Позиция по умолчанию - перед укрытием
            interactionPointObj.transform.position = transform.position + transform.forward * 1.5f;
            interactionPointObj.transform.rotation = Quaternion.LookRotation(transform.position - interactionPointObj.transform.position);
            monsterInteractionPoint = interactionPointObj.transform;
        }
    }

    void Update()
    {
        if (wasRecentlyChecked && Time.time - lastCheckTime > checkCooldown)
        {
            wasRecentlyChecked = false;
        }

        if (type == HidingType.Cabinet && cabinetDoor != null)
        {
            // Определяем целевое вращение на основе ОДНОЙ переменной
            Quaternion targetRotation = isDoorTargetOpen ? doorOpenLocalRotation : doorClosedLocalRotation;

            // Плавно анимируем дверь к цели
            cabinetDoor.localRotation = Quaternion.Slerp(cabinetDoor.localRotation, targetRotation, Time.deltaTime * doorMoveSpeed);
        }
    }

    void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            // Если AudioSource отсутствует, добавляем его
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            // Можно настроить другие параметры по умолчанию, например, объем 3D звука
            audioSource.spatialBlend = 1.0f;
        }
    }
    void SetupColliders()
    {
        Collider[] colliders = GetComponents<Collider>();

        foreach (Collider col in colliders)
        {
            if (!col.isTrigger)
            {
                physicalCollider = col;
            }
            else
            {
                triggerCollider = col;
            }
        }

        if (triggerCollider == null)
        {
            CreateTriggerCollider();
        }

        if (physicalCollider != null)
        {
            physicalCollider.isTrigger = false;
        }
    }

    void CreateTriggerCollider()
    {
        BoxCollider boxTrigger = gameObject.AddComponent<BoxCollider>();
        boxTrigger.isTrigger = true;

        if (physicalCollider != null)
        {
            if (physicalCollider is BoxCollider physicalBox)
            {
                boxTrigger.center = physicalBox.center;
                boxTrigger.size = physicalBox.size * triggerSizeMultiplier;
            }
            else if (physicalCollider is SphereCollider physicalSphere)
            {
                boxTrigger.center = physicalSphere.center;
                boxTrigger.size = Vector3.one * physicalSphere.radius * 2f * triggerSizeMultiplier;
            }
            else if (physicalCollider is CapsuleCollider physicalCapsule)
            {
                boxTrigger.center = physicalCapsule.center;
                boxTrigger.size = new Vector3(physicalCapsule.radius * 2f, physicalCapsule.height, physicalCapsule.radius * 2f) * triggerSizeMultiplier;
            }
        }
        else
        {
            boxTrigger.size = Vector3.one * 2f * triggerSizeMultiplier;
            boxTrigger.center = Vector3.zero;
        }

        triggerCollider = boxTrigger;
    }

    void SetupDoor()
    {
        if (type == HidingType.Cabinet && cabinetDoor != null)
        {
            doorClosedLocalRotation = Quaternion.Euler(cabinetDoor.localRotation.eulerAngles.x, cabinetDoor.localRotation.eulerAngles.y, doorClosedLocalRotationZ);
            doorOpenLocalRotation = Quaternion.Euler(cabinetDoor.localRotation.eulerAngles.x, cabinetDoor.localRotation.eulerAngles.y, doorOpenLocalRotationZ);

            isDoorTargetOpen = true;
            cabinetDoor.localRotation = doorOpenLocalRotation;
        }
    }

    public void MonsterOpenDoor()
    {
        if (type == HidingType.Cabinet)
        {
            isDoorTargetOpen = true;
            Debug.Log($"[HidingSpot] Monster is opening the door of {name}.");
        }
    }

    public void MonsterCloseDoor()
    {
        if (type == HidingType.Cabinet)
        {
            isDoorTargetOpen = false;
            Debug.Log($"[HidingSpot] Monster is closing the door of {name}.");
        }
    }

    public void MarkAsChecked()
    {
        wasRecentlyChecked = true;
        lastCheckTime = Time.time;
        Debug.Log($"Hiding spot {gameObject.name} marked as checked. Cooldown: {checkCooldown}s");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Monster"))
        {
            if (isOccupied)
            {
                PlayerController player = FindFirstObjectByType<PlayerController>();
                MonsterAI monster = other.GetComponent<MonsterAI>();

                if (player != null && player.isHiding && monster != null)
                {
                    Debug.Log("Player caught while hiding!");
                    monster.CatchPlayer();
                }
            }
        }
    }

    public void HidePlayer(PlayerController player)
    {
        isOccupied = true;
        player.isHiding = true;
        Debug.Log("Player hidden in " + gameObject.name);
        if (hideSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hideSound);
        }
        if (type == HidingType.Cabinet)
        {
            isDoorTargetOpen = false; // Даем команду закрыться
        }
    }

    public void ReleasePlayer(PlayerController player)
    {
        isOccupied = false;
        player.isHiding = false;
        Debug.Log("Player left hiding spot");
        if (releaseSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(releaseSound);
        }
        if (type == HidingType.Cabinet)
        {
            isDoorTargetOpen = true; // Даем команду открыться
        }
    }

    public float GetViewHeight()
    {
        if (viewHeight > 0) return viewHeight;

        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            float autoHeight = collider.bounds.size.y * 0.7f;
            return Mathf.Min(autoHeight, 1.6f);
        }

        return 1.6f;
    }

    void OnDrawGizmos()
    {
        // Draw the physical collider in blue
        Collider physCol = physicalCollider != null ? physicalCollider : GetComponent<Collider>();
        if (physCol != null && !physCol.isTrigger)
        {
            Gizmos.color = Color.blue;
            DrawColliderGizmo(physCol);
        }

        // Draw the trigger collider in green
        Collider trigCol = triggerCollider != null ? triggerCollider : GetComponent<Collider>();
        if (trigCol != null && trigCol.isTrigger)
        {
            Gizmos.color = Color.green;
            DrawColliderGizmo(trigCol);
        }

        // Draw hiding position point in yellow
        if (type != HidingType.Cabinet || hidingPositionOffset != Vector3.zero || hidingYRotationOffset != 0f || hidingCameraEyeHeightOverride != 0f)
        {
            Gizmos.color = Color.yellow;
            Vector3 hidingWorldPosition = transform.TransformPoint(hidingPositionOffset);
            Gizmos.DrawWireSphere(hidingWorldPosition, 0.2f);
            Quaternion hidingWorldRotation = transform.rotation * Quaternion.Euler(0, hidingYRotationOffset, 0);
            Vector3 forward = hidingWorldRotation * Vector3.forward;
            Gizmos.DrawLine(hidingWorldPosition, hidingWorldPosition + forward * 0.5f);
            Gizmos.DrawSphere(hidingWorldPosition + forward * 0.5f, 0.05f);
        }

        // Draw exit position point in magenta
        Gizmos.color = Color.magenta;
        Vector3 exitWorldPosition = transform.TransformPoint(exitPositionOffset);
        Gizmos.DrawWireSphere(exitWorldPosition, 0.2f);
        Quaternion exitWorldRotation = transform.rotation * Quaternion.Euler(0, exitYRotationOffset, 0);
        Vector3 exitForward = exitWorldRotation * Vector3.forward;
        Gizmos.DrawLine(exitWorldPosition, exitWorldPosition + exitForward * 0.5f);
        Gizmos.DrawSphere(exitWorldPosition + exitForward * 0.5f, 0.05f);

        if (type == HidingType.Cabinet && cabinetDoor != null)
        {
            Gizmos.color = Color.cyan;
            // Draw a line indicating the closed position
            Quaternion closedRot = Quaternion.Euler(cabinetDoor.localRotation.eulerAngles.x, cabinetDoor.localRotation.eulerAngles.y, doorClosedLocalRotationZ);
            Vector3 closedPos = cabinetDoor.position;
            Gizmos.DrawLine(closedPos, closedPos + (closedRot * Vector3.forward) * 0.3f);
            Gizmos.DrawSphere(closedPos + (closedRot * Vector3.forward) * 0.3f, 0.03f);

            // Draw a line indicating the open position
            Gizmos.color = Color.yellow;
            Quaternion openRot = Quaternion.Euler(cabinetDoor.localRotation.eulerAngles.x, cabinetDoor.localRotation.eulerAngles.y, doorOpenLocalRotationZ);
            Vector3 openPos = cabinetDoor.position;
            Gizmos.DrawLine(openPos, openPos + (openRot * Vector3.forward) * 0.3f);
            Gizmos.DrawSphere(openPos + (openRot * Vector3.forward) * 0.3f, 0.03f);
        }
        if (monsterInteractionPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(monsterInteractionPoint.position, 0.25f);
            Gizmos.DrawLine(monsterInteractionPoint.position, monsterInteractionPoint.position + monsterInteractionPoint.forward * 1f);
        }
    }

    void DrawColliderGizmo(Collider collider)
    {
        if (collider == null) return;

        Matrix4x4 originalMatrix = Gizmos.matrix;

        if (collider is BoxCollider box)
        {
            Gizmos.matrix = Matrix4x4.TRS(
                transform.TransformPoint(box.center),
                transform.rotation,
                transform.lossyScale
            );
            Gizmos.DrawWireCube(Vector3.zero, box.size);
        }
        else if (collider is SphereCollider sphere)
        {
            Vector3 center = transform.TransformPoint(sphere.center);
            float radius = sphere.radius * Mathf.Max(
                transform.lossyScale.x,
                transform.lossyScale.y,
                transform.lossyScale.z
            );
            Gizmos.DrawWireSphere(center, radius);
        }
        else if (collider is CapsuleCollider capsule)
        {
            Bounds bounds = collider.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        Gizmos.matrix = originalMatrix;
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Collider physCol = physicalCollider != null ? physicalCollider : GetComponent<Collider>();
        if (physCol != null && !physCol.isTrigger)
        {
            DrawColliderGizmo(physCol);
        }
    }
}