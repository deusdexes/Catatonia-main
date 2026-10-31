using UnityEngine;
using UnityEngine.AI; // Добавляем, так как робот использует NavMeshAgent

public class RobotAnimationController : MonoBehaviour
{
    // Ссылка на компонент Animator
    [Header("Component References")]
    [Tooltip("Перетащите сюда компонент Animator робота")]
    [SerializeField] private Animator animator;

    // Ссылка на компонент NavMeshAgent (опционально, если скорость будет передаваться напрямую)
    [Tooltip("Перетащите сюда компонент NavMeshAgent этого робота (опционально)")]
    [SerializeField] private NavMeshAgent navMeshAgent;

    [Header("Animation Parameter Names")]
    [Tooltip("Имя Float параметра скорости в Animator Controller")]
    [SerializeField] private string speedParamName = "Speed";

    // Триггеры для действий, согласно скриншоту
    [Tooltip("Имя Trigger параметра для атаки кулаком")]
    [SerializeField] private string punchTriggerName = "Punch";
    [Tooltip("Имя Trigger параметра для анимации смерти")]
    [SerializeField] private string deathTriggerName = "Death";
    [Tooltip("Имя Trigger параметра для открытия двери")]
    [SerializeField] private string openDoorTriggerName = "OpenDoor";
    [Tooltip("Имя Trigger параметра для закрытия двери")]
    [SerializeField] private string closeDoorTriggerName = "CloseDoor";
    [Tooltip("Имя Trigger параметра для проверки шкафа")]
    [SerializeField] private string checkCabTriggerName = "CheckCab";
    [Tooltip("Имя Trigger параметра для приседания")]
    [SerializeField] private string sitDownTriggerName = "SitDown";
    [Tooltip("Имя Trigger параметра для вставания")]
    [SerializeField] private string standUpTriggerName = "StandUp";

    void Awake()
    {
        if (animator == null)
        {
            Debug.LogError("Animator reference is not set on " + gameObject.name + ". Please assign it in the Inspector!", this);
            enabled = false; // Отключаем скрипт, если нет Animator
            return;
        }

        if (navMeshAgent == null)
        {
            Debug.LogWarning("NavMeshAgent reference is not set on " + gameObject.name + ". Ensure MonsterAI updates animation speed externally if needed.", this);
        }
    }

    /// <summary>
    /// Устанавливает значение float параметра 'Speed' в Animator.
    /// Вызывается для управления анимацией ходьбы/бега.
    /// </summary>
    /// <param name="speed">Текущая скорость движения робота.</param>
    public void SetSpeed(float speed)
    {
        if (animator != null)
        {
            animator.SetFloat(speedParamName, speed);
        }
    }

    /// <summary>
    /// Запускает анимацию атаки кулаком.
    /// </summary>
    public void TriggerPunch()
    {
        if (animator != null)
        {
            animator.SetTrigger(punchTriggerName);
        }
    }

    /// <summary>
    /// Запускает анимацию смерти.
    /// </summary>
    public void TriggerDeath()
    {
        if (animator != null)
        {
            animator.SetTrigger(deathTriggerName);
        }
    }

    /// <summary>
    /// Запускает анимацию открытия двери.
    /// </summary>
    public void TriggerOpenDoor()
    {
        if (animator != null)
        {
            animator.SetTrigger(openDoorTriggerName);
        }
    }

    /// <summary>
    /// Запускает анимацию закрытия двери.
    /// </summary>
    public void TriggerCloseDoor()
    {
        if (animator != null)
        {
            animator.SetTrigger(closeDoorTriggerName);
        }
    }

    /// <summary>
    /// Запускает анимацию проверки шкафа.
    /// </summary>
    public void TriggerCheckCab()
    {
        if (animator != null)
        {
            animator.SetTrigger(checkCabTriggerName);
        }
    }

    /// <summary>
    /// Запускает анимацию приседания.
    /// </summary>
    public void TriggerSitDown()
    {
        if (animator != null)
        {
            animator.SetTrigger(sitDownTriggerName);
        }
    }

    /// <summary>
    /// Запускает анимацию вставания.
    /// </summary>
    public void TriggerStandUp()
    {
        if (animator != null)
        {
            animator.SetTrigger(standUpTriggerName);
        }
    }

    // Метод для получения текущей скорости NavMeshAgent (для отладки)
    public float GetCurrentNavMeshSpeed()
    {
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            return navMeshAgent.velocity.magnitude;
        }
        return 0f;
    }
}