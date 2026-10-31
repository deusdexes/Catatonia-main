using UnityEngine;

public class InteractablePuzzleTrigger : MonoBehaviour
{
    [Header("Puzzle Settings")]
    [Tooltip("ID головоломки из базы данных, которую нужно запустить.")]
    public int puzzleID;

    [Tooltip("Сообщение для игрока, когда он может взаимодействовать.")]
    public GameObject interactMessageUI;

    private bool playerInRange = false;

    void Start()
    {
        if (interactMessageUI != null) interactMessageUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (interactMessageUI != null) interactMessageUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (interactMessageUI != null) interactMessageUI.SetActive(false);
        }
    }

    // Этот метод должен вызываться из вашего скрипта PlayerInteraction
    public void Interact()
    {
        if (playerInRange)
        {
            // Говорим единому менеджеру показать головоломку с нашим ID
            AcousticPuzzleManager.Instance.PresentPuzzle(puzzleID);
        }
    }
}