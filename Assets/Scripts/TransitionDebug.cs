using UnityEngine;

public class TransitionDebug : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("=== TRANSITION DEBUG ===");
            Debug.Log($"Instance exists: {SceneTransitionManager.Instance != null}");
            Debug.Log($"Is transitioning: {SceneTransitionManager.Instance?.IsTransitioning}");

            if (SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.uiAnimator != null)
            {
                Animator animator = SceneTransitionManager.Instance.uiAnimator;
                Debug.Log($"Animator enabled: {animator.enabled}");
                Debug.Log($"Animator gameObject active: {animator.gameObject.activeInHierarchy}");
                Debug.Log($"Animator isInitialized: {animator.isInitialized}");
            }
        }

        // Тест перехода с 2 на 1 сцену
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SceneTransitionManager.Instance?.LoadScene("Scene1");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SceneTransitionManager.Instance?.LoadScene("Scene2");
        }
    }
}