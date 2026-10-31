[System.Serializable]
public class PuzzleData
{
    public string description; // Ќеоб€зательное поле дл€ удобства в инспекторе
    public int puzzleID;
    public int[] correctCombination = new int[5];
}