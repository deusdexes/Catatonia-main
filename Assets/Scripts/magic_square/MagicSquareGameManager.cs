using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using GameSystem;
// Структура для хранения данных о ячейке, включая предустановленные значения
public struct CellData
{
    public int Value;
    public bool IsFixed;
}

public class MagicSquareGameManager : MonoBehaviour
{
    public static MagicSquareGameManager Instance { get; private set; }

    // Список заведомо решаемых базовых конфигураций
    private static readonly List<MagicSquareConfig> SolvableConfigs = new List<MagicSquareConfig>
    {
        // 1. Стандартный (Сумма 15)
        new MagicSquareConfig { BaseNumbers = new int[] { 8, 1, 6, 3, 5, 7, 4, 9, 2 } },
        // 2. Поворот на 90 градусов
        new MagicSquareConfig { BaseNumbers = new int[] { 4, 3, 8, 9, 5, 1, 2, 7, 6 } },
        // 3. Зеркальное отражение
        new MagicSquareConfig { BaseNumbers = new int[] { 6, 1, 8, 7, 5, 3, 2, 9, 4 } },
    };

    [Header("UI References")]
    public GameObject puzzleCanvas;
    public TextMeshProUGUI targetSumText;
    public Transform tileContainer;
    public MagicSquareCell[] cells; // 3x3 = 9 ячеек
    public GameObject tilePrefab; // Префаб плитки с компонентом MagicSquareTile

    [Header("Sum Display UI")] 
    public TextMeshProUGUI[] rowSums = new TextMeshProUGUI[3];
    public TextMeshProUGUI[] colSums = new TextMeshProUGUI[3];
    public TextMeshProUGUI diagSumL; // Левая-Главная диагональ (0,0 -> 2,2)
    public TextMeshProUGUI diagSumR;

    [Header("Player Control")]
    [Tooltip("Ссылка на компонент PlayerController для блокировки управления.")]
    public PlayerController playerController;

    [Header("Game Settings")]
    [Tooltip("Минимальная целевая сумма (должна быть >= 18, чтобы использовать числа > 9)")]
    public int minTargetSum = 18;
    [Tooltip("Максимальная целевая сумма (должна быть <= 30, чтобы не превышать 9+5=14)")]
    public int maxTargetSum = 30;
    public int minTileValue = 1;

    public int maxTileValue = 15;
    
