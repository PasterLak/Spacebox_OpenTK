using Engine.CanvasUI;
using Engine.Light;
using OpenTK.Mathematics;

namespace Engine.UI;

public sealed class DevLightingSection : DevPanelSection
{
    private readonly ColorPickerButton _ambient = new ColorPickerButton();
    private readonly ColorPickerButton _fog = new ColorPickerButton();
    private readonly HSlider _fogDensity = new HSlider { Step = 1 };
    private readonly Label _fogValue = new Label { CustomMinimumSize = new Vector2(32f, 0f) };

    public DevLightingSection() : base("Lighting")
    {
        _fogDensity.MinValue = Lighting.FogDensityMin;
        _fogDensity.MaxValue = Lighting.FogDensityMax;

        _ambient.ColorChanged += color => Lighting.AmbientColor = new Vector3(color.R, color.G, color.B);
        _fog.ColorChanged += color => Lighting.FogColor = new Vector3(color.R, color.G, color.B);
        _fogDensity.ValueChanged += value =>
        {
            Lighting.FogDensity = (int)value;
            _fogValue.Text = Lighting.FogDensity.ToString();
        };

        Content.AddChild(Row("Ambient", _ambient));
        Content.AddChild(Row("Fog color", _fog));
        var fogRow = Row("Fog", _fogDensity);
        fogRow.AddChild(_fogValue);
        Content.AddChild(fogRow);
    }

    public override void Refresh()
    {
        var ambient = Lighting.AmbientColor;
        var fog = Lighting.FogColor;
        _ambient.Color = new Color4(ambient.X, ambient.Y, ambient.Z, 1f);
        _fog.Color = new Color4(fog.X, fog.Y, fog.Z, 1f);
        if (!_fogDensity.HasFocus) _fogDensity.Value = Math.Clamp(Lighting.FogDensity, Lighting.FogDensityMin, Lighting.FogDensityMax);
        _fogValue.Text = Lighting.FogDensity.ToString();
    }
}
