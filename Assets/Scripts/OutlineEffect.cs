using UnityEngine;
using System.Collections.Generic;

public class OutlineEffect : MonoBehaviour
{
    [Tooltip("Материал, который будет использоваться для обводки.")]
    public Material outlineMaterial;
    [Tooltip("Ширина обводки.")]
    [Range(0.001f, 0.1f)]
    public float outlineWidth = 0.02f;
    [Tooltip("Цвет обводки.")]
    public Color outlineColor = Color.yellow;
    [Tooltip("Включить обводку для всех дочерних рендереров?")]
    public bool includeChildrenRenderers = true;

    private List<Renderer> targetRenderers = new List<Renderer>(); // Список всех рендереров, которыми мы будем управлять
    private Dictionary<Renderer, List<Material>> originalMaterialsMap = new Dictionary<Renderer, List<Material>>();
    private Dictionary<Renderer, List<Material>> outlineInstanceMaterialsMap = new Dictionary<Renderer, List<Material>>();

    void Awake()
    {
        // Находим все рендереры, которыми будем управлять
        if (includeChildrenRenderers)
        {
            targetRenderers.AddRange(GetComponentsInChildren<Renderer>());
        }
        else
        {
            Renderer selfRenderer = GetComponent<Renderer>();
            if (selfRenderer != null)
            {
                targetRenderers.Add(selfRenderer);
            }
        }

        if (targetRenderers.Count == 0)
        {
            Debug.LogWarning($"OutlineEffect на объекте {gameObject.name}: Не найдено рендереров для управления. Обводка не будет работать.");
            enabled = false; // Отключаем скрипт, если нет рендереров
            return;
        }

        if (outlineMaterial == null)
        {
            Debug.LogWarning($"OutlineEffect на объекте {gameObject.name}: outlineMaterial не назначен. Обводка не будет работать.");
            enabled = false;
            return;
        }

        foreach (Renderer rend in targetRenderers)
        {
            // Сохраняем оригинальные материалы для каждого рендерера
            List<Material> originalMats = new List<Material>();
            originalMats.AddRange(rend.sharedMaterials);
            originalMaterialsMap.Add(rend, originalMats);

            // Создаем экземпляры материалов обводки для каждого рендерера
            List<Material> outlineMats = new List<Material>();
            foreach (var mat in originalMats) // Создаем столько же материалов обводки, сколько у рендерера оригинальных материалов
            {
                Material newOutlineInstance = new Material(outlineMaterial);
                outlineMats.Add(newOutlineInstance);
            }
            outlineInstanceMaterialsMap.Add(rend, outlineMats);
        }

        DisableOutline(); // Изначально обводка отключена
    }

    // Включает обводку
    public void EnableOutline()
    {
        foreach (Renderer rend in targetRenderers)
        {
            if (rend == null) continue; // Пропускаем удаленные рендереры

            List<Material> outlineMats = outlineInstanceMaterialsMap[rend];
            if (outlineMats == null || outlineMats.Count == 0) continue;

            // Применяем настройки к экземплярам материалов обводки
            foreach (Material outlineMatInstance in outlineMats)
            {
                outlineMatInstance.SetFloat("_OutlineWidth", outlineWidth);
                outlineMatInstance.SetColor("_OutlineColor", outlineColor);
                // Если ваш шейдер поддерживает _EmissionColor или _Emission, можете настроить их здесь
                // outlineMatInstance.SetColor("_EmissionColor", outlineColor * emissionIntensity); // Пример для свечения
            }

            // Собираем новый массив материалов: сначала оригинальные, потом обводка
            List<Material> newMaterials = new List<Material>(originalMaterialsMap[rend]);
            newMaterials.AddRange(outlineMats); // Добавляем материалы обводки

            rend.materials = newMaterials.ToArray();
        }
    }

    // Отключает обводку, возвращая оригинальные материалы
    public void DisableOutline()
    {
        foreach (Renderer rend in targetRenderers)
        {
            if (rend == null) continue; // Пропускаем удаленные рендереры
            if (originalMaterialsMap.ContainsKey(rend))
            {
                rend.materials = originalMaterialsMap[rend].ToArray();
            }
        }
    }

    void OnDestroy()
    {
        // Очищаем созданные экземпляры материалов
        foreach (var entry in outlineInstanceMaterialsMap)
        {
            foreach (Material mat in entry.Value)
            {
                if (mat != null)
                {
                    Destroy(mat);
                }
            }
        }
    }
}