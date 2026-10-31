using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

// Необходимо для Drag & Drop
public class MagicSquareTile : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public TextMeshProUGUI numberText;
    [HideInInspector] public int Value;

    public RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    public Transform originalParent;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Initialize(int value)
    {
        Value = value;
        numberText.text = value.ToString();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        // Устанавливаем родителя на корень Canvas, чтобы плитка отображалась поверх других элементов UI
// Позволяет рейкасту доходить до ячеек под плиткой
        Debug.Log($"Начало перетаскивания плитки со значением {Value}");

        // Очищаем значение в предыдущей ячейке, если оно было
        MagicSquareCell originalCell = originalParent.GetComponent<MagicSquareCell>();
        if (originalCell != null)
        {
            originalCell.ClearTile();
        }
        transform.SetParent(MagicSquareGameManager.Instance.puzzleCanvas.transform);
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / MagicSquareGameManager.Instance.puzzleCanvas.GetComponent<Canvas>().scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        if (eventData.pointerCurrentRaycast.gameObject != null)
        {
            Debug.Log("Raycast попал в объект: " + eventData.pointerCurrentRaycast.gameObject.name);
        }
        else
        {
            Debug.Log("Raycast не попал в UI-элемент.");
        }
        // Проверяем, была ли плитка брошена на допустимую зону (MagicSquareCell)
        if (transform.parent == MagicSquareGameManager.Instance.puzzleCanvas.transform)
        {
            // Если родитель не был изменен (т.е. плитка не была дропнута на MagicSquareCell), 
            // возвращаем ее на исходное место
            transform.SetParent(originalParent);
            rectTransform.anchoredPosition = Vector2.zero; // Возвращаем в центр родителя

            // Если исходное место было ячейкой, нужно восстановить ее значение в гриде
            MagicSquareCell originalCell = originalParent.GetComponent<MagicSquareCell>();
            if (originalCell != null && originalCell.CellIndex != -1)
            {
                MagicSquareGameManager.Instance.UpdateGridValue(originalCell.CellIndex, Value);
            }
        }
        // Если родитель был изменен, значит, плитка успешно дропнута в ячейку. 
        // Логика обновления грида будет в MagicSquareCell.OnDrop.
    }
}