using System;
using System.Numerics;
using System.Text;
using ImGuiNET;

namespace Engine
{
    public static class ParticleSystemUI
    {
        private static int _activeIndex = 0;
        private static readonly string[] _emitterNames = { "Box", "Cone", "Disk", "Plane", "Sphere" };
        private static readonly Type[] _emitterTypes =
        {
            typeof(BoxEmitter),
            typeof(ConeEmitter),
            typeof(DiskEmitter),
            typeof(PlaneEmitter),
            typeof(SphereEmitter)
        };

        public static void Show(ParticleSystem[] systems, OrbitalCamera camera)
        {
            if (systems == null || systems.Length == 0) return;
            _activeIndex = Math.Clamp(_activeIndex, 0, systems.Length - 1);
            var ps = systems[_activeIndex];
            if (ps == null) return;

            var io = ImGui.GetIO();
            var style = ImGui.GetStyle();
            float ww = io.DisplaySize.X * 0.25f;
            float contentW = ww - style.WindowPadding.X * 2;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));
            ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, 18f);
            ImGui.SetNextWindowPos(new Vector2(io.DisplaySize.X - ww, 0), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(ww, io.DisplaySize.Y), ImGuiCond.Always);
            ImGui.Begin("Particle Systems", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysVerticalScrollbar);

            // system tabs
            for (int i = 0; i < systems.Length; i++)
            {
                if (i > 0) ImGui.SameLine();
                if (ImGui.Button($"System {i + 1}")) _activeIndex = i;
            }
            ImGui.Separator();

            // block camera
            bool overUI = ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows) || ImGui.IsAnyItemActive();
            camera.CanMove = !overUI;

            // enable / restart
            bool enabled = ps.Enabled;
            if (ImGui.Checkbox("Enabled", ref enabled)) ps.Enabled = enabled;
            ImGui.SameLine();
            if (ImGui.Button("Restart")) ps.Restart();
            ImGui.Separator();

            var pos = ps.Position.ToSystemVector3();
            ImGui.Text("Position"); ImGui.Separator();

            float totalW = io.DisplaySize.X * 0.25f - style.WindowPadding.X * 2;
            float colW = totalW / 4f;

            ImGui.Columns(4, "transform_cols", false);

            ImGui.SetColumnWidth(0, colW);
            ImGui.Text("Position");
            ImGui.NextColumn();

            ImGui.SetColumnWidth(1, colW);
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "X"); ImGui.SameLine();
            ImGui.SetNextItemWidth(colW - ImGui.CalcTextSize("X").X - style.ItemSpacing.X);
            ImGui.DragFloat("##posX", ref pos.X, 0.1f, 0f, 0f, "%.2f");
            ImGui.NextColumn();

            ImGui.SetColumnWidth(2, colW);
            ImGui.TextColored(new Vector4(0, 1, 0, 1), "Y"); ImGui.SameLine();
            ImGui.SetNextItemWidth(colW - ImGui.CalcTextSize("Y").X - style.ItemSpacing.X);
            ImGui.DragFloat("##posY", ref pos.Y, 0.1f, 0f, 0f, "%.2f");
            ImGui.NextColumn();

            ImGui.SetColumnWidth(3, colW);
            ImGui.TextColored(new Vector4(0, 0, 1, 1), "Z"); ImGui.SameLine();
            ImGui.SetNextItemWidth(colW - ImGui.CalcTextSize("Z").X - style.ItemSpacing.X);
            ImGui.DragFloat("##posZ", ref pos.Z, 0.1f, 0f, 0f, "%.2f");
            ImGui.NextColumn();

            ImGui.Columns(1);

            // Rotation
            var rot = ps.Rotation.ToSystemVector3();
            {



                ImGui.Columns(4, "rot_cols", false);

                ImGui.SetColumnWidth(0, colW);
                ImGui.Text("Rotation");
                ImGui.NextColumn();

                ImGui.SetColumnWidth(1, colW);
                ImGui.TextColored(new Vector4(1, 0, 0, 1), "X"); ImGui.SameLine();
                ImGui.SetNextItemWidth(colW - ImGui.CalcTextSize("X").X - style.ItemSpacing.X);
                if (ImGui.DragFloat("##rotX", ref rot.X, 1f, 0f, 0f, "%.2f")) ps.Rotation = rot.ToOpenTKVector3();
                ImGui.NextColumn();

                ImGui.SetColumnWidth(2, colW);
                ImGui.TextColored(new Vector4(0, 1, 0, 1), "Y"); ImGui.SameLine();
                ImGui.SetNextItemWidth(colW - ImGui.CalcTextSize("Y").X - style.ItemSpacing.X);
                if (ImGui.DragFloat("##rotY", ref rot.Y, 1f, 0f, 0f, "%.2f")) ps.Rotation = rot.ToOpenTKVector3();
                ImGui.NextColumn();

                ImGui.SetColumnWidth(3, colW);
                ImGui.TextColored(new Vector4(0, 0, 1, 1), "Z"); ImGui.SameLine();
                ImGui.SetNextItemWidth(colW - ImGui.CalcTextSize("Z").X - style.ItemSpacing.X);
                if (ImGui.DragFloat("##rotZ", ref rot.Z, 1f, 0f, 0f, "%.2f")) ps.Rotation = rot.ToOpenTKVector3();
                ImGui.NextColumn();

                ImGui.Columns(1);
            }

            // Scale
            var scl = ps.Scale.ToSystemVector3();
            {



                ImGui.Columns(4, "scl_cols", false);

                ImGui.SetColumnWidth(0, colW);
                ImGui.Text("Scale");
                ImGui.NextColumn();

                ImGui.SetColumnWidth(1, colW);
                ImGui.TextColored(new Vector4(1, 0, 0, 1), "X"); ImGui.SameLine();
                ImGui.SetNextItemWidth(colW - ImGui.CalcTextSize("X").X - style.ItemSpacing.X);
                if (ImGui.DragFloat("##sclX", ref scl.X, 0.1f, 0f, 0f, "%.2f")) ps.Scale = scl.ToOpenTKVector3();
                ImGui.NextColumn();

                ImGui.SetColumnWidth(2, colW);
                ImGui.TextColored(new Vector4(0, 1, 0, 1), "Y"); ImGui.SameLine();
                ImGui.SetNextItemWidth(colW - ImGui.CalcTextSize("Y").X - style.ItemSpacing.X);
                if (ImGui.DragFloat("##sclY", ref scl.Y, 0.1f, 0f, 0f, "%.2f")) ps.Scale = scl.ToOpenTKVector3();
                ImGui.NextColumn();

                ImGui.SetColumnWidth(3, colW);
                ImGui.TextColored(new Vector4(0, 0, 1, 1), "Z"); ImGui.SameLine();
                ImGui.SetNextItemWidth(colW - ImGui.CalcTextSize("Z").X - style.ItemSpacing.X);
                if (ImGui.DragFloat("##sclZ", ref scl.Z, 0.1f, 0f, 0f, "%.2f")) ps.Scale = scl.ToOpenTKVector3();
                ImGui.NextColumn();

                ImGui.Columns(1);
            }



            // grid
            ImGui.Columns(2, "cols", false);
            ImGui.SetColumnWidth(0, contentW * 0.5f);
            ImGui.SetColumnWidth(1, contentW * 0.5f);
            ImGui.PushItemWidth(-1);

            ImGui.Text("Max Particles"); ImGui.NextColumn();
            int max = ps.Max;
            if (ImGui.InputInt("##max", ref max)) ps.Max = Math.Max(0, max);
            ImGui.NextColumn();

            ImGui.Text($"Active: {ps.ParticlesCount}"); ImGui.NextColumn();
            if (ImGui.Button("Clear")) ps.ClearParticles();
            ImGui.NextColumn();

            ImGui.Text("Emission Rate"); ImGui.NextColumn();
            float rate = ps.Rate;
            if (ImGui.InputFloat("##rate", ref rate, 1f, 10f, "%.2f")) ps.Rate = rate;
            ImGui.NextColumn();

            ImGui.Text("Simulation Speed"); ImGui.NextColumn();
            float sim = ps.SimulationSpeed;
            if (ImGui.InputFloat("##sim", ref sim, 0.1f, 1f, "%.2f")) ps.SimulationSpeed = sim;
            ImGui.NextColumn();

            ImGui.Text("Duration"); ImGui.NextColumn();
            float dur = ps.Duration;
            if (ImGui.InputFloat("##dur", ref dur, 1f, 10f, "%.2f")) ps.Duration = Math.Max(0, dur);
            ImGui.NextColumn();

            ImGui.Text("Loop"); ImGui.NextColumn();
            bool loop = ps.Loop;
            if (ImGui.Checkbox("##loop", ref loop)) ps.Loop = loop;
            ImGui.NextColumn();

            ImGui.Text("World Space"); ImGui.NextColumn();
            bool ws = ps.Space == SimulationSpace.World;
            if (ImGui.Checkbox("##ws", ref ws)) ps.Space = ws ? SimulationSpace.World : SimulationSpace.Local;
            ImGui.NextColumn();

            ImGui.Text("Emitter Type"); ImGui.NextColumn();
            int idx = Array.FindIndex(_emitterTypes, t => t == ps.Emitter.GetType());
            if (ImGui.Combo("##etype", ref idx, _emitterNames, _emitterNames.Length) && idx >= 0)
            {
                var newEmitter = (EmitterBase)Activator.CreateInstance(_emitterTypes[idx])!;
                newEmitter.CopyFrom(ps.Emitter);
                ps.SetEmitter(newEmitter, false);
            }
            ImGui.NextColumn();

            // emitter common
            var e = ps.Emitter;

            ImGui.Text("Speed Min/Max"); ImGui.NextColumn();
            float s0 = e.SpeedMin, s1 = e.SpeedMax;
            if (ImGui.DragFloatRange2("##s", ref s0, ref s1, 0.1f, 0f, 0f, "%.2f")) { e.SpeedMin = s0; e.SpeedMax = s1; }
            ImGui.NextColumn();

            ImGui.Text("Life Min/Max"); ImGui.NextColumn();
            float l0 = e.LifeMin, l1 = e.LifeMax;
            if (ImGui.DragFloatRange2("##l", ref l0, ref l1, 0.1f, 0f, 0f, "%.2f")) { e.LifeMin = l0; e.LifeMax = l1; }
            ImGui.NextColumn();

            ImGui.Text("Size Start Min/Max"); ImGui.NextColumn();
            float z0 = e.StartSizeMin, z1 = e.StartSizeMax;
            if (ImGui.DragFloatRange2("##z0", ref z0, ref z1, 0.01f, 0f, 0f, "%.2f")) { e.StartSizeMin = z0; e.StartSizeMax = z1; }
            ImGui.NextColumn();

            ImGui.Text("Size End Min/Max"); ImGui.NextColumn();
            float z2 = e.EndSizeMin, z3 = e.EndSizeMax;
            if (ImGui.DragFloatRange2("##z1", ref z2, ref z3, 0.01f, 0f, 0f, "%.2f")) { e.EndSizeMin = z2; e.EndSizeMax = z3; }
            ImGui.NextColumn();

            ImGui.Text("Accel Start"); ImGui.NextColumn();
            var a0 = e.AccelerationStart.ToSystemVector3();
            if (ImGui.DragFloat3("##a0", ref a0, 0.1f, 0f, 0f, "%.2f")) e.AccelerationStart = a0.ToOpenTKVector3();
            ImGui.NextColumn();

            ImGui.Text("Accel End"); ImGui.NextColumn();
            var a1 = e.AccelerationEnd.ToSystemVector3();
            if (ImGui.DragFloat3("##a1", ref a1, 0.1f, 0f, 0f, "%.2f")) e.AccelerationEnd = a1.ToOpenTKVector3();
            ImGui.NextColumn();

            ImGui.Text("Rot Speed Min/Max"); ImGui.NextColumn();
            float r0 = e.RotationSpeedMin, r1 = e.RotationSpeedMax;
            if (ImGui.DragFloatRange2("##r", ref r0, ref r1, 0.1f, 0f, 0f, "%.2f")) { e.RotationSpeedMin = r0; e.RotationSpeedMax = r1; }
            ImGui.NextColumn();

            ImGui.Separator();
            ImGui.Text("Start Color"); ImGui.NextColumn();
            var c0 = e.ColorStart.ToSystemVector4();
            if (ImGui.ColorPicker4("##c0", ref c0, ImGuiColorEditFlags.PickerHueWheel)) e.ColorStart = c0.ToOpenTKVector4();
            ImGui.NextColumn();

            ImGui.Text("End Color"); ImGui.NextColumn();
            var c1 = e.ColorEnd.ToSystemVector4();
            if (ImGui.ColorPicker4("##c1", ref c1, ImGuiColorEditFlags.PickerHueWheel)) e.ColorEnd = c1.ToOpenTKVector4();
            ImGui.NextColumn();

            ImGui.Separator();
            int tIdx = Array.FindIndex(_emitterTypes, tp => tp == e.GetType());
            switch (tIdx)
            {
                case 0:
                    ImGui.Text("Box Center"); ImGui.NextColumn();
                    var b0 = ((BoxEmitter)e).Center.ToSystemVector3();
                    if (ImGui.DragFloat3("##b0", ref b0, 0.1f, 0f, 0f, "%.2f")) ((BoxEmitter)e).Center = b0.ToOpenTKVector3();
                    ImGui.NextColumn();

                    ImGui.Text("Box Size"); ImGui.NextColumn();
                    var b1 = ((BoxEmitter)e).Size.ToSystemVector3();
                    if (ImGui.DragFloat3("##b1", ref b1, 0.1f, 0f, 0f, "%.2f")) ((BoxEmitter)e).Size = b1.ToOpenTKVector3();
                    ImGui.NextColumn();

                    ImGui.Text("Direction"); ImGui.NextColumn();
                    var b2 = ((BoxEmitter)e).Direction.ToSystemVector3();
                    if (ImGui.DragFloat3("##b2", ref b2, 0.1f, 0f, 0f, "%.2f")) ((BoxEmitter)e).Direction = b2.ToOpenTKVector3();
                    ImGui.NextColumn();
                    break;

                case 1:
                    ImGui.Text("Cone Apex"); ImGui.NextColumn();
                    var cA = ((ConeEmitter)e).Apex.ToSystemVector3();
                    if (ImGui.DragFloat3("##cA", ref cA, 0.1f, 0f, 0f, "%.2f")) ((ConeEmitter)e).Apex = cA.ToOpenTKVector3();
                    ImGui.NextColumn();

                    ImGui.Text("Cone Axis"); ImGui.NextColumn();
                    var cB = ((ConeEmitter)e).Axis.ToSystemVector3();
                    if (ImGui.DragFloat3("##cB", ref cB, 0.1f, 0f, 0f, "%.2f")) ((ConeEmitter)e).Axis = cB.ToOpenTKVector3();
                    ImGui.NextColumn();

                    ImGui.Text("Angle"); ImGui.NextColumn();
                    float cC = ((ConeEmitter)e).Angle;
                    if (ImGui.DragFloat("##cC", ref cC, 0.1f, 0f, 0f, "%.2f")) ((ConeEmitter)e).Angle = cC;
                    ImGui.NextColumn();

                    ImGui.Text("Height"); ImGui.NextColumn();
                    float cD = ((ConeEmitter)e).Height;
                    if (ImGui.DragFloat("##cD", ref cD, 0.1f, 0f, 0f, "%.2f")) ((ConeEmitter)e).Height = cD;
                    ImGui.NextColumn();
                    break;

                case 2:
                    ImGui.Text("Disk Center"); ImGui.NextColumn();
                    var d0 = ((DiskEmitter)e).Center.ToSystemVector3();
                    if (ImGui.DragFloat3("##d0", ref d0, 0.1f, 0f, 0f, "%.2f")) ((DiskEmitter)e).Center = d0.ToOpenTKVector3();
                    ImGui.NextColumn();

                    ImGui.Text("Disk Normal"); ImGui.NextColumn();
                    var d1 = ((DiskEmitter)e).Normal.ToSystemVector3();
                    if (ImGui.DragFloat3("##d1", ref d1, 0.1f, 0f, 0f, "%.2f")) ((DiskEmitter)e).Normal = d1.ToOpenTKVector3();
                    ImGui.NextColumn();

                    ImGui.Text("Radius"); ImGui.NextColumn();
                    float d2 = ((DiskEmitter)e).Radius;
                    if (ImGui.DragFloat("##d2", ref d2, 0.1f, 0f, 0f, "%.2f")) ((DiskEmitter)e).Radius = d2;
                    ImGui.NextColumn();
                    break;

                case 3:
                    ImGui.Text("Plane Center"); ImGui.NextColumn();
                    var p0v = ((PlaneEmitter)e).Center.ToSystemVector3();
                    if (ImGui.DragFloat3("##p0v", ref p0v, 0.1f, 0f, 0f, "%.2f")) ((PlaneEmitter)e).Center = p0v.ToOpenTKVector3();
                    ImGui.NextColumn();

                    ImGui.Text("Plane Normal"); ImGui.NextColumn();
                    var p1v = ((PlaneEmitter)e).Normal.ToSystemVector3();
                    if (ImGui.DragFloat3("##p1v", ref p1v, 0.1f, 0f, 0f, "%.2f")) ((PlaneEmitter)e).Normal = p1v.ToOpenTKVector3();
                    ImGui.NextColumn();

                    ImGui.Text("Width"); ImGui.NextColumn();
                    float pW = ((PlaneEmitter)e).Width;
                    if (ImGui.DragFloat("##pW", ref pW, 0.1f, 0f, 0f, "%.2f")) ((PlaneEmitter)e).Width = pW;
                    ImGui.NextColumn();

                    ImGui.Text("Height"); ImGui.NextColumn();
                    float pH2 = ((PlaneEmitter)e).Height;
                    if (ImGui.DragFloat("##pH2", ref pH2, 0.1f, 0f, 0f, "%.2f")) ((PlaneEmitter)e).Height = pH2;
                    ImGui.NextColumn();

                    ImGui.Text("Direction"); ImGui.NextColumn();
                    var p2v = ((PlaneEmitter)e).Direction.ToSystemVector3();
                    if (ImGui.DragFloat3("##p2v", ref p2v, 0.1f, 0f, 0f, "%.2f")) ((PlaneEmitter)e).Direction = p2v.ToOpenTKVector3();
                    ImGui.NextColumn();
                    break;

                case 4:
                    ImGui.Text("Sphere Center"); ImGui.NextColumn();
                    var s0v = ((SphereEmitter)e).Center.ToSystemVector3();
                    if (ImGui.DragFloat3("##s0v", ref s0v, 0.1f, 0f, 0f, "%.2f")) ((SphereEmitter)e).Center = s0v.ToOpenTKVector3();
                    ImGui.NextColumn();

                    ImGui.Text("Radius"); ImGui.NextColumn();
                    float s1v = ((SphereEmitter)e).Radius;
                    if (ImGui.DragFloat("##s1v", ref s1v, 0.1f, 0f, 0f, "%.2f")) ((SphereEmitter)e).Radius = s1v;
                    ImGui.NextColumn();
                    break;
            }
            ImGui.Separator();
            if (ImGui.Button("Generate Emitter Code"))
            {
                var code = GenerateEmitterCode(ps.Emitter, $"emitter{_activeIndex + 1}");
                ImGui.SetClipboardText(code);
            }
            ImGui.Columns(1);
            ImGui.PopItemWidth();
            ImGui.End();
            ImGui.PopStyleVar(2);
        }


        public static string GenerateEmitterCode(EmitterBase e, string varName)
        {
            var sb = new StringBuilder();
            var type = e.GetType().Name;
            sb.AppendLine($"var {varName} = new {type}");
            sb.AppendLine("{");
            sb.AppendLine($"    SpeedMin = {e.SpeedMin}f,");
            sb.AppendLine($"    SpeedMax = {e.SpeedMax}f,");
            sb.AppendLine($"    LifeMin = {e.LifeMin}f,");
            sb.AppendLine($"    LifeMax = {e.LifeMax}f,");
            sb.AppendLine($"    StartSizeMin = {e.StartSizeMin}f,");
            sb.AppendLine($"    StartSizeMax = {e.StartSizeMax}f,");
            sb.AppendLine($"    EndSizeMin = {e.EndSizeMin}f,");
            sb.AppendLine($"    EndSizeMax = {e.EndSizeMax}f,");
            sb.AppendLine($"    AccelerationStart = new Vector3({e.AccelerationStart.X}f, {e.AccelerationStart.Y}f, {e.AccelerationStart.Z}f),");
            sb.AppendLine($"    AccelerationEnd = new Vector3({e.AccelerationEnd.X}f, {e.AccelerationEnd.Y}f, {e.AccelerationEnd.Z}f),");
            sb.AppendLine($"    RotationSpeedMin = {e.RotationSpeedMin}f,");
            sb.AppendLine($"    RotationSpeedMax = {e.RotationSpeedMax}f,");

            sb.AppendLine($"    ColorStart = new Vector4({e.ColorStart.X}f, {e.ColorStart.Y}f, {e.ColorStart.Z}f, {e.ColorStart.W}f),");
            sb.AppendLine($"    ColorEnd = new Vector4({e.ColorEnd.X}f, {e.ColorEnd.Y}f, {e.ColorEnd.Z}f, {e.ColorEnd.W}f),");

              switch (e)
            {
                case BoxEmitter be:
                    sb.AppendLine($"    Center = new Vector3({be.Center.X}f, {be.Center.Y}f, {be.Center.Z}f),");
                    sb.AppendLine($"    Size = new Vector3({be.Size.X}f, {be.Size.Y}f, {be.Size.Z}f),");
                    sb.AppendLine($"    Direction = new Vector3({be.Direction.X}f, {be.Direction.Y}f, {be.Direction.Z}f),");
                    break;
                case ConeEmitter ce:
                    sb.AppendLine($"    Apex = new Vector3({ce.Apex.X}f, {ce.Apex.Y}f, {ce.Apex.Z}f),");
                    sb.AppendLine($"    Axis = new Vector3({ce.Axis.X}f, {ce.Axis.Y}f, {ce.Axis.Z}f),");
                    sb.AppendLine($"    Angle = {ce.Angle}f,");
                    sb.AppendLine($"    Height = {ce.Height}f,");
                    break;
                case DiskEmitter de:
                    sb.AppendLine($"    Center = new Vector3({de.Center.X}f, {de.Center.Y}f, {de.Center.Z}f),");
                    sb.AppendLine($"    Normal = new Vector3({de.Normal.X}f, {de.Normal.Y}f, {de.Normal.Z}f),");
                    sb.AppendLine($"    Radius = {de.Radius}f,");
                    break;
                case PlaneEmitter pe:
                    sb.AppendLine($"    Center = new Vector3({pe.Center.X}f, {pe.Center.Y}f, {pe.Center.Z}f),");
                    sb.AppendLine($"    Normal = new Vector3({pe.Normal.X}f, {pe.Normal.Y}f, {pe.Normal.Z}f),");
                    sb.AppendLine($"    Width = {pe.Width}f,");
                    sb.AppendLine($"    Height = {pe.Height}f,");
                    sb.AppendLine($"    Direction = new Vector3({pe.Direction.X}f, {pe.Direction.Y}f, {pe.Direction.Z}f),");
                    break;
                case SphereEmitter se:
                    sb.AppendLine($"    Center = new Vector3({se.Center.X}f, {se.Center.Y}f, {se.Center.Z}f),");
                    sb.AppendLine($"    Radius = {se.Radius}f,");
                    break;
            }

            sb.AppendLine("};");
            return sb.ToString();
        }
    }
}
