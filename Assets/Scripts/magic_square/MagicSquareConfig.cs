// MagicSquareConfig.cs
using UnityEngine;

[System.Serializable]
public struct MagicSquareConfig
{
    [Tooltip("9 чисел, представляющих собой решаемый магический квадрат (обычно 1-9)")]
    public int[] BaseNumbers; // Должно быть 9 элементов

    // Базовая сумма для этих чисел всегда 15, если используются 1-9
    // Убрал [HideInInspector] const, так как он не сериализуется, но оставил для ясности
    public const int BaseTargetSum = 15;
}