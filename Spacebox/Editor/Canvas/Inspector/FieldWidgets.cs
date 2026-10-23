using System.Text.Json;
using System.Text.Json.Nodes;
using Engine.CanvasUI;
using Engine.Graphics.Atlas;
using OpenTK.Mathematics;
using Spacebox.Editor.Data;
using Spacebox.Editor.Canvas.Inspector;

namespace Spacebox.Editor.Canvas.Inspector;

public interface IFieldWidget
{
    bool CanEdit(FieldContext context);
    Control Create(FieldContext context, Action<JsonNode> commit);
}

public sealed class PrimitiveFieldWidget : IFieldWidget
{
    private static readonly string[] MultilineNames = { "Description", "Text", "Hint" };

    public bool CanEdit(FieldContext context) => context.Value is JsonValue || context.Value == null;

    public Control Create(FieldContext context, Action<JsonNode> commit)
    {
        if (context.Value == null)
        {
            var set = new Button("Set value") { ThemeTypeVariation = "SmallButton", SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
            var schema = context.SchemaType ?? typeof(string);
            set.Pressed += _ => commit(JsonSchema.CreateEmpty(schema));
            return set;
        }

        var json = (JsonValue)context.Value;
        switch (json.GetValueKind())
        {
            case JsonValueKind.True:
            case JsonValueKind.False:
                var check = new CheckBox("", json.GetValue<bool>()) { SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
                check.Toggled += (_, value) => commit(JsonValue.Create(value));
                return check;
            case JsonValueKind.Number:
                return CreateNumber(context, json, commit);
            default:
                string text = json.TryGetValue(out string s) ? s : json.ToJsonString();
                TextEditBase edit = context.NameIs(MultilineNames) ? new TextEdit { VisibleLinesMinimum = 3 } : new LineEdit();
                edit.Text = text;
                edit.TextChanged += value => commit(JsonValue.Create(value));
                return edit;
        }
    }

    private static Control CreateNumber(FieldContext context, JsonValue json, Action<JsonNode> commit)
    {
        EditorJson.TryGetNumber(json, out var number);
        var type = Nullable.GetUnderlyingType(context.SchemaType ?? typeof(double)) ?? context.SchemaType ?? typeof(double);
        bool integral = IsIntegral(type) || context.SchemaType == null && EditorJson.IsIntegral(json);
        var edit = new NumberEdit { Integer = integral, Decimals = 4 };
        if (integral) Range(type, edit);
        edit.Value = number;
        edit.ValueChanged += value => commit(integral ? JsonValue.Create((long)Math.Round(value)) : JsonValue.Create(Math.Round(value, 4)));
        return edit;
    }

    public static bool IsIntegral(Type type)
    {
        return type == typeof(int) || type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) ||
               type == typeof(ushort) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong);
    }

    private static void Range(Type type, NumberEdit edit)
    {
        if (type == typeof(byte)) { edit.MinValue = byte.MinValue; edit.MaxValue = byte.MaxValue; }
        else if (type == typeof(sbyte)) { edit.MinValue = sbyte.MinValue; edit.MaxValue = sbyte.MaxValue; }
        else if (type == typeof(short)) { edit.MinValue = short.MinValue; edit.MaxValue = short.MaxValue; }
        else if (type == typeof(ushort)) { edit.MinValue = ushort.MinValue; edit.MaxValue = ushort.MaxValue; }
    }
}

public sealed class NumberRangeFieldWidget : IFieldWidget
{
    private readonly Func<FieldContext, bool> _matches;
    private readonly int _min;
    private readonly int _max;

    public NumberRangeFieldWidget(Func<FieldContext, bool> matches, int min, int max)
    {
        _matches = matches;
        _min = min;
        _max = max;
    }

    public bool CanEdit(FieldContext context) => context.Value is JsonValue json && json.GetValueKind() == JsonValueKind.Number && _matches(context);

    public Control Create(FieldContext context, Action<JsonNode> commit)
    {
        EditorJson.TryGetNumber((JsonValue)context.Value, out var number);
        var edit = new NumberEdit { Integer = true, MinValue = _min, MaxValue = _max, Value = number };
        edit.ValueChanged += value => commit(JsonValue.Create((int)value));
        return edit;
    }
}

public sealed class ReferenceFieldWidget : IFieldWidget
{
    private readonly Func<FieldContext, bool> _matches;
    private readonly Func<EditorContext, IReadOnlyList<string>> _options;
    private readonly Func<EditorContext, string, AtlasSprite> _icon;

