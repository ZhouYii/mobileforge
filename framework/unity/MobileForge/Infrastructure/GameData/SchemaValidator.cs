using System;
using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Validates loaded JSON data against schema rules.
    /// Called optionally by GameData after loading definitions.
    /// Supports: required fields, type checks, enum ranges, min/max values.
    /// </summary>
    public class SchemaValidator
    {
        public class ValidationResult
        {
            public bool IsValid { get; set; } = true;
            public List<string> Errors { get; } = new List<string>();

            public void AddError(string path, string message)
            {
                IsValid = false;
                Errors.Add($"{path}: {message}");
            }
        }

        public class FieldRule
        {
            public string FieldName { get; set; } = "";
            public bool Required { get; set; }
            public string ExpectedType { get; set; } = "";
            public double MinValue { get; set; } = double.MinValue;
            public double MaxValue { get; set; } = double.MaxValue;
            public List<object> EnumValues { get; set; } = new List<object>();
            public int MinLength { get; set; }

            public FieldRule(string name, string type = "", bool required = false)
            {
                FieldName = name;
                ExpectedType = type;
                Required = required;
            }
        }

        private readonly Dictionary<string, List<FieldRule>> _schemas = new();

        public void RegisterSchema(string typeName, List<FieldRule> rules)
        {
            _schemas[typeName] = rules;
        }

        public ValidationResult ValidateDefinition(string typeName, Dictionary<string, object> data)
        {
            var result = new ValidationResult();

            if (!_schemas.TryGetValue(typeName, out var rules))
                return result;

            foreach (var rule in rules)
                ValidateField(data, rule, typeName, result);

            return result;
        }

        public ValidationResult ValidateDefinitions(string typeName, List<Dictionary<string, object>> definitions)
        {
            var result = new ValidationResult();

            if (!_schemas.ContainsKey(typeName))
                return result;

            for (int i = 0; i < definitions.Count; i++)
            {
                var entryResult = ValidateDefinition(typeName, definitions[i]);
                if (!entryResult.IsValid)
                {
                    foreach (var error in entryResult.Errors)
                        result.AddError($"[{i}]", error);
                }
            }

            return result;
        }

        public bool HasSchema(string typeName) => _schemas.ContainsKey(typeName);

        public void Clear() => _schemas.Clear();

        public static FieldRule Field(string name, string type = "", bool required = false)
            => new FieldRule(name, type, required);

        private void ValidateField(Dictionary<string, object> data, FieldRule rule, string context, ValidationResult result)
        {
            string path = $"{context}.{rule.FieldName}";

            if (rule.Required && !data.ContainsKey(rule.FieldName))
            {
                result.AddError(path, "required field missing");
                return;
            }

            if (!data.TryGetValue(rule.FieldName, out var value))
                return;

            if (!string.IsNullOrEmpty(rule.ExpectedType) && !CheckType(value, rule.ExpectedType))
            {
                result.AddError(path, $"expected type '{rule.ExpectedType}', got '{value?.GetType().Name ?? "null"}'");
                return;
            }

            if ((rule.ExpectedType == "int" || rule.ExpectedType == "float") && value != null)
            {
                try
                {
                    double numVal = Convert.ToDouble(value);
                    if (numVal < rule.MinValue)
                        result.AddError(path, $"value {numVal} below minimum {rule.MinValue}");
                    if (numVal > rule.MaxValue)
                        result.AddError(path, $"value {numVal} above maximum {rule.MaxValue}");
                }
                catch { /* non-numeric, already caught by type check */ }
            }

            if (rule.EnumValues.Count > 0 && !rule.EnumValues.Contains(value))
            {
                result.AddError(path, $"value '{value}' not in enum [{string.Join(", ", rule.EnumValues)}]");
            }

            if (rule.MinLength > 0)
            {
                if (value is string str && str.Length < rule.MinLength)
                    result.AddError(path, $"string length {str.Length} below minimum {rule.MinLength}");
                else if (value is IList<object> list && list.Count < rule.MinLength)
                    result.AddError(path, $"array size {list.Count} below minimum {rule.MinLength}");
            }
        }

        private bool CheckType(object value, string expected)
        {
            return expected switch
            {
                "int" => value is int || value is long || value is float || value is double,
                "float" => value is float || value is double || value is int || value is long,
                "string" => value is string,
                "bool" => value is bool,
                "array" => value is IList<object>,
                "dict" => value is IDictionary<string, object>,
                _ => true
            };
        }
    }
}
