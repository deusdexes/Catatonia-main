using UnityEditor;
using UnityEngine;
using UnityEditor.Build;
using System.Collections.Generic;
using UnityEditor.Compilation;
using System.Reflection;
using System.Linq;
using System;

[InitializeOnLoad]
public class ES3ScriptingDefineSymbols
{
    static ES3ScriptingDefineSymbols()
    {
        SetDefineSymbols();
    }

    static void SetDefineSymbols()
    {
        if (Type.GetType("Unity.VisualScripting.IncludeInSettingsAttribute, Unity.VisualScripting.Core") != null)
            SetDefineSymbol("UNITY_VISUAL_SCRIPTING");

        if (Type.GetType("Ludiq.IncludeInSettingsAttribute, Ludiq.Core.Runtime") != null)
            SetDefineSymbol("BOLT_VISUAL_SCRIPTING");
    }

    static void SetDefineSymbol(string symbol)
    {
#if UNITY_2021_2_OR_NEWER
        foreach (var target in GetAllNamedBuildTargets())
        {
            string[] defines;
            PlayerSettings.GetScriptingDefineSymbols(target, out defines);
            if(!defines.Contains(symbol))
            {
                ArrayUtility.Add(ref defines, symbol);
                PlayerSettings.SetScriptingDefineSymbols(target, defines);
            }
        }
#else
        string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        var allDefines = new HashSet<string>(definesString.Split(';'));
        if (!allDefines.Contains(symbol))
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", allDefines.Concat(new string[] { symbol }).ToArray()));
#endif
            return;
    }

#if UNITY_2021_2_OR_NEWER
    static List<NamedBuildTarget> GetAllNamedBuildTargets()
    {
        var staticFields = typeof(NamedBuildTarget).GetFields(BindingFlags.Public | BindingFlags.Static);
        var buildTargets = new List<NamedBuildTarget>();

        foreach (var staticField in staticFields)
        {
            // Мы исключаем 'Unknown', так как это может вызвать ошибки при использовании с некоторыми методами.
            // Также явно исключаем "Stadia", поскольку эта платформа больше не поддерживается Unity.
            if (staticField.Name == "Unknown" || staticField.Name == "Stadia")
                continue;

            try
            {
                if (staticField.FieldType == typeof(NamedBuildTarget))
                {
                    var target = (NamedBuildTarget)staticField.GetValue(null);
                    buildTargets.Add(target);
                }
            }
            catch (ArgumentException e)
            {
                // Если возникает ArgumentException при попытке получить NamedBuildTarget,
                // это означает, что платформа больше не существует или является невалидной для текущей версии Unity.
                // Просто игнорируем ее и логируем предупреждение.
                Debug.LogWarning($"Easy Save 3: Игнорируем устаревшую или недопустимую платформу при получении NamedBuildTargets: {staticField.Name}. Ошибка: {e.Message}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Easy Save 3: Неожиданная ошибка при обработке NamedBuildTarget {staticField.Name}: {e.Message}");
            }
        }

        return buildTargets;
    }
#endif
}