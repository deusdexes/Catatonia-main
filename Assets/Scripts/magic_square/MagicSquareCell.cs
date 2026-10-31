using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class MagicSquareCell : MonoBehaviour, IDropHandler
{
    [Tooltip("Индекс ячейки от 0 до 8 (0,0=0, 0,1=1... 2,2=8)")]
    public int CellIndex = -1;
    public TextMeshProUGUI fixedValueText;

    private MagicSquareTile currentTile;

    public void SetFixedValue(int value)
    {
        // Устанавливает число как фиксированное (нельзя перетаскивать)
        fixedValueText.text = value.ToString();
        fixedValueText.gameObject.SetActive(true);
        // Обновляем грид менеджера
        MagicSquareGameManager.Instance.UpdateGridValue(CellIndex, value);
    }

    public void ClearCell()
    {
        // Очищаем визуальное представление
        fixedValueText.text = "";
        fixedValueText.gameObject.SetActive(false);
        if (currentTile != null)
        {
            Destroy(currentTile.gameObject);
            currentTile = null;
        }
        // Очистка значения в гриде (если вызывается при генерации)
        if (CellIndex != -1)
            MagicSquareGameManager.Instance.ClearGridValue(CellIndex);
    }

    // Вызывается компонентом MagicSquareTile при начале перетаскивания
    public void ClearTile()
    {
        currentTile = null;
        if (CellIndex != -1)
            MagicSquareGameManager.Instance.ClearGridValue(CellIndex);
    }

    public void OnDrop(PointerEventData eventData)
    {
        MagicSquareTile droppedTile = eventData.pointerDrag.GetComponent<MagicSquareTile>();

        // Проверяем, что это плитка и ячейка не фиксирована
        if (droppedTile != null && fixedValueText.gameObject.activeSelf == false)
        {
            // 1. Возвращаем старую плитку (если есть) обратно в контейнер для перетаскивания
            if (currentTile != null)
            {
                // Используем сохраненный originalParent плитки как целевой контейнер.
                // В нашем случае это должен быть MagicSquareGameManager.Instance.tileContainer
                currentTile.transform.SetParent(MagicSquareGameManager.Instance.tileContainer);
                currentTile.rectTransform.anchoredPosition = Vector2.zero; // Возвращаем в центр
                // Убедимся, что плитка "отвязана" от ячейки
                currentTile.originalParent = MagicSquareGameManager.Instance.tileContainer;
            }

            // 2. Устанавливаем новую плитку в ячейку
            droppedTile.transform.SetParent(transform);
            droppedTile.transform.localPosition = Vector3.zero;
            currentTile = droppedTile;

            // 3. Обновляем значение в менеджере игры
            MagicSquareGameManager.Instance.UpdateGridValue(CellIndex, currentTile.Value);
        }
        else if (fixedValueText.gameObject.activeSelf)
        {
            // Если пытались бросить на фиксированную ячейку, возвращаем плитку на ее originalParent.
            if (droppedTile != null)
            {
                droppedTile.transform.SetParent(droppedTile.originalParent);
                droppedTile.rectTransform.anchoredPosition = Vector2.zero;
                // Не забываем восстановить значение в гриде, если originalParent был ячейкой
                MagicSquareCell originalCell = droppedTile.originalParent.GetComponent<MagicSquareCell>();
                if (originalCell != null)
                {
                    MagicSquareGameManager.Instance.UpdateGridValue(originalCell.CellIndex, droppedTile.Value);
                }
            }
        }
        // Если droppedTile == null, то это не плитка, игнорируем.
    }
}