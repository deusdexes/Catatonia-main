using System.Collections.Generic;
using UnityEngine;

public class FuzzyLogicSystem : MonoBehaviour
{
    [System.Serializable]
    public class FuzzySet
    {
        public string name;
        public AnimationCurve membershipFunction;

        public float GetMembership(float inputValue)
        {
            return membershipFunction.Evaluate(inputValue);
        }
    }

    [System.Serializable]
    public class FuzzyVariable
    {
        public string variableName;
        public List<FuzzySet> fuzzySets = new List<FuzzySet>();
    }

    [System.Serializable]
    public class FuzzyRule
    {
        public string name;
        public string conditionVariable;
        public string conditionSet;
        public string outputVariable;
        public string outputSet;

        public float Evaluate(Dictionary<string, float> inputs, Dictionary<string, FuzzyVariable> variables)
        {
            if (inputs.ContainsKey(conditionVariable) && variables.ContainsKey(conditionVariable))
            {
                float inputValue = inputs[conditionVariable];
                var variable = variables[conditionVariable];

                // Находим нужный fuzzy set
                foreach (var set in variable.fuzzySets)
                {
                    if (set.name == conditionSet)
                    {
                        return set.GetMembership(inputValue);
                    }
                }
            }
            return 0f;
        }
    }

    public List<FuzzyVariable> inputVariables = new List<FuzzyVariable>();
    public List<FuzzyVariable> outputVariables = new List<FuzzyVariable>();
    public List<FuzzyRule> rules = new List<FuzzyRule>();

    private Dictionary<string, float> currentInputs = new Dictionary<string, float>();
    private Dictionary<string, float> outputResults = new Dictionary<string, float>();
    private Dictionary<string, FuzzyVariable> allVariables = new Dictionary<string, FuzzyVariable>();

    void Start()
    {
        // Собираем все переменные в один словарь для быстрого доступа
        foreach (var variable in inputVariables)
        {
            allVariables[variable.variableName] = variable;
        }
        foreach (var variable in outputVariables)
        {
            allVariables[variable.variableName] = variable;
        }

        // Инициализируем выходные результаты
        foreach (var outputVar in outputVariables)
        {
            outputResults[outputVar.variableName] = 0f;
        }
    }

    public void SetInput(string variableName, float value)
    {
        currentInputs[variableName] = Mathf.Clamp01(value);
    }

    public float GetOutput(string variableName)
    {
        if (outputResults.ContainsKey(variableName))
        {
            return outputResults[variableName];
        }
        else
        {
            Debug.LogWarning($"Output variable '{variableName}' not found. Available: {string.Join(", ", outputResults.Keys)}");
            return 0f;
        }
    }

    public void Evaluate()
    {
        // Сбрасываем выходные значения
        foreach (var outputVar in outputVariables)
        {
            outputResults[outputVar.variableName] = 0f;
        }

        // Применяем все правила
        foreach (var rule in rules)
        {
            float ruleStrength = rule.Evaluate(currentInputs, allVariables);

            if (ruleStrength > 0)
            {
                // Применяем правило к выходной переменной
                if (outputResults.ContainsKey(rule.outputVariable))
                {
                    outputResults[rule.outputVariable] = Mathf.Max(outputResults[rule.outputVariable], ruleStrength);
                }
            }
        }
    }
}