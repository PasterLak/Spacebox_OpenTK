using Engine;
using Engine.Components;
using Engine.Light;
using OpenTK.Mathematics;

namespace Spacebox.Game.Player.Armor;

public sealed class ArmorRenderer : Component, IExtraLightTarget
{
    private static readonly int SlotCount = Enum.GetValues<EquipmentSlotType>().Length;

    private readonly ArmorPiece?[] _pieces = new ArmorPiece?[SlotCount];

    public bool Visible { get; set; } = true;
    public Vector3 ExtraLight { get; set; }

    public void Set(EquipmentSlotType slot, EquipmentItem? item)
    {
        _pieces[(int)slot] = ArmorLibrary.Get(item);
    }

    public override void OnRender()
    {
        var camera = Camera.Main;
        if (!Visible || camera == null || Owner == null) return;

        var model = Owner.GetRenderModelMatrix();
        var view = camera.GetViewMatrix();
        var projection = camera.GetProjectionMatrix();

        for (int i = 0; i < _pieces.Length; i++)
        {
            var piece = _pieces[i];
            if (piece == null) continue;

            var material = piece.Material;
            material.ExtraLight = ExtraLight;
            material.Apply(model);
            material.Shader.SetMatrix4("view", view);
            material.Shader.SetMatrix4("projection", projection);
            piece.Mesh.Render();
        }
    }
}
