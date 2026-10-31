using UnityEngine;
using System.Collections; // Для корутин
using UnityEditor;

public class SecurityCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    public float visionRange = 15f; // Дальность обзора
    public float visionAngle = 70f; // Угол обзора (общий)
    public LayerMask visionLayerMask; // Маска слоев, которые камера может видеть (например, "Player", "Environment")
    public Light indicatorLight; // Ссылка на компонент Light для лампочки
    public Renderer indicatorRenderer; // Или ссылка на Renderer, если лампочка - это объект с материалом

    [Header("Detection Settings")]
    public float detectionTime = 1f; // Время, через которое желтый сигнал становится красным
    public float alertDuration = 10f; // Время, в течение которого камера остается в красном состоянии
    public float cooldownTime = 3f; // Время, через которое желтый/красный сигнал сбрасывается обратно в зеленый

    private PlayerController player;
    private MonsterAI monster;

    private enum CameraState { Green, Yellow, Red, Cooldown }
    private CameraState currentState = CameraState.Green;
    private float currentDetectionTimer = 0f;
    private float currentAlertTimer = 0f;
    private float currentCooldownTimer = 0f;

    private Vector3 lastKnownPlayerPosition; // Место, где игрок был замечен для MonsterAI

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        monster = FindFirstObjectByType<MonsterAI>();

        if (player == null)
        {
            Debug.LogError("SecurityCamera: PlayerController not found in scene!");
        }
        if (monster == null)
        {
            Debug.LogError("SecurityCamera: MonsterAI not found in scene!");
        }

        SetCameraState(CameraState.Green); // Начальное состояние
    }

    void Update()
    {
        bool playerInVision = CheckForPlayer();

        switch (currentState)
        {
            case CameraState.Green:
                if (playerInVision)
                {
                    SetCameraState(CameraState.Yellow);
                    currentDetectionTimer = detectionTime;
                    lastKnownPlayerPosition = player.transform.position;
                    Debug.Log("Player detected, switching to YELLOW state.");
                }
                break;

            case CameraState.Yellow:
                if (playerInVision)
                {
                    currentDetectionTimer -= Time.deltaTime;
                    lastKnownPlayerPosition = player.transform.position; // Обновляем позицию, пока желтый
                    if (currentDetectionTimer <= 0f)
                    {
                        SetCameraState(CameraState.Red);
                        currentAlertTimer = alertDuration;
                        TriggerAlarm(); // Активируем тревогу
                        Debug.Log("Detection confirmed, switching to RED state and triggering alarm!");
                    }
                }
                else
                {
                    SetCameraState(CameraState.Cooldown);
                    currentCooldownTimer = cooldownTime;
                    Debug.Log("Player left vision during YELLOW state, starting cooldown.");
                }
                break;

            case CameraState.Red:
                if (playerInVision)
                {
                    currentAlertTimer = alertDuration; // Сбрасываем таймер тревоги, пока игрок в поле зрения
                    lastKnownPlayerPosition = player.transform.position; // Обновляем позицию, пока красный
                }
                else
                {
                    currentAlertTimer -= Time.deltaTime;
                    if (currentAlertTimer <= 0f)
                    {
                        SetCameraState(CameraState.Cooldown);
                        currentCooldownTimer = cooldownTime;
                        Debug.Log("Alarm duration expired, switching to Cooldown.");
                    }
                }
                break;

            case CameraState.Cooldown:
                currentCooldownTimer -= Time.deltaTime;
                if (currentCooldownTimer <= 0f)
                {
                    SetCameraState(CameraState.Green);
                    Debug.Log("Cooldown finished, returning to GREEN state.");
                }
                // Если игрок возвращается в поле зрения во время Cooldown, начинаем заново
                if (playerInVision)
                {
                    SetCameraState(CameraState.Yellow);
                    currentDetectionTimer = detectionTime;
                    lastKnownPlayerPosition = player.transform.position;
                    Debug.Log("Player re-detected during Cooldown, switching to YELLOW state.");
                }
                break;
        }
    }

    bool CheckForPlayer()
    {
        if (player == null) return false;

        Vector3 origin = transform.position;
        Vector3 directionToPlayer = (player.transform.position - origin).normalized;
        float distanceToPlayer = Vector3.Distance(origin, player.transform.position);

        // Проверяем дальность
        if (distanceToPlayer > visionRange)
        {
            return false;
        }

        // Проверяем угол обзора
        if (Vector3.Angle(transform.forward, directionToPlayer) > visionAngle / 2f)
        {
            return false;
        }

        // Проверяем наличие препятствий между камерой и игроком
        RaycastHit hit;
        if (Physics.Raycast(origin, directionToPlayer, out hit, visionRange, visionLayerMask))
        {
            if (hit.collider.CompareTag("Player"))
            {
                return true;
            }
        }
        return false;
    }

    void SetLightColor(Color color)
    {
        if (indicatorLight != null)
        {
            indicatorLight.color = color;
        }
        if (indicatorRenderer != null)
        {
            // Предполагаем, что материал имеет "_Color" свойство
            indicatorRenderer.material.color = color;

        }
    }

    void SetCameraState(CameraState newState)
    {
        currentState = newState;
        switch (newState)
        {
            case CameraState.Green:
                SetLightColor(Color.green);
                break;
            case CameraState.Yellow:
                SetLightColor(Color.yellow);
                break;
            case CameraState.Red:
                SetLightColor(Color.red);
                break;
            case CameraState.Cooldown:
                // Можно использовать мигающий цвет или просто серый/отключенный
                SetLightColor(Color.grey);
                break;
        }
    }

    void TriggerAlarm()
    {
        if (monster != null && player != null)
        {
            monster.ReactToCameraAlarm(lastKnownPlayerPosition, 0.5f);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Устанавливаем цвет для гизмо
        Color gizmoColor = Color.yellow;
        gizmoColor.a = 0.25f; // Делаем его полупрозрачным
        Handles.color = gizmoColor;

        // Рисуем залитый сектор обзора
        Vector3 startDirection = Quaternion.AngleAxis(-visionAngle / 2, transform.up) * transform.forward;
        Handles.DrawSolidArc(transform.position, transform.up, startDirection, visionAngle, visionRange);

        // Рисуем контур сектора для лучшей видимости
        Handles.color = Color.yellow;
        Handles.DrawWireArc(transform.position, transform.up, startDirection, visionAngle, visionRange);
    }
#endif
}
