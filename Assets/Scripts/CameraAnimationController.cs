using UnityEngine;

public class CameraAnimationController : MonoBehaviour
{
    [Header("Camera Animation Settings")]
    public float flyDistance = 10f;
    public float animationDuration = 2f;
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Camera animationCamera;
    private Camera mainCamera;
    private bool isAnimating = false;
    private Vector3 startPosition;
    private float animationTimer = 0f;

    void Start()
    {
        CreateAnimationCamera();
        DontDestroyOnLoad(this.gameObject);
    }

    void Update()
    {
        if (isAnimating)
        {
            AnimateCamera();
        }
    }

    private void CreateAnimationCamera()
    {
        GameObject cameraObj = new GameObject("AnimationCamera");
        animationCamera = cameraObj.AddComponent<Camera>();
        animationCamera.enabled = false;
        animationCamera.depth = 100;
        DontDestroyOnLoad(cameraObj);
    }

    public void StartCameraAnimation()
    {
        mainCamera = Camera.main;

        if (mainCamera != null && !isAnimating)
        {
            // Копируем настройки основной камеры
            animationCamera.CopyFrom(mainCamera);
            animationCamera.transform.position = mainCamera.transform.position;
            animationCamera.transform.rotation = mainCamera.transform.rotation;

            // Настройки для анимационной камеры
            animationCamera.depth = mainCamera.depth + 100;

            startPosition = animationCamera.transform.position;
            animationCamera.enabled = true;
            isAnimating = true;
            animationTimer = 0f;

            Debug.Log("Camera animation started");
        }
    }

    public void StopCameraAnimation()
    {
        if (isAnimating)
        {
            isAnimating = false;
            animationCamera.enabled = false;
            Debug.Log("Camera animation stopped");
        }
    }

    private void AnimateCamera()
    {
        animationTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(animationTimer / animationDuration);
        float curveValue = movementCurve.Evaluate(progress);

        Vector3 flyDirection = animationCamera.transform.forward;
        animationCamera.transform.position = startPosition + flyDirection * (curveValue * flyDistance);

        if (progress >= 1f)
        {
            StopCameraAnimation();
        }
    }

    public bool IsAnimating
    {
        get { return isAnimating; }
    }
}