using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PuzzleDatabaseManager : MonoBehaviour
{
    public static PuzzleDatabaseManager Instance { get; private set; }

    public List<PuzzleData> puzzleDatabase = new List<PuzzleData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public PuzzleData GetPuzzleDataByID(int id)
    {
        PuzzleData data = puzzleDatabase.FirstOrDefault(p => p.puzzleID == id);
        if (data == null)
        {
            Debug.LogError($"PuzzleDatabaseManager: Головоломка с ID {id} не найдена в базе данных!", this);
        }
        return data;
    }
}