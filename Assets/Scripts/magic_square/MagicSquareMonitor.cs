using UnityEngine;

public class MagicSquareMonitor : MonoBehaviour
{
    [Header("Monitor Settings")]
    [Tooltip("Рендерер 3D-модели монитора, чей материал экрана должен меняться.")]
    public Renderer monitorRenderer;

    [Tooltip("Индекс материала экрана в списке материалов рендерера. Обычно 0 или 1.")]
    public int screenMaterialIndex = 0;

    [Header("Monitor Materials")]
    [Tooltip("Материал для состояния 'Неактивен' (полностью выключенный экран).")]
    public Material inactiveMaterial; // НОВЫЙ МАТЕРИАЛ

    [Tooltip("Материал для состояния 'Не решено' (активный экран головоломки).")]
    public Material unsolvedMaterial;

    [Header("Post-Solve Actions")]
    [Tooltip("Коллайдер-триггер, который должен быть ВКЛЮЧЕН после решения головоломки. Может быть пустым.")]
    public Collider targetColliderToEnable;

    [Tooltip("Материал для состояния 'Решено' (экран после решения).")]
    public Material solvedMaterial;

    // Перечисляем возможные состояния для понятности
    private enum MonitorState
    {
        Inactive,
        Unsolved,
        Solved
    }

    private MonitorState currentState;

    private void Start()
    {
        // 1. Устанавливаем НАЧАЛЬНОЕ состояние - "Неактивен"
        SetState(MonitorState.Inactive);
        if (targetColliderToEnable != null)
        {
            targetColliderToEnable.enabled = false;
        }
    }

    // Метод для внешнего вызова (например, из квест-менеджера)
    public void ActivateMonitor()
    {
        // Активировать можно только если он был неактивен
        if (currentState == MonitorState.Inactive)
        {
            Debug.Log("Монитор с магическим квадратом был активирован!");
            SetState(MonitorState.Unsolved);
        }
    }

    // Вызывается при взаимодействии игрока с монитором
    public void OnInteract()
    {
        // Взаимодействовать можно только в состоянии "Не решено"
        if (currentState == MonitorState.Unsolved)
        {
            if (MagicSquareGameManager.Instance != null)
            {
                MagicSquareGameManager.Instance.StartGame(this.gameObject);
            }
            else
            {
                Debug.LogError("MagicSquareGameManager.Instance не найден в сцене!");
            }
        }
        else if (currentState == MonitorState.Solved)
        {
            Debug.Log("Эта головоломка уже решена!");
        }
        else // Inactive
        {
            Debug.Log("Монитор пока неактивен.");
        }
    }

    // Вызывается, когда головоломка решена (например, из MagicSquareGameManager)
    public void MarkAsSolved()
    {
        // Отметить решенной можно только если она была не решена
        if (currentState == MonitorState.Unsolved)
        {
            Debug.Log("MagicSquareMonitor: Головоломка решена, меняем материал.");
            SetState(MonitorState.Solved);
            if (targetColliderToEnable != null)
            {
                targetColliderToEnable.enabled = true;
                Debug.Log($"Коллайдер на объекте '{targetColliderToEnable.gameObject.name}' был успешно ВКЛЮЧЕН!", targetColliderToEnable.gameObject);
            }
        }
    }

    // Централизованный метод для смены состояния и материала
    private void SetState(MonitorState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case MonitorState.Inactive:
                SetScreenMaterial(inactiveMaterial);
                break;
            case MonitorState.Unsolved:
                SetScreenMaterial(unsolvedMaterial);
                break;
            case MonitorState.Solved:
                SetScreenMaterial(solvedMaterial);
                break;
        }
    }

    // Надежный метод для смены материала экрана
    private void SetScreenMaterial(Material materialToSet)
    {
        if (monitorRenderer == null)
        {
            Debug.LogError("MonitorRenderer не назначен!", this);
            return;
        }
        if (materialToSet == null)
        {
            Debug.LogWarning($"Попытка установить пустой материал для состояния {currentState}", this);
            return;
        }

        // Получаем КОПИЮ текущего массива материалов
        Material[] materials = monitorRenderer.materials;

        // Проверяем, что индекс не выходит за пределы массива
        if (screenMaterialIndex < 0 || screenMaterialIndex >= materials.Length)
        {
            Debug.LogError($"Индекс материала '{screenMaterialIndex}' выходит за пределы массива материалов! " +
                           $"У объекта '{gameObject.name}' всего {materials.Length} материалов.", this);
            return;
        }

        // Заменяем нужный материал в копии массива
        materials[screenMaterialIndex] = materialToSet;

        // Присваиваем измененный массив обратно рендереру
        monitorRenderer.materials = materials;
    }
}