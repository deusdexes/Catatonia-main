using UnityEngine;

public class AnimatorDebug : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("No Animator found on " + gameObject.name);
            return;
        }

        Debug.Log("Animator Debug Info:");
        Debug.Log("- Enabled: " + animator.enabled);
        Debug.Log("- Has Controller: " + (animator.runtimeAnimatorController != null));
        Debug.Log("- Controller: " + animator.runtimeAnimatorController?.name);
        Debug.Log("- Layer Count: " + animator.layerCount);
        Debug.Log("- Current State: " + animator.GetCurrentAnimatorStateInfo(0).fullPathHash);
    }

    void Update()
    {
        // Логируем изменения состояния
        if (animator != null)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("FadeIn") || state.IsName("FadeOut"))
            {
                Debug.Log($"Current state: {state}, Normalized time: {state.normalizedTime}");
            }
        }
    }

    // Методы для ручного тестирования
    [ContextMenu("Test FadeIn")]
    public void TestFadeIn()
    {
        if (animator != null)
        {
            animator.SetTrigger("FadeIn");
            Debug.Log("Manual FadeIn triggered");
        }
    }

    [ContextMenu("Test FadeOut")]
    public void TestFadeOut()
    {
        if (animator != null)
        {
            animator.SetTrigger("FadeOut");
            Debug.Log("Manual FadeOut triggered");
        }
    }
}