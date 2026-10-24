using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Engine.CanvasUI;
using Engine.UI;
using System.Text;

namespace Engine
{
    public static class ParticleSystemUI
    {
        static DebugCanvas _canvas;
        static ParticleSystemPanel _panel;

        #region textures
        struct TexEntry { public string Path; public Texture2D Tex; }
        static readonly List<TexEntry> _tex = new();
        static readonly List<(string Name, Texture2D Texture)> _bank = new();
        static string[] CollectTexturePaths(string rootDirectory, params string[] formats)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory) || !Directory.Exists(rootDirectory))
                return Array.Empty<string>();

            if (formats == null || formats.Length == 0)
                formats = new[] { ".png", ".jpg" };

            var allowed = new HashSet<string>(formats.Select(e => e.StartsWith('.') ? e.ToLower() : "." + e.ToLower()),
                                              StringComparer.OrdinalIgnoreCase);

            return Directory
                   .EnumerateFiles(rootDirectory, "*.*", SearchOption.AllDirectories)
                   .Where(f => allowed.Contains(Path.GetExtension(f)))
                   .ToArray();
        }
        static void EnsureTex()
        {
            if (_tex.Count > 0) return;
            var root = Path.Combine(AppContext.BaseDirectory, "Resources", "Textures");

            var paths = CollectTexturePaths(root, new string[] { ".png", ".jpg" });

            Resources.LoadAll<Texture2D>(paths);

            foreach ( var p in paths )
            {
                var t = Resources.Load<Texture2D>(p);

                t.FilterMode = FilterMode.Nearest;
                _tex.Add(new TexEntry { Path = p, Tex = t });
                _bank.Add((Path.GetFileNameWithoutExtension(p), t));
            }
        }

        #endregion

        public static void Show(ParticleSystem[] systems, OrbitalCamera cam)
        {
            if (systems == null || systems.Length == 0 || Application.IsHeadless) return;

            EnsureTex();
            if (_canvas == null)
            {
                _canvas = new DebugCanvas("Particle systems");
                _panel = DebugCanvas.Area(_canvas.Root, UiLayer.Windows).AddChild(new ParticleSystemPanel(_bank));
            }

            /*ImGui.Text("Texture"); ImGui.NextColumn();
            Vector2 preview = new(64, 64);
            if (_curTex != null &&
                ImGui.ImageButton("##texPreview", (IntPtr)_curTex.Handle, preview))
                ImGui.OpenPopup("texPicker");
            ImGui.NextColumn();
            DrawTexturePicker("texPicker", ref _curTex, _tex, () =>
            {
                ps.Material = new ParticleMaterial(_curTex);
            });*/

            // 2) flip buttons
            _panel.SetSystems(systems);
            _canvas.Layer.Update(Time.Delta);
            cam.CanMove = !(_canvas.Root.WantsPointer || _canvas.Root.WantsKeyboard);
            _canvas.Layer.Render();
        }

        public static void Shutdown()
        {
            _canvas?.Dispose();
            _canvas = null;
            _panel = null;
        }

        public static string GenerateEmitterCode(EmitterBase e, string varName)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"var {varName} = new {e.GetType().Name}");
            sb.AppendLine("{");
            sb.AppendLine($"    SpeedMin={e.SpeedMin}f, SpeedMax={e.SpeedMax}f,");
            sb.AppendLine($"    LifeMin={e.LifeMin}f,  LifeMax={e.LifeMax}f,");
            sb.AppendLine($"    StartSizeMin={e.StartSizeMin}f, StartSizeMax={e.StartSizeMax}f,");
            sb.AppendLine($"    EndSizeMin={e.EndSizeMin}f,   EndSizeMax={e.EndSizeMax}f,");
            sb.AppendLine($"    AccelerationStart=new Vector3({e.AccelerationStart.X}f,{e.AccelerationStart.Y}f,{e.AccelerationStart.Z}f),");
            sb.AppendLine($"    AccelerationEnd  =new Vector3({e.AccelerationEnd.X}f,{e.AccelerationEnd.Y}f,{e.AccelerationEnd.Z}f),");
            sb.AppendLine($"    RotationSpeedMin={e.RotationSpeedMin}f, RotationSpeedMax={e.RotationSpeedMax}f,");
            sb.AppendLine($"    ColorStart=new Vector4({e.ColorStart.X}f,{e.ColorStart.Y}f,{e.ColorStart.Z}f,{e.ColorStart.W}f),");
            sb.AppendLine($"    ColorEnd  =new Vector4({e.ColorEnd.X}f,{e.ColorEnd.Y}f,{e.ColorEnd.Z}f,{e.ColorEnd.W}f),");
            switch (e)
            {
                case BoxEmitter b:
                    sb.AppendLine($"    Center=new Vector3({b.Center.X}f,{b.Center.Y}f,{b.Center.Z}f),");
                    sb.AppendLine($"    Size  =new Vector3({b.Size.X}f,{b.Size.Y}f,{b.Size.Z}f),");
                    sb.AppendLine($"    Direction=new Vector3({b.Direction.X}f,{b.Direction.Y}f,{b.Direction.Z}f),"); break;
                case ConeEmitter c:
                    sb.AppendLine($"    Apex  =new Vector3({c.Apex.X}f,{c.Apex.Y}f,{c.Apex.Z}f),");
                    sb.AppendLine($"    Axis  =new Vector3({c.Axis.X}f,{c.Axis.Y}f,{c.Axis.Z}f),");
                    sb.AppendLine($"    Angle ={c.Angle}f,");
                    sb.AppendLine($"    Height={c.Height}f,"); break;
                case DiskEmitter d:
                    sb.AppendLine($"    Center=new Vector3({d.Center.X}f,{d.Center.Y}f,{d.Center.Z}f),");
                    sb.AppendLine($"    Normal=new Vector3({d.Normal.X}f,{d.Normal.Y}f,{d.Normal.Z}f),");
                    sb.AppendLine($"    Radius={d.Radius}f,"); break;
                case PlaneEmitter p:
                    sb.AppendLine($"    Center=new Vector3({p.Center.X}f,{p.Center.Y}f,{p.Center.Z}f),");
                    sb.AppendLine($"    Normal=new Vector3({p.Normal.X}f,{p.Normal.Y}f,{p.Normal.Z}f),");
                    sb.AppendLine($"    Width ={p.Width}f,");
                    sb.AppendLine($"    Height={p.Height}f,");
                    sb.AppendLine($"    Direction=new Vector3({p.Direction.X}f,{p.Direction.Y}f,{p.Direction.Z}f),"); break;
                case SphereEmitter s:
                    sb.AppendLine($"    Center=new Vector3({s.Center.X}f,{s.Center.Y}f,{s.Center.Z}f),");
                    sb.AppendLine($"    Radius={s.Radius}f,"); break;
                case PointEmitter pe:
                    sb.AppendLine($"    Position =new Vector3({pe.Position.X}f,{pe.Position.Y}f,{pe.Position.Z}f),");
                    sb.AppendLine($"    Direction=new Vector3({pe.Direction.X}f,{pe.Direction.Y}f,{pe.Direction.Z}f),"); break;
            }
            sb.AppendLine("};");
            return sb.ToString();
        }
    }
}
