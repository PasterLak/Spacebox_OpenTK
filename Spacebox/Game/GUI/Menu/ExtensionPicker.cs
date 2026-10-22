using System.Numerics;
using ImGuiNET;
using Spacebox.Game.Resource.Content;

namespace Spacebox.Game.GUI.Menu;

public sealed class ExtensionPicker
{
    private static readonly Vector4 ErrorColor = new Vector4(1f, 0.4f, 0.35f, 1f);
    private static readonly Vector4 InfoColor = new Vector4(0.6f, 0.8f, 1f, 1f);

    private readonly GameMenu _menu;

    public ExtensionPicker(GameMenu menu)
    {
        _menu = menu;
    }

    public bool HasErrors { get; private set; }

    public void Render(float x, float width, float height, bool readOnly)
    {
        var baseConfig = readOnly ? _menu.GetModById(_menu.selectedWorld?.ModId) : _menu.GameSets.Count > 0 ? _menu.GameSets[Math.Clamp(_menu.SelectedGameSetIndex, 0, _menu.GameSets.Count - 1)] : null;
        var available = _menu.ExtensionsFor(baseConfig);
        var selected = readOnly ? WorldGameSetCheck.ExtensionsOf(_menu.selectedWorld).ToList() : _menu.NewWorldExtensions;

        string label = "Extensions";
        ImGui.SetCursorPosX(x + (width - ImGui.CalcTextSize(label).X) * 0.5f);
        ImGui.Text(label);
        ImGui.SetCursorPosX(x);

        if (ImGui.BeginChild("##extensions", new Vector2(width, height), ImGuiChildFlags.Border))
        {
            if (available.Count == 0) ImGui.TextDisabled("No extensions installed for this GameSet");

            if (readOnly) ImGui.BeginDisabled();
            foreach (var extension in available)
            {
                bool enabled = Contains(selected, extension.Id);
                if (ImGui.Checkbox($"{extension.DisplayName}  v{extension.Config.Version}##{extension.Id}", ref enabled) && !readOnly)
                {
                    _menu.Click1.Play();
                    if (enabled) selected.Add(extension.Id);
                    else selected.RemoveAll(id => string.Equals(id, extension.Id, StringComparison.OrdinalIgnoreCase));
                }
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) ImGui.SetTooltip(Tooltip(extension));
            }
            if (readOnly) ImGui.EndDisabled();

            HasErrors = false;
            if (!readOnly && selected.Count > 0)
            {
                var resolution = _menu.ResolveNewWorldExtensions();
                HasErrors = !resolution.Success;
                if (HasErrors) ImGui.TextColored(ErrorColor, resolution.ErrorText);
                else ImGui.TextColored(InfoColor, "Load order: " + string.Join(" > ", resolution.Order.Select(e => e.Id)));
            }
            else if (readOnly)
            {
                ImGui.TextDisabled(selected.Count == 0 ? "This world uses no extensions" : "Extensions are chosen when the world is created");
            }
        }
        ImGui.EndChild();
    }

    private static string Tooltip(InstalledGameSet extension)
    {
        string text = string.IsNullOrWhiteSpace(extension.Config.Description) ? extension.Id : extension.Config.Description;
        if (extension.Includes.Count > 0) text += "\nIncludes: " + string.Join(", ", extension.Includes);
        return text + "\nSaved with the world, cannot be changed later";
    }

    private static bool Contains(List<string> ids, string id)
    {
        return ids.Exists(existing => string.Equals(existing, id, StringComparison.OrdinalIgnoreCase));
    }
}
