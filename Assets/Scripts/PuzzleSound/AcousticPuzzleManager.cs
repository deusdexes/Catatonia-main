// Файл: AcousticPuzzleManager.cs (ФИНАЛЬНАЯ ВЕРСИЯ С ПРЯМЫМ УПРАВЛЕНИЕМ)
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using GameSystem;

[System.Serializable]
public class PuzzleQuestLink
{
    [Tooltip("ID головоломки из PuzzleDatabaseManager.")]
    public int puzzleID;
    [Tooltip("Список ID квестов, которые будут завершены после решения этой головоломки.")]
    public List<string> objectiveIDsToComplete = new List<string>();
}

public class AcousticPuzzleManager : MonoBehaviour
{
    public static AcousticPuzzleManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject puzzleCanvasObject;
    public List<NumberSlot> numberSlots = new List<NumberSlot>();
    public Button checkButton;

    [Header("Audio Settings")]
    public AudioClip correctSound;
    public AudioClip incorrectSound;
    public AudioSource audioSource;
    public float soundDelay = 0.5f;

    // --- ИЗМЕНЕНИЕ: Добавляем ссылку на контроллер игрока ---
    [Header("Player Control")]
    [Tooltip("Ссылка на PlayerController игрока. Если не задано, будет найдено автоматически.")]
    public PlayerController playerController;

    [Header("Quest Integration")]
    [Tooltip("Настройте здесь, какие квесты завершаются после решения конкретных головоломок.")]
    public List<PuzzleQuestLink> puzzleQuestLinks = new List<PuzzleQuestLink>();

    public event Action<int> OnPuzzleSolved;

    private int _currentPuzzleID;
    private int[] _correctCombination;
    private bool isChecking = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); } else { Instance = this; }
    }

    void Start()
    {
        // --- ИЗМЕНЕНИЕ: Ищем PlayerController, если он не назначен ---
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("AcousticPuzzleManager: PlayerController не назначен и не найден в сцене!", this);
            }
        }

        if (puzzleCanvasObject == null) puzzleCanvasObject = this.gameObject;
        puzzleCanvasObject.SetActive(false);
        if (checkButton != null) checkButton.onClick.AddListener(CheckCombination);
    }

    public void PresentPuzzle(int puzzleID)
    {
        _currentPuzzleID = puzzleID;

        PuzzleData data = PuzzleDatabaseManager.Instance.GetPuzzleDataByID(_currentPuzzleID);
        if (data == null)
        {
            Debug.LogError($"AcousticPuzzleManager не смог загрузить данные для ID {_currentPuzzleID}.");
            return;
        }

        _correctCombination = data.correctCombination;
        isChecking = false;

        foreach (var slot in numberSlots)
        {
            if (slot != null) slot.Initialize(); // Или ваш метод сброса
        }

        if (checkButton != null) checkButton.interactable = true;

        // --- ИЗМЕНЕНИЕ: Заменяем SwitchActionMap на прямое управление ---
        // Блокируем игрока и показываем UI
        if (playerController != null)
        {
            playerController.SetMovementEnabled(false);
            playerController.SetLookEnabled(false);
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        puzzleCanvasObject.SetActive(true);
    }

    public void ClosePuzzle()
    {
        // --- ИЗМЕНЕНИЕ: Заменяем SwitchActionMap на прямое управление ---
        // Возвращаем управление игроку
        if (playerController != null)
        {
            playerController.SetMovementEnabled(true);
            playerController.SetLookEnabled(true);
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        puzzleCanvasObject.SetActive(false);
    }

    public void CheckCombination()
    {
        if (isChecking) return;
        StartCoroutine(PlaySoundSequence());
    }

    private IEnumerator PlaySoundSequence()
    {
        isChecking = true;
        if (checkButton != null) checkButton.interactable = false;

        int[] playerInput = new int[_correctCombination.Length];
        for (int i = 0; i < numberSlots.Count; i++)
        {
            playerInput[i] = numberSlots[i].GetCurrentValue();
        }

        bool allCorrect = true;
        for (int i = 0; i < _correctCombination.Length; i++)
        {
            if (playerInput[i] == _correctCombination[i])
            {
                if (audioSource != null && correctSound != null) audioSource.PlayOneShot(correctSound);
            }
            else
            {
                if (audioSource != null && incorrectSound != null) audioSource.PlayOneShot(incorrectSound);
                allCorrect = false;
            }
            yield return new WaitForSeconds(soundDelay);
        }

        if (allCorrect)
        {
            PuzzleQuestLink link = puzzleQuestLinks.FirstOrDefault(l => l.puzzleID == _currentPuzzleID);

            if (link != null)
            {
                // Если связь найдена, проходимся по всем ID квестов в ней
                foreach (string objectiveID in link.objectiveIDsToComplete)
                {
                    if (!string.IsNullOrEmpty(objectiveID))
                    {
                        Debug.Log($"[AcousticPuzzleManager] Завершаем квест '{objectiveID}' после решения головоломки ID: {_currentPuzzleID}");
                        QuestManager.Instance.CompleteObjective(objectiveID);
                    }
                }
            }
            OnPuzzleSolved?.Invoke(_currentPuzzleID);
            yield return new WaitForSeconds(0.5f);
            ClosePuzzle();
        }
        else
        {
            if (checkButton != null) checkButton.interactable = true;
            isChecking = false;
        }
    }

    public bool IsPuzzleActive()
    {
        return puzzleCanvasObject != null && puzzleCanvasObject.activeSelf;
    }
}