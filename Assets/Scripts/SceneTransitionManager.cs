using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    [Header("Animation References")]
    public Animator uiAnimator;
    public CameraAnimationController cameraController;

    [Header("Timing")]
    public float cameraAnimationTime = 2f;
    public float fadeInTime = 1f;
    public float fadeOutTime = 1f;

    private bool isTransitioning = false;
    private AsyncOperation sceneLoadOperation;
    private Coroutine currentTransition;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Убедимся что UI активен но в спящем состоянии
            if (uiAnimator != null)
            {
                uiAnimator.gameObject.SetActive(true);
                uiAnimator.enabled = true;
                ResetAnimatorToDefaultState();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadScene(string sceneName)
    {
        if (!isTransitioning && currentTransition == null)
        {
            currentTransition = StartCoroutine(AnimatedTransitionCoroutine(sceneName));
        }
    }

    private IEnumerator AnimatedTransitionCoroutine(string sceneName)
    {
        isTransitioning = true;
        Debug.Log($"Starting animated transition to: {sceneName}");

        // 1. Подготавливаем UI для анимации
        if (uiAnimator != null)
        {
            uiAnimator.gameObject.SetActive(true);
            uiAnimator.enabled = true;
            ResetAnimatorToDefaultState();
        }

        // 2. Отключаем управление на текущей сцене
        DisableCurrentScene();

        // 3. Запускаем UI анимацию затемнения
        if (uiAnimator != null)
        {
            uiAnimator.SetTrigger("FadeIn");
            Debug.Log("UI FadeIn triggered");
        }

        // 4. Ждем затемнения
        yield return new WaitForSeconds(fadeInTime);

        // 5. Запускаем анимацию камеры на СТАРОЙ сцене
        if (cameraController != null)
        {
            cameraController.StartCameraAnimation();
            Debug.Log("Camera animation started");
        }

        // 6. Начинаем загрузку новой сцены
        sceneLoadOperation = SceneManager.LoadSceneAsync(sceneName);
        sceneLoadOperation.allowSceneActivation = false;

        // 7. Ждем загрузки сцены до 90%
        yield return StartCoroutine(WaitForSceneLoad());

        // 8. Ждем завершения анимации камеры
        float cameraWaitTime = 0f;
        while (cameraController != null && cameraController.IsAnimating && cameraWaitTime < cameraAnimationTime)
        {
            cameraWaitTime += Time.deltaTime;
            yield return null;
        }

        // 9. Останавливаем анимацию камеры
        if (cameraController != null)
        {
            cameraController.StopCameraAnimation();
        }

        // 10. Активируем новую сцену
        sceneLoadOperation.allowSceneActivation = true;
        yield return new WaitUntil(() => sceneLoadOperation.isDone);

        // 11. Ждем один кадр чтобы новая сцена полностью загрузилась
        yield return null;

        // 12. Настраиваем новую сцену
        SetupNewScene();

        // 13. Запускаем UI анимацию появления (FadeOut)
        if (uiAnimator != null)
        {
            uiAnimator.SetTrigger("FadeOut");
            Debug.Log("UI FadeOut triggered on new scene");
        }

        // 14. Ждем завершения FadeOut
        yield return new WaitForSeconds(fadeOutTime);

        // 15. Возвращаем UI в спящее состояние (но не выключаем полностью!)
        if (uiAnimator != null)
        {
            ResetAnimatorToDefaultState();
        }

        isTransitioning = false;
        currentTransition = null;
        Debug.Log("Animated transition completed");
    }

    private void ResetAnimatorToDefaultState()
    {
        if (uiAnimator != null)
        {
            // Сбрасываем все триггеры
            uiAnimator.ResetTrigger("FadeIn");
            uiAnimator.ResetTrigger("FadeOut");

            // Перезагружаем аниматор чтобы вернуться в начальное состояние
            uiAnimator.Rebind();

            // Принудительно обновляем аниматор
            uiAnimator.Update(0f);

            Debug.Log("Animator reset to default state");
        }
    }

    private IEnumerator WaitForSceneLoad()
    {
        float timeout = 5f;
        float timer = 0f;

        while (sceneLoadOperation.progress < 0.9f && timer < timeout)
        {
            timer += Time.deltaTime;
            Debug.Log($"Scene loading: {sceneLoadOperation.progress * 100:F1}%");
            yield return null;
        }

        if (timer >= timeout)
        {
            Debug.LogWarning("Scene loading timeout");
        }
        else
        {
            Debug.Log("Scene loaded to 90%, ready for activation");
        }
    }

    private void DisableCurrentScene()
    {
        PlayerController currentPlayerController = FindAnyObjectByType<PlayerController>();
        if (currentPlayerController != null)
        {
            currentPlayerController.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SetupNewScene()
    {
        PlayerController newPlayerController = FindAnyObjectByType<PlayerController>();
        if (newPlayerController != null)
        {
            newPlayerController.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public bool IsTransitioning
    {
        get { return isTransitioning; }
    }
}