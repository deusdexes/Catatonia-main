using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ThrowPreview : MonoBehaviour
{
    private LineRenderer lineRenderer;

    [Header("Trajectory Settings")]
    public int trajectorySegments = 20; // Количество сегментов для LineRenderer
    public float trajectoryTimeStep = 0.1f; // Шаг времени для симуляции
    public float maxSimulationTime = 2f; // Максимальное время симуляции траектории

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false; // По умолчанию траектория скрыта
        lineRenderer.positionCount = 0;

        // Настройка внешнего вида LineRenderer (можно изменить в инспекторе)
        if (lineRenderer.material == null)
        {
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.startColor = Color.cyan;
        lineRenderer.endColor = Color.blue;
    }

    /// <summary>
    /// Симулирует и отрисовывает траекторию броска.
    /// </summary>
    /// <param name="startPosition">Начальная позиция броска.</param>
    /// <param name="initialVelocity">Начальная скорость броска.</param>
    /// <param name="hitLayers">Слои, с которыми может столкнуться траектория.</param>
    public void SimulateTrajectory(Vector3 startPosition, Vector3 initialVelocity, LayerMask hitLayers)
    {
        lineRenderer.enabled = true;
        lineRenderer.positionCount = trajectorySegments;

        Vector3 currentPosition = startPosition;
        Vector3 currentVelocity = initialVelocity;
        Vector3[] points = new Vector3[trajectorySegments];
        points[0] = currentPosition;

        for (int i = 1; i < trajectorySegments; i++)
        {
            float time = trajectoryTimeStep * i;
            if (time > maxSimulationTime)
            {
                lineRenderer.positionCount = i;
                break;
            }

            Vector3 nextPosition = startPosition + initialVelocity * time + Physics.gravity * (0.5f * time * time);

            // Проверяем на столкновение между текущей и следующей позицией
            RaycastHit hit;
            if (Physics.Raycast(currentPosition, (nextPosition - currentPosition).normalized, out hit, Vector3.Distance(currentPosition, nextPosition), hitLayers))
            {
                // Если столкновение произошло, траектория заканчивается здесь
                lineRenderer.positionCount = i + 1;
                points[i] = hit.point;
                // Изменяем цвет последней точки, чтобы показать место удара
                lineRenderer.startColor = Color.cyan;
                lineRenderer.endColor = Color.red; // Красный цвет для точки удара
                break;
            }

            points[i] = nextPosition;
            currentPosition = nextPosition;

            // Если симуляция дошла до конца без столкновений
            if (i == trajectorySegments - 1)
            {
                lineRenderer.startColor = Color.cyan;
                lineRenderer.endColor = Color.blue; // Синий, если не было удара
            }
        }
        lineRenderer.SetPositions(points);
    }

    public void DisablePreview()
    {
        lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;
    }
}