    public ReferenceFieldWidget(Func<FieldContext, bool> matches, Func<EditorContext, IReadOnlyList<string>> options, Func<EditorContext, string, AtlasSprite> icon = null)
    {
        _matches = matches;
        _options = options;
        _icon = icon;
    }

    public bool CanEdit(FieldContext context) => context.Value is JsonValue && context.StringValue != null && _matches(context);

    public Control Create(FieldContext context, Action<JsonNode> commit)
    {
        var editor = context.Editor;
        var combo = new ComboBox { Value = context.StringValue ?? "", PlaceholderText = "(none)" };
        foreach (var option in _options(editor) ?? Array.Empty<string>())
        {
            var item = combo.AddOption(option);
            if (_icon != null) item.WithIcon(_icon(editor, option));
        }
        combo.ValueChanged += value => commit(JsonValue.Create(value));
        return combo;
    }
}

public sealed class ColorFieldWidget : IFieldWidget
{
    public bool CanEdit(FieldContext context) => EditorJson.HasKeys(context.Value, "r", "g", "b");

    public Control Create(FieldContext context, Action<JsonNode> commit)
    {
        var obj = (JsonObject)context.Value;
        bool alpha = EditorJson.FindKey(obj, "a") != null;
        var picker = new ColorPickerButton
        {
            EditAlpha = alpha,
            Color = new Color4((float)EditorJson.GetNumber(obj, "r") / 255f, (float)EditorJson.GetNumber(obj, "g") / 255f, (float)EditorJson.GetNumber(obj, "b") / 255f, alpha ? (float)EditorJson.GetNumber(obj, "a", 255) / 255f : 1f),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            CustomMinimumSize = new Vector2(80f, 18f)
        };
        picker.ColorChanged += color =>
        {
            EditorJson.Set(obj, "r", JsonValue.Create(ToByte(color.R)));
            EditorJson.Set(obj, "g", JsonValue.Create(ToByte(color.G)));
            EditorJson.Set(obj, "b", JsonValue.Create(ToByte(color.B)));
            if (alpha) EditorJson.Set(obj, "a", JsonValue.Create(ToByte(color.A)));
            commit(obj);
        };
        return picker;
    }

    private static int ToByte(float value) => (int)MathF.Round(Math.Clamp(value, 0f, 1f) * 255f);
}

public sealed class HexColorFieldWidget : IFieldWidget
{
    private readonly Func<FieldContext, bool> _matches;

    public HexColorFieldWidget(Func<FieldContext, bool> matches)
    {
        _matches = matches;
    }

    public bool CanEdit(FieldContext context) => _matches(context) && (context.Value == null || context.Value is JsonValue value && value.TryGetValue(out string _));

    public Control Create(FieldContext context, Action<JsonNode> commit)
    {
        string text = context.Value is JsonValue value && value.TryGetValue(out string hex) ? hex : "";
        var picker = new ColorPickerButton
        {
            Color = ColorPickerButton.TryParseHex(text, out var color) ? color : Color4.White,
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            CustomMinimumSize = new Vector2(80f, 18f)
        };
        picker.ColorChanged += changed => commit(JsonValue.Create(ColorPickerButton.ToHex(changed)));
        return picker;
    }
}

public sealed class VectorFieldWidget : IFieldWidget
{
    private static readonly string[] Keys = { "x", "y", "z" };

    public bool CanEdit(FieldContext context) => EditorJson.HasKeys(context.Value, "x", "y", "z");

    public Control Create(FieldContext context, Action<JsonNode> commit)
    {
        var obj = (JsonObject)context.Value;
        bool integral = true;
        foreach (var key in Keys)
            integral &= EditorJson.Get(obj, key) is JsonValue value && EditorJson.IsIntegral(value);

        var row = new HBoxContainer();
        foreach (var key in Keys)
        {
            string name = key;
            row.AddChild(new Label(key.ToUpperInvariant()) { FontColor = EditorCanvasTheme.TextDim });
            var edit = row.AddChild(new NumberEdit { Integer = integral, Decimals = 3, Value = EditorJson.GetNumber(obj, name), SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand });
            edit.ValueChanged += value =>
            {
                EditorJson.Set(obj, name, integral ? JsonValue.Create((long)value) : JsonValue.Create(Math.Round(value, 4)));
                commit(obj);
            };
        }
        return row;
    }
}