    private CellData[,] grid = new CellData[3, 3];
    private int targetSum;
    private GameObject currentMonitor;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        puzzleCanvas.SetActive(false);
    }

    public bool IsGameActive()
    {
        // Проверяем, активен ли Canvas головоломки
        return puzzleCanvas != null && puzzleCanvas.activeSelf;
    }

    // Публичный метод для закрытия извне (например, из PlayerInputHandler)
    public void CloseGameExternally()
    {
        if (IsGameActive())
        {
            Debug.Log("MagicSquareGameManager: Закрытие игры по команде извне.");
            CloseGame(); // Вызываем существующий метод CloseGame()
        }
    }

        public void StartGame(GameObject monitor)
    {
        currentMonitor = monitor;
        GenerateGame();
        puzzleCanvas.SetActive(true);
        if (playerController != null)
        {
            playerController.SetMovementEnabled(false);
            playerController.SetLookEnabled(false);
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseGame()
    {
        puzzleCanvas.SetActive(false);
        if (playerController != null)
        {
            playerController.SetMovementEnabled(true);
            playerController.SetLookEnabled(true);
        }

        // Восстанавливаем курсор для FPS-геймплея
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void GenerateGame()
    {
        // 1. Очистка
        for (int i = 0; i < 9; i++)
        {
            cells[i].ClearCell();
            grid[i / 3, i % 3] = new CellData { Value = 0, IsFixed = false };
        }
        foreach (Transform child in tileContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Выбор целевой суммы и смещения (Offset)
        // Разница TargetSum - BaseSum (15) должна быть кратна 3: T = 15 + 3*O, где O >= 1
        // Возможные T: 18, 21, 24, 27, 30. (Offset O: 1, 2, 3, 4, 5)

        List<int> possibleOffsets = new List<int>();
        for (int O = 1; O <= 5; O++)
        {
            int T = MagicSquareConfig.BaseTargetSum + 3 * O;
            if (T >= minTargetSum && T <= maxTargetSum)
            {
                possibleOffsets.Add(O);
            }
        }

        if (possibleOffsets.Count == 0)
        {
            Debug.LogError("Не удалось найти подходящий Target Sum. Проверьте minTargetSum/maxTargetSum!");
            targetSum = MagicSquareConfig.BaseTargetSum + 3; // Fallback: 18
        }
        else
        {
            int selectedOffset = possibleOffsets[Random.Range(0, possibleOffsets.Count)];
            targetSum = MagicSquareConfig.BaseTargetSum + 3 * selectedOffset;

            // 3. Выбор базовой конфигурации и создание смещенного решения
            MagicSquareConfig baseConfig = SolvableConfigs[Random.Range(0, SolvableConfigs.Count)];
            int[] solutionNumbers = new int[9];
            for (int i = 0; i < 9; i++)
            {
                solutionNumbers[i] = baseConfig.BaseNumbers[i] + selectedOffset;
            }

            targetSumText.text = $"Целевая сумма: {targetSum}";

            // 4. Предустановка 2-3 случайных полей
            HashSet<int> fixedIndices = new HashSet<int>();
            HashSet<int> fixedValues = new HashSet<int>();
            int fixedCount = Random.Range(2, 4);

            while (fixedIndices.Count < fixedCount)
            {
                int index = Random.Range(0, 9);
                fixedIndices.Add(index);
            }

            // Установка фиксированных значений и обновление грида
            for (int i = 0; i < 9; i++)
            {
                int row = i / 3;
                int col = i % 3;

                if (fixedIndices.Contains(i))
                {
                    int fixedValue = solutionNumbers[i];
                    grid[row, col] = new CellData { Value = fixedValue, IsFixed = true };
                    cells[i].SetFixedValue(fixedValue);
                    fixedValues.Add(fixedValue);
                }
                // Иначе (пустая ячейка) остается { Value = 0, IsFixed = false }
            }

            // 5. Генерация перетаскиваемых плиток
            // Включаем все числа от minTileValue до maxTileValue, которые НЕ были установлены как фиксированные.
            // Это будет включать:
            // а) Оставшиеся 7-8 чисел из solutionNumbers.
            // б) Все остальные "лишние" числа (дистракторы) в диапазоне 1-15.
            List<int> availableTiles = new List<int>();
            for (int i = minTileValue; i <= maxTileValue; i++)
            {
                if (!fixedValues.Contains(i))
                {
                    availableTiles.Add(i);
                }
            }

            // Шейк плиток для лучшего UX
            availableTiles = availableTiles.OrderBy(x => Random.value).ToList();

            foreach (int number in availableTiles)
            {
                GameObject tileObj = Instantiate(tilePrefab, tileContainer);
                MagicSquareTile tile = tileObj.GetComponent<MagicSquareTile>();
                tile.Initialize(number);
            }
        }
    }

    // Вызывается компонентом MagicSquareCell при успешном дропе
    public void UpdateGridValue(int index, int value)
    {
        int row = index / 3;
        int col = index % 3;
        grid[row, col].Value = value;

        UpdateSumDisplay();

        // Проверяем победу после каждого успешного дропа
        CheckWinCondition();
    }

    public void ClearGridValue(int index)
    {
        int row = index / 3;
        int col = index % 3;
        grid[row, col].Value = 0;
        UpdateSumDisplay();
    }

    private void CheckWinCondition()
    {
        // Проверяем, все ли ячейки заполнены 
        bool allFilled = true;
        for (int i = 0; i < 9; i++)
        {
            if (grid[i / 3, i % 3].Value == 0)
            {
                allFilled = false;
                break;
            }
        }

        if (!allFilled) return;

        bool solved = true;

        // 1. Проверка строк
        for (int r = 0; r < 3; r++)
        {
            int sum = grid[r, 0].Value + grid[r, 1].Value + grid[r, 2].Value;
            if (sum != targetSum) solved = false;
        }

        // 2. Проверка столбцов
        if (solved)
        {
            for (int c = 0; c < 3; c++)
            {
                int sum = grid[0, c].Value + grid[1, c].Value + grid[2, c].Value;
                if (sum != targetSum) solved = false;
            }
        }

        // 3. Проверка главной диагонали
        if (solved)
        {
            int sum = grid[0, 0].Value + grid[1, 1].Value + grid[2, 2].Value;
            if (sum != targetSum) solved = false;
        }

        // 4. Проверка побочной диагонали
        if (solved)
        {
            int sum = grid[0, 2].Value + grid[1, 1].Value + grid[2, 0].Value;
            if (sum != targetSum) solved = false;
        }


        if (solved)
        {
            Debug.Log("Поздравляем! Магический квадрат решен!");
            OnGameSolved();
        }
        else
        {
            Debug.Log("Квадрат заполнен, но суммы не совпадают. Попробуйте снова.");
        }
    }
    private void OnGameSolved()
    {
        CloseGame();

        if (QuestManager.Instance != null)
        {
            // Завершаем финальный квест
            QuestManager.Instance.CompleteObjective("QUEST_KPP");
        }

        if (currentMonitor != null)
        {
            MagicSquareMonitor monitor = currentMonitor.GetComponent<MagicSquareMonitor>();
            if (monitor != null)
            {
                Debug.Log($"MagicSquareGameManager: Вызываем ChangeToSolvedMaterial на мониторе: {currentMonitor.name}");
                monitor.MarkAsSolved();
            }
            else
            {
                Debug.LogWarning($"MagicSquareGameManager: На объекте '{currentMonitor.name}' не найден компонент MagicSquareMonitor!");
            }
        }
    }

    private void UpdateSumDisplay()
    {
        if (targetSumText == null) return; // Не можем работать без базовых ссылок

        // 1. Проверка и обновление строк
        for (int r = 0; r < 3; r++)
        {
            int sum = grid[r, 0].Value + grid[r, 1].Value + grid[r, 2].Value;
            if (rowSums[r] != null)
            {
                rowSums[r].text = sum.ToString();
                // Опционально: Изменение цвета, если сумма совпадает с целевой
                if (sum != 0) // Игнорируем пустые строки
                {
                    rowSums[r].color = (sum == targetSum) ? Color.green : Color.white;
                }
                else
                {
                    rowSums[r].color = Color.gray; // Серый для пустых строк
                }
            }
        }

        // 2. Проверка и обновление столбцов
        for (int c = 0; c < 3; c++)
        {
            int sum = grid[0, c].Value + grid[1, c].Value + grid[2, c].Value;
            if (colSums[c] != null)
            {
                colSums[c].text = sum.ToString();
                if (sum != 0)
                {
                    colSums[c].color = (sum == targetSum) ? Color.green : Color.white;
                }
                else
                {
                    colSums[c].color = Color.gray;
                }
            }
        }

        // 3. Проверка и обновление главной диагонали (L)
        int sumL = grid[0, 0].Value + grid[1, 1].Value + grid[2, 2].Value;
        if (diagSumL != null)
        {
            diagSumL.text = sumL.ToString();
            if (sumL != 0)
            {
                diagSumL.color = (sumL == targetSum) ? Color.green : Color.white;
            }
            else
            {
                diagSumL.color = Color.gray;
            }
        }

        // 4. Проверка и обновление побочной диагонали (R)
        int sumR = grid[0, 2].Value + grid[1, 1].Value + grid[2, 0].Value;
        if (diagSumR != null)
        {
            diagSumR.text = sumR.ToString();
            if (sumR != 0)
            {
                diagSumR.color = (sumR == targetSum) ? Color.green : Color.white;
            }
            else
            {
                diagSumR.color = Color.gray;
            }
        }
    }
}