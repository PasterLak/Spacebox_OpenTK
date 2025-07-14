using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Engine
{
    public static class ParticleSystemUI
    {
        static int _activeIdx;
        static readonly string[] _emitNames = { "Box", "Cone", "Disk", "Plane", "Sphere", "Point" };
        static readonly Type[] _emitTypes =
        {
            typeof(BoxEmitter), typeof(ConeEmitter), typeof(DiskEmitter),
            typeof(PlaneEmitter), typeof(SphereEmitter), typeof(PointEmitter)
        };

        static readonly Vector4[] _axisClr = { new(1, 0, 0, 1), new(0, 1, 0, 1), new(0, 0, 1, 1) };
        static readonly string[] _axisLbl = { "X", "Y", "Z" };

        #region textures
        struct TexEntry { public string Path; public Texture2D Tex; }
        static readonly List<TexEntry> _tex = new();
        static Texture2D _curTex; // активная текстура-превью
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

                
               // t.FlipY();
                t.FilterMode = FilterMode.Nearest;
                _tex.Add(new TexEntry { Path = p, Tex = t });
            }

            _curTex = _tex.Count > 0 ? _tex[0].Tex : null;
        }
    ----------------------------------------------------------
        static void DrawTextureRow(ref Texture2D curTex,
                                   IReadOnlyList<TexEntry> texBank, ParticleSystem ps,
                                   string pickerPopupId = "##texPicker")
        {
            if (curTex == null && texBank.Count > 0)          
                curTex = texBank[0].Tex;

            float rowW = ImGui.GetContentRegionAvail().X;
            float previewW = 72;                             
            float ctrlW = rowW - previewW - ImGui.GetStyle().ItemSpacing.X;

            ImGui.Columns(2, "tex_row", false);
            ImGui.SetColumnWidth(0, previewW);
            // --- preview column --------------------------------------------------
            Vector2 pv = new(previewW, previewW);
            if (ImGui.ImageButton("##texPreview", (IntPtr)curTex.Handle, pv))
                ImGui.OpenPopup(pickerPopupId);
            ImGui.NextColumn();

         
            ImGui.PushItemWidth(ctrlW);
     
            int fIdx = curTex.FilterMode == FilterMode.Nearest ? 0 : 1;
            string[] fItems = { "Nearest", "Linear" };
            if (ImGui.Combo("Filter", ref fIdx, fItems, fItems.Length))
                curTex.FilterMode = fIdx == 0 ? FilterMode.Nearest : FilterMode.Linear;

            // 2) flip buttons
            if (ImGui.Button("Flip X")) curTex.FlipX(); ImGui.SameLine();
            if (ImGui.Button("Flip Y")) curTex.FlipY();
            ImGui.PopItemWidth();
            ImGui.Columns(1);

         
            DrawTexturePicker(pickerPopupId, ref curTex, texBank, () =>
            {
                ps.Material = new ParticleMaterial(_curTex);
            });
        }

        static void DrawTexturePicker(string popupId,
                              ref Texture2D current,
                              IReadOnlyList<TexEntry> all, Action onClicked)
        {
            if (!ImGui.BeginPopup(popupId))
                return;

            //--- target popup size ─ 30 % of screen width × 40 % of screen height
            var io = ImGui.GetIO();
            var desired = new Vector2(io.DisplaySize.X * .30f, io.DisplaySize.Y * .40f);
            var minSize = new Vector2(700, 450);                // never smaller than this
            var popupSize = new Vector2(MathF.Max(desired.X, minSize.X),
                                         MathF.Max(desired.Y, minSize.Y));

            ImGui.SetWindowSize(popupSize, ImGuiCond.Always);

            float avail = ImGui.GetContentRegionAvail().X;
            float spacing = ImGui.GetStyle().ItemSpacing.X;

            const float baseThumb = 96f;                           // desired thumb size
            int columns = Math.Max(1, (int)((popupSize.X + spacing) / (baseThumb + spacing)));
            float thumb = (popupSize.X - spacing * (columns - 1)) / columns;

            int col = 0;
            for (int i = 0; i < all.Count; ++i)
            {
                var tex = all[i].Tex;

                ImGui.PushID(i);
                if (ImGui.ImageButton("x"+ tex.Handle, tex.Handle, new Vector2(thumb, thumb)))
                {
                    current = tex;
                    onClicked?.Invoke();
                    ImGui.CloseCurrentPopup();
                    ImGui.PopID();
                    break;
                }
                ImGui.PopID();

                if (++col < columns)
                    ImGui.SameLine();
                else
                    col = 0;
            }

            ImGui.EndPopup();
        }


        #endregion

        public static void Show(ParticleSystem[] systems, OrbitalCamera cam)
        {
            if (systems == null || systems.Length == 0) return;
            _activeIdx = Math.Clamp(_activeIdx, 0, systems.Length - 1);
            var ps = systems[_activeIdx];
            if (ps == null) return;

            EnsureTex();

            var io = ImGui.GetIO();
            var style = ImGui.GetStyle();
            float winW = io.DisplaySize.X * .25f;
            ImGui.StyleColorsDark();
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));
            ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, 18f);
            ImGui.SetNextWindowPos(new Vector2(io.DisplaySize.X - winW, 0), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(winW, io.DisplaySize.Y), ImGuiCond.Always);
            ImGui.Begin("Particle Systems",
                        ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
                        ImGuiWindowFlags.AlwaysVerticalScrollbar);

            for (int i = 0; i < systems.Length; ++i)
            {
                if (i > 0) ImGui.SameLine();
                if (ImGui.Button($"System {i + 1}")) _activeIdx = i;
            }
            ImGui.Separator();
            cam.CanMove = !(ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows) || ImGui.IsAnyItemActive());

            bool en = ps.Enabled;
            if (ImGui.Checkbox("Enabled", ref en)) ps.Enabled = en;
            ImGui.SameLine();
            if (ImGui.Button("Restart")) ps.Restart();
            ImGui.Separator();

            DrawTransform(ps);
            DrawSystemSettings(ps);
            DrawEmitterCommon(ps);
            DrawEmitterSpecific(ps);

            ImGui.Separator();
            if (ImGui.Button("Generate Emitter Code"))
                ImGui.SetClipboardText(GenerateEmitterCode(ps.Emitter, $"emitter{_activeIdx + 1}"));

            ImGui.End();
            ImGui.PopStyleVar(2);
        }

        #region layout helpers
        static void DrawTransform(ParticleSystem ps)
        {
            DrawVec3("Position", ps.Position.ToSystemVector3(), v => ps.Position = v.ToOpenTKVector3(), .1f);
            DrawVec3("Rotation", ps.Rotation.ToSystemVector3(), v => ps.Rotation = v.ToOpenTKVector3(), 1f);
            DrawVec3("Scale", ps.Scale.ToSystemVector3(), v => ps.Scale = v.ToOpenTKVector3(), .1f);
            ImGui.Separator();
        }

        static void DrawSystemSettings(ParticleSystem ps)
        {
            ImGui.Columns(2, "sys", false); ImGui.PushItemWidth(-1);

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

            DrawTextureRow(ref _curTex, _tex, ps);


            DrawInt("Max Particles", ps.Max, v => ps.Max = Math.Max(0, v));
            ImGui.Text("Active"); ImGui.SameLine(); ImGui.Text(ps.ParticlesCount.ToString()); ImGui.SameLine();
            if (ImGui.Button("Clear")) ps.ClearParticles();
            ImGui.NextColumn(); ImGui.NextColumn();

            DrawFloat("Emission Rate", ps.Rate, v => ps.Rate = v);
            DrawFloat("Simulation Spd", ps.SimulationSpeed, v => ps.SimulationSpeed = v);
            DrawFloat("Duration", ps.Duration, v => ps.Duration = Math.Max(0, v));
            DrawBool("Loop", ps.Loop, v => ps.Loop = v);

            int sp = ps.Space == SimulationSpace.Local ? 0 : 1;
                   
            if (DrawComboRow("Space", ref sp, new[] { "Local", "World" }))
                ps.Space = sp == 0 ? SimulationSpace.Local : SimulationSpace.World;

            ImGui.NextColumn();

            int et = Array.FindIndex(_emitTypes, t => t == ps.Emitter.GetType());

            if (DrawComboRow("Emitter Type", ref et, _emitNames))
            {
                var ne = (EmitterBase)Activator.CreateInstance(_emitTypes[et])!;
                ne.CopyFrom(ps.Emitter);
                ps.SetEmitter(ne, false);
            }

            ImGui.NextColumn();

            ImGui.Columns(1); ImGui.PopItemWidth(); ImGui.Separator();
        }
        #endregion

        #region primitive row helpers

        static void DrawVec3(string label, Vector3 v, Action<Vector3> set, float speed = .1f)
        {
            float rowW = ImGui.GetContentRegionAvail().X;         
            ImGui.Columns(2, $"##row_{label}", false);
            ImGui.SetColumnWidth(0, rowW * .25f);                
            ImGui.Text(label);
            ImGui.NextColumn();

            // --- правый блок: «X Y Z» ---
            float avail = ImGui.GetContentRegionAvail().X;      
            float spacing = ImGui.GetStyle().ItemSpacing.X;
            float lblW = ImGui.CalcTextSize("X").X;          
            float slotW = (avail - (lblW + spacing) * 3         
                                    - spacing * 2)                 
                            / 3f;                                 

            float[] val = { v.X, v.Y, v.Z };
            bool changed = false;

            for (int i = 0; i < 3; ++i)
            {
                if (i != 0) ImGui.SameLine();

                ImGui.TextColored(_axisClr[i], _axisLbl[i]);
                ImGui.SameLine();

                ImGui.PushItemWidth(slotW);
                float before = val[i];
                ImGui.DragFloat($"##{label}_{_axisLbl[i]}", ref val[i], speed, 0f, 0f, "%.2f");
                ImGui.PopItemWidth();

                if (!changed && Math.Abs(val[i] - before) > float.Epsilon) changed = true;
            }

            if (changed) set(new Vector3(val[0], val[1], val[2]));
            ImGui.Columns(1);
        }

        static bool DrawComboRow(string label,
                         ref int current,
                         IReadOnlyList<string> items,
                         float leftWidthRatio = 0.5f)
        {
            float full = ImGui.GetContentRegionAvail().X;
            ImGui.Columns(2, $"##row_{label}", false);
            ImGui.SetColumnWidth(0, full * leftWidthRatio);
            ImGui.Text(label);
            ImGui.NextColumn();

            ImGui.SetNextItemWidth(-1);
            bool changed = ImGui.Combo($"##{label}", ref current,
                                       items.ToArray(), items.Count);

            ImGui.Columns(1);
            return changed;
        }


        static void DrawFloat(string label, float v, Action<float> set) => GenericRow(label, ref v, (id, refVal) =>
        {
            ImGui.InputFloat(id, ref refVal, .1f, 1f, "%.2f");
            return refVal;
        }, set);

        static void DrawInt(string label, int v, Action<int> set) => GenericRow(label, ref v, (id, refVal) =>
        {
            ImGui.InputInt(id, ref refVal);
            return refVal;
        }, set);

        static void DrawBool(string label, bool v, Action<bool> set) => GenericRow(label, ref v, (id, refVal) =>
        {
            ImGui.Checkbox(id, ref refVal);
            return refVal;
        }, set);
        static void DrawEmitterCommon(ParticleSystem ps)
        {
            var e = ps.Emitter;
            DrawRange("Speed", e.SpeedMin, e.SpeedMax, (a, b) => { e.SpeedMin = a; e.SpeedMax = b; });
            DrawRange("Life", e.LifeMin, e.LifeMax, (a, b) => { e.LifeMin = a; e.LifeMax = b; });
            DrawRange("Start Size", e.StartSizeMin, e.StartSizeMax, (a, b) => { e.StartSizeMin = a; e.StartSizeMax = b; });
            DrawRange("End Size", e.EndSizeMin, e.EndSizeMax, (a, b) => { e.EndSizeMin = a; e.EndSizeMax = b; });
            DrawVec3("Accel Start", e.AccelerationStart.ToSystemVector3(), v => e.AccelerationStart = v.ToOpenTKVector3());
            DrawVec3("Accel End", e.AccelerationEnd.ToSystemVector3(), v => e.AccelerationEnd = v.ToOpenTKVector3());
            DrawRange("Rot Speed", e.RotationSpeedMin, e.RotationSpeedMax, (a, b) => { e.RotationSpeedMin = a; e.RotationSpeedMax = b; });
            DrawColor("Start Color", e.ColorStart.ToSystemVector4(), c => e.ColorStart = c.ToOpenTKVector4());
            DrawColor("End Color", e.ColorEnd.ToSystemVector4(), c => e.ColorEnd = c.ToOpenTKVector4());
            ImGui.Separator();
        }

        static void DrawEmitterSpecific(ParticleSystem ps)
        {
            var e = ps.Emitter;
            switch (e)
            {
                case BoxEmitter b:
                    DrawVec3("Center", b.Center.ToSystemVector3(), v => b.Center = v.ToOpenTKVector3());
                    DrawVec3("Size", b.Size.ToSystemVector3(), v => b.Size = v.ToOpenTKVector3());
                    DrawVec3("Direction", b.Direction.ToSystemVector3(), v => b.Direction = v.ToOpenTKVector3());
                    break;

                case ConeEmitter c:
                    DrawVec3("Apex", c.Apex.ToSystemVector3(), v => c.Apex = v.ToOpenTKVector3());
                    DrawVec3("Axis", c.Axis.ToSystemVector3(), v => c.Axis = v.ToOpenTKVector3());
                    DrawFloat("Angle", c.Angle, v => c.Angle = v);
                    DrawFloat("Height", c.Height, v => c.Height = v);
                    DrawVec3("Direction", c.Direction.ToSystemVector3(), v => c.Direction = v.ToOpenTKVector3());
                    break;

                case DiskEmitter d:
                    DrawVec3("Center", d.Center.ToSystemVector3(), v => d.Center = v.ToOpenTKVector3());
                    DrawVec3("Normal", d.Normal.ToSystemVector3(), v => d.Normal = v.ToOpenTKVector3());
                    DrawFloat("Radius", d.Radius, v => d.Radius = v);
                    break;

                case PlaneEmitter p:
                    DrawVec3("Center", p.Center.ToSystemVector3(), v => p.Center = v.ToOpenTKVector3());
                    DrawVec3("Normal", p.Normal.ToSystemVector3(), v => p.Normal = v.ToOpenTKVector3());
                    DrawFloat("Width", p.Width, v => p.Width = v);
                    DrawFloat("Height", p.Height, v => p.Height = v);
                    DrawVec3("Direction", p.Direction.ToSystemVector3(), v => p.Direction = v.ToOpenTKVector3());
                    break;

                case SphereEmitter s:
                    DrawVec3("Center", s.Center.ToSystemVector3(), v => s.Center = v.ToOpenTKVector3());
                    DrawFloat("Radius", s.Radius, v => s.Radius = v);
                    break;

                case PointEmitter p2:
                    DrawVec3("Position", p2.Position.ToSystemVector3(), v => p2.Position = v.ToOpenTKVector3());
                    DrawVec3("Direction", p2.Direction.ToSystemVector3(), v => p2.Direction = v.ToOpenTKVector3());
                    break;
            }
        }
        static void DrawRange(string label, float a, float b, Action<float, float> set)
        {
            float full = ImGui.GetContentRegionAvail().X;
            ImGui.Columns(2, $"##rng{label}", false);
            ImGui.SetColumnWidth(0, full * .25f);
            ImGui.Text(label + " Min/Max");
            ImGui.NextColumn();
            if (ImGui.DragFloatRange2($"##{label}", ref a, ref b, .1f, 0, 0, "%.2f"))
                set(a, b);
            ImGui.Columns(1);
        }

        static void DrawColor(string label, Vector4 col, Action<Vector4> set)
        {
            float full = ImGui.GetContentRegionAvail().X;
            ImGui.Columns(2, $"##col{label}", false);
            ImGui.SetColumnWidth(0, full * .25f);
            ImGui.Text(label);
            ImGui.NextColumn();

            Vector4 c = col;
            Vector2 size = new(28, 28);
            ImGui.PushID(label);
            if (ImGui.ColorButton("##btn", c, ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.AlphaPreviewHalf, size))
                ImGui.OpenPopup("pick");
            if (ImGui.BeginPopup("pick"))
            {
                ImGui.SetNextWindowSize(new Vector2(250, 300), ImGuiCond.Appearing);
                if (ImGui.ColorPicker4("##picker", ref c, ImGuiColorEditFlags.PickerHueWheel))
                    set(c);
                ImGui.EndPopup();
            }
            ImGui.PopID();
            if (!ImGui.IsPopupOpen("pick") && c != col) set(c);
            ImGui.Columns(1);
        }

        static void GenericRow<T>(string label, ref T val,
                                  Func<string,  T, T> widget,
                                  Action<T> setter)
        {
            float full = ImGui.GetContentRegionAvail().X;
            ImGui.Columns(2, $"##gen{label}", false);
            ImGui.SetColumnWidth(0, full * .25f);
            ImGui.Text(label); ImGui.NextColumn();
            ImGui.PushItemWidth(-1);
            var tmp = val;
            val = widget($"##{label}",  tmp);
            ImGui.PopItemWidth();
            ImGui.Columns(1);
            setter(val);
        }
        #endregion

        static string GenerateEmitterCode(EmitterBase e, string varName)
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
