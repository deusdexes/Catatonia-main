using UnityEngine;

public class ForceDisableAnimator : MonoBehaviour
{
    void Start()
    {
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            Debug.Log("ForceDisableAnimator: Disabling animator on " + gameObject.name);
            animator.enabled = false;

            // Дополнительная гарантия - выключаем через несколько кадров
            StartCoroutine(DisableAfterFrames());
        }
    }

    private System.Collections.IEnumerator DisableAfterFrames()
    {
        // Ждем 2 кадра
        yield return null;
        yield return null;

        Animator animator = GetComponent<Animator>();
        if (animator != null && animator.enabled)
        {
            animator.enabled = false;
            Debug.Log("ForceDisableAnimator: Animator force-disabled on frame 2");
        }
    }

    void OnEnable()
    {
        // При каждом включении объекта тоже выключаем аниматор
        Animator animator = GetComponent<Animator>();
        if (animator != null && animator.enabled)
        {
            animator.enabled = false;
        }
    }
}