using UnityEngine;
using UnityEngine.AI; // Добавляем, так как CreatureController использует NavMeshAgent

public class MonsterAnimationController : MonoBehaviour
{
    // Ссылка на компонент Animator, которую мы будем перетаскивать в Inspector
    [Header("Component References")]
    [Tooltip("Перетащите сюда компонент Animator этого монстра")]
    [SerializeField] private Animator animator;

    // Ссылка на компонент NavMeshAgent, которую мы будем перетаскивать в Inspector
    [Tooltip("Перетащите сюда компонент NavMeshAgent этого монстра (опционально)")]
    [SerializeField] private NavMeshAgent navMeshAgent;

    [Header("Animation Parameter Names")]
    [Tooltip("Имя Float параметра скорости в Animator Controller")]
    [SerializeField] private string speedParamName = "Speed";
    [Tooltip("Имя Bool параметра для удара кулаком в Animator Controller")]
    [SerializeField] private string punchingParamName = "IsPunching";

    // Добавь сюда другие параметры, если они появятся (например, IsDead, IsHurt)

    // Awake теперь используется только для проверки, что ссылки не null
    void Awake()
    {
        if (animator == null)
        {
            Debug.LogError("Animator reference is not set on " + gameObject.name + ". Please assign it in the Inspector!", this);
            enabled = false; // Отключаем скрипт, если нет Animator
            return;
        }

        // NavMeshAgent теперь опционален, предупреждаем, если не установлен
        if (navMeshAgent == null)
        {
            Debug.LogWarning("NavMeshAgent reference is not set on " + gameObject.name + ". Ensure CreatureController updates animation speed externally if needed.", this);
        }
    }

    // --- Public методы, которые CreatureController будет вызывать ---

    /// <summary>
    /// Устанавливает значение float параметра 'Speed' в Animator.
    /// CreatureController будет вызывать это, передавая скорость движения.
    /// </summary>
    /// <param name="speed">Текущая скорость движения монстра.</param>
    public void SetSpeed(float speed)
    {
        if (animator != null)
        {
            animator.SetFloat(speedParamName, speed);
        }
    }

    /// <summary>
    /// Устанавливает значение bool параметра 'IsPunching' в true для запуска анимации удара.
    /// CreatureController будет вызывать это, когда монстр начинает атаку ударом.
    /// </summary>
    public void StartPunch()
    {
        if (animator != null)
        {
            animator.SetBool(punchingParamName, true);
        }
    }

    /// <summary>
    /// Устанавливает значение bool параметра 'IsPunching' в false для завершения анимации удара.
    /// Это должно вызываться через Animation Event в клипе 'Punch' или из CreatureController
    /// после завершения анимации удара.
    /// </summary>
    public void StopPunch()
    {
        if (animator != null)
        {
            animator.SetBool(punchingParamName, false);
        }
    }

    // --- Дополнительные методы для отладки или других анимаций ---

    /// <summary>
    /// Метод, который можно использовать для отладки или получения текущей скорости NavMeshAgent.
    /// CreatureController теперь будет вызывать SetSpeed напрямую.
    /// </summary>
    public float GetCurrentNavMeshSpeed()
    {
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            return navMeshAgent.velocity.magnitude;
        }
        return 0f;
    }

    // Если у тебя есть анимации смерти, получения урона и т.п., ты можешь добавить здесь методы:
    // public void Die() { if(animator != null) animator.SetTrigger("Die"); }
    // public void TakeDamage() { if(animator != null) animator.SetTrigger("Hit"); }
}