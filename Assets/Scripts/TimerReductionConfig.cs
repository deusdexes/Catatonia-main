using UnityEngine;

[CreateAssetMenu(fileName = "TimerReductionConfig", menuName = "Quest/Timer Reduction Config", order = 1)]
public class TimerReductionConfig : ScriptableObject
{
    [Tooltip("Минимальное количество секунд, на которое будет сокращен таймер.")]
    public float minReduction = 5f;
    [Tooltip("Максимальное количество секунд, на которое будет сокращен таймер.")]
    public float maxReduction = 8f;
}