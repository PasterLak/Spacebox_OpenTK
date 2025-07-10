using System;
using System.Numerics;
using System.Reflection;
using ImGuiNET;

namespace Engine
{
    public static class ParticleSystemUI
    {
        private static readonly string[] _emitterNames = { "Box", "Cone", "Disk", "Plane", "Sphere" };
        private static readonly EmitterBase[] _emitterPrototypes =
        {
            new BoxEmitter(),
            new ConeEmitter(),
            new DiskEmitter(),
            new PlaneEmitter(),
            new SphereEmitter()
        };
        private static int _emitterIndex = -1;

        public static void ShowParticleSystemEditor(ParticleSystem ps, OrbitalCamera camera)
        {
            var io = ImGui.GetIO();
            var style = ImGui.GetStyle();
            float windowWidth = io.DisplaySize.X * 0.25f;
            float contentWidth = windowWidth - style.WindowPadding.X * 2;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));
            ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, 18f);
            ImGui.SetNextWindowPos(new Vector2(io.DisplaySize.X - windowWidth, 0), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, io.DisplaySize.Y), ImGuiCond.Always);
            ImGui.Begin("Particle System", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysVerticalScrollbar);

            bool overUI = ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows);
            camera.CanMove = !overUI;

            Vector3 pos = ps.Position.ToSystemVector3();
            if (ImGui.DragFloat3("Position", ref pos, 0.1f)) ps.Position = pos.ToOpenTKVector3();

            Vector3 rot = ps.Rotation.ToSystemVector3();
            if (ImGui.DragFloat3("Rotation", ref rot, 1f)) ps.Rotation = rot.ToOpenTKVector3();

            Vector3 scl = ps.Scale.ToSystemVector3();
            if (ImGui.DragFloat3("Scale", ref scl, 0.1f)) ps.Scale = scl.ToOpenTKVector3();

            ImGui.Separator();

            ImGui.Columns(2, "ps_cols", false);
            ImGui.SetColumnWidth(0, contentWidth * 0.5f);
            ImGui.SetColumnWidth(1, contentWidth * 0.5f);
            ImGui.PushItemWidth(-1);

            ImGui.Text("Max Particles"); ImGui.NextColumn();
            int max = ps.Max;
            if (ImGui.InputInt("##max", ref max)) ps.Max = max;
            ImGui.NextColumn();

            ImGui.Text("Emission Rate"); ImGui.NextColumn();
            int rate = (int)ps.Rate;
            if (ImGui.InputInt("##rate", ref rate)) ps.Rate = rate;
            ImGui.NextColumn();

            ImGui.Text("World Space"); ImGui.NextColumn();
            bool world = ps.Space == SimulationSpace.World;
            if (ImGui.Checkbox("##world", ref world)) ps.Space = world ? SimulationSpace.World : SimulationSpace.Local;
            ImGui.NextColumn();

            ImGui.Text("Emitter Type"); ImGui.NextColumn();
            int idx = _emitterIndex < 0 ? Array.IndexOf(_emitterPrototypes, ps.Emitter) : _emitterIndex;
            if (ImGui.Combo("##etype", ref idx, _emitterNames, _emitterNames.Length) && idx != _emitterIndex)
            {
                var old = ps.Emitter;
                var neu = _emitterPrototypes[idx];
                neu.CopyFrom(old);
                ps.SetEmitter(neu, false);
                _emitterIndex = idx;
            }
            ImGui.NextColumn();

            var emitter = ps.Emitter;

            ImGui.Text("Speed Min/Max"); ImGui.NextColumn();
            float spMin = emitter.SpeedMin, spMax = emitter.SpeedMax;
            if (ImGui.DragFloatRange2("##speed", ref spMin, ref spMax, 0.1f)) { emitter.SpeedMin = spMin; emitter.SpeedMax = spMax; }
            ImGui.NextColumn();

            ImGui.Text("Life Min/Max"); ImGui.NextColumn();
            float liMin = emitter.LifeMin, liMax = emitter.LifeMax;
            if (ImGui.DragFloatRange2("##life", ref liMin, ref liMax, 0.1f)) { emitter.LifeMin = liMin; emitter.LifeMax = liMax; }
            ImGui.NextColumn();

            ImGui.Text("Size Min/Max"); ImGui.NextColumn();
            float szMin = emitter.SizeMin, szMax = emitter.SizeMax;
            if (ImGui.DragFloatRange2("##size", ref szMin, ref szMax, 0.01f)) { emitter.SizeMin = szMin; emitter.SizeMax = szMax; }
            ImGui.NextColumn();

            ImGui.Text("Start Color"); ImGui.NextColumn();
            var colStart = emitter.ColorStart.ToSystemVector4();
            if (ImGui.ColorPicker4("##cstart", ref colStart)) emitter.ColorStart = colStart.ToOpenTKVector4();
            ImGui.NextColumn();

            ImGui.Text("End Color"); ImGui.NextColumn();
            var colEnd = emitter.ColorEnd.ToSystemVector4();
            if (ImGui.ColorPicker4("##cend", ref colEnd)) emitter.ColorEnd = colEnd.ToOpenTKVector4();
            ImGui.NextColumn();

            int typeIdx = Array.IndexOf(_emitterPrototypes, emitter);
            switch (typeIdx)
            {
                case 0:
                    ImGui.Text("Box Center"); ImGui.NextColumn();
                    var box = (BoxEmitter)emitter;
                    var bc = box.Center.ToSystemVector3();
                    if (ImGui.DragFloat3("##box_center", ref bc)) box.Center = bc.ToOpenTKVector3();
                    ImGui.NextColumn();
                    ImGui.Text("Box Size"); ImGui.NextColumn();
                    var bs = box.Size.ToSystemVector3();
                    if (ImGui.DragFloat3("##box_size", ref bs)) box.Size = bs.ToOpenTKVector3();
                    ImGui.NextColumn();
                    ImGui.Text("Direction"); ImGui.NextColumn();
                    var bd = box.Direction.ToSystemVector3();
                    if (ImGui.DragFloat3("##box_dir", ref bd)) box.Direction = bd.ToOpenTKVector3();
                    ImGui.NextColumn();
                    break;
                case 1:
                    ImGui.Text("Apex"); ImGui.NextColumn();
                    var cone = (ConeEmitter)emitter;
                    var ca = cone.Apex.ToSystemVector3();
                    if (ImGui.DragFloat3("##cone_apex", ref ca)) cone.Apex = ca.ToOpenTKVector3();
                    ImGui.NextColumn();
                    ImGui.Text("Axis"); ImGui.NextColumn();
                    var cx = cone.Axis.ToSystemVector3();
                    if (ImGui.DragFloat3("##cone_axis", ref cx)) cone.Axis = cx.ToOpenTKVector3();
                    ImGui.NextColumn();
                    ImGui.Text("Angle"); ImGui.NextColumn();
                    float ang = cone.Angle;
                    if (ImGui.DragFloat("##cone_angle", ref ang)) cone.Angle = ang;
                    ImGui.NextColumn();
                    ImGui.Text("Height"); ImGui.NextColumn();
                    float ch = cone.Height;
                    if (ImGui.DragFloat("##cone_height", ref ch)) cone.Height = ch;
                    ImGui.NextColumn();
                    break;
                case 2:
                    ImGui.Text("Center"); ImGui.NextColumn();
                    var disk = (DiskEmitter)emitter;
                    var dc = disk.Center.ToSystemVector3();
                    if (ImGui.DragFloat3("##disk_center", ref dc)) disk.Center = dc.ToOpenTKVector3();
                    ImGui.NextColumn();
                    ImGui.Text("Normal"); ImGui.NextColumn();
                    var dn = disk.Normal.ToSystemVector3();
                    if (ImGui.DragFloat3("##disk_normal", ref dn)) disk.Normal = dn.ToOpenTKVector3();
                    ImGui.NextColumn();
                    ImGui.Text("Radius"); ImGui.NextColumn();
                    float dr = disk.Radius;
                    if (ImGui.DragFloat("##disk_radius", ref dr)) disk.Radius = dr;
                    ImGui.NextColumn();
                    break;
                case 3:
                    ImGui.Text("Center"); ImGui.NextColumn();
                    var plane = (PlaneEmitter)emitter;
                    var pc = plane.Center.ToSystemVector3();
                    if (ImGui.DragFloat3("##plane_center", ref pc)) plane.Center = pc.ToOpenTKVector3();
                    ImGui.NextColumn();
                    ImGui.Text("Normal"); ImGui.NextColumn();
                    var pn = plane.Normal.ToSystemVector3();
                    if (ImGui.DragFloat3("##plane_normal", ref pn)) plane.Normal = pn.ToOpenTKVector3();
                    ImGui.NextColumn();
                    ImGui.Text("Width"); ImGui.NextColumn();
                    float pw = plane.Width;
                    if (ImGui.DragFloat("##plane_width", ref pw)) plane.Width = pw;
                    ImGui.NextColumn();
                    ImGui.Text("Height"); ImGui.NextColumn();
                    float ph = plane.Height;
                    if (ImGui.DragFloat("##plane_height", ref ph)) plane.Height = ph;
                    ImGui.NextColumn();
                    ImGui.Text("Direction"); ImGui.NextColumn();
                    var pd = plane.Direction.ToSystemVector3();
                    if (ImGui.DragFloat3("##plane_dir", ref pd)) plane.Direction = pd.ToOpenTKVector3();
                    ImGui.NextColumn();
                    break;
                case 4:
                    ImGui.Text("Center"); ImGui.NextColumn();
                    var sphere = (SphereEmitter)emitter;
                    var sc = sphere.Center.ToSystemVector3();
                    if (ImGui.DragFloat3("##sphere_center", ref sc)) sphere.Center = sc.ToOpenTKVector3();
                    ImGui.NextColumn();
                    ImGui.Text("Radius"); ImGui.NextColumn();
                    float sr = sphere.Radius;
                    if (ImGui.DragFloat("##sphere_radius", ref sr)) sphere.Radius = sr;
                    ImGui.NextColumn();
                    break;
            }

            ImGui.Columns(1);

            if (ImGui.Button("Clear Particles")) ps.ClearParticles();

            ImGui.PopItemWidth();
            ImGui.End();
            ImGui.PopStyleVar(2);
        }
    }
}
