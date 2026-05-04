using System.Text.Json;

namespace AccountingSystem.Controllers.APIs;

public static class DevExtremeFormValueMapper
{
    public static void Apply(string values, params FormValueSetter[] setters)
    {
        if (string.IsNullOrWhiteSpace(values))
            return;

        var formValues = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(values);
        if (formValues is null || formValues.Count == 0)
            return;

        foreach (var setter in setters)
        {
            if (formValues.TryGetValue(setter.PropertyName, out var value))
                setter.Apply(value);
        }
    }
}

public sealed class FormValueSetter
{
    private FormValueSetter(string propertyName, Action<JsonElement> apply)
    {
        PropertyName = propertyName;
        Apply = apply;
    }

    public string PropertyName { get; }
    public Action<JsonElement> Apply { get; }

    public static FormValueSetter String(string propertyName, Action<string> assign, bool trim = false)
    {
        return new FormValueSetter(propertyName, value =>
        {
            if (value.ValueKind == JsonValueKind.Null)
                return;

            var text = value.GetString() ?? string.Empty;
            assign(trim ? text.Trim() : text);
        });
    }

    public static FormValueSetter Boolean(string propertyName, Action<bool> assign)
    {
        return new FormValueSetter(propertyName, value =>
        {
            if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                assign(value.GetBoolean());
        });
    }
}
