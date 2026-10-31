using UnityEngine;
using UnityEngine.UI;

public class ButtonVisibilityChecker : MonoBehaviour
{
    void Start()
    {
        CheckAllButtons();
    }

    void CheckAllButtons()
    {
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Button btn in allButtons)
        {
            Debug.Log($"Кнопка: {btn.name} | " +
                     $"Активна: {btn.gameObject.activeInHierarchy} | " +
                     $"Interactable: {btn.interactable} | " +
                     $"Родитель: {btn.transform.parent.name}");
        }
    }

    [ContextMenu("Check Buttons Now")]
    void CheckButtonsNow()
    {
        CheckAllButtons();
    }
}