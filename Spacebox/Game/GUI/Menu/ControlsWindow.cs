using Engine;
using Engine.InputPro;
using ImGuiNET;
using System.Numerics;
using System.Linq;

namespace Spacebox.Game.GUI.Menu;

public class ControlsWindow : MenuWindow
{
    private GameMenu menu;
    private InputRemapper remapper;
    private string remappingAction = null;
    private bool isRemapping = false;
    private float remapTimeout = 0f;
    private const float MAX_REMAP_TIME = 10f;
    private List<string> sameKeys = new List<string>();

    public ControlsWindow(GameMenu menu)
    {
        this.menu = menu;
        remapper = new InputRemapper();
        InputManager.Instance.LoadConfiguration("Resources/default_input.json");
    }

    public override void Render()
    {
        SettingsUI.Render("Controls", "Controls", 5,
            (listSize, rowH) =>
            {
                RenderControlsTable(listSize, rowH);
            },
            () =>
            {
                menu.Click1.Play();
                InputManager.Instance.SaveConfiguration("Resources/default_input.json");
                menu.SetStateToOptions();
            },
            () =>
            {
                menu.Click1.Play();
                InputManager.Instance.LoadConfiguration("Resources/default_input.json");
                menu.SetStateToOptions();
            }
        );

        if (isRemapping)
        {
            RenderRemapOverlay();
        }
    }

    public override void OnWindowChanged()
    {
        sameKeys = ValidateSameBindings();
    }

    private void RenderControlsTable(Vector2 listSize, float rowH)
    {
        float totalW = listSize.X;
        float col1 = totalW * 0.22f;
        float col2 = totalW * 0.40f;
        float col3 = totalW * 0.20f;
        float col4 = totalW * 0.15f;

        if (ImGui.BeginTable("header_table", 4, ImGuiTableFlags.BordersInnerV))
        {
            ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed, col1);
            ImGui.TableSetupColumn("Description", ImGuiTableColumnFlags.WidthFixed, col2);
            ImGui.TableSetupColumn("Key", ImGuiTableColumnFlags.WidthFixed, col3);
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, col4);
            ImGui.TableHeadersRow();
            ImGui.EndTable();
        }

        var actions = InputManager.Instance.GetAllActions();
        var groupedActions = actions
            .Select(x => new { x.id, x.action })
            .GroupBy(x => string.IsNullOrEmpty(x.action.Category) ? "General" : x.action.Category)
            .OrderBy(g => g.Key);

        foreach (var group in groupedActions)
        {
            ImGui.Dummy(new Vector2(0, 5));
            ImGui.TextColored(new Vector4(1f, 0.8f, 0.2f, 1f), $"--- {group.Key.ToUpper()} ---");

            if (ImGui.BeginTable("table_" + group.Key, 4, ImGuiTableFlags.BordersInnerV))
            {
                ImGui.TableSetupColumn("##c1", ImGuiTableColumnFlags.WidthFixed, col1);
                ImGui.TableSetupColumn("##c2", ImGuiTableColumnFlags.WidthFixed, col2);
                ImGui.TableSetupColumn("##c3", ImGuiTableColumnFlags.WidthFixed, col3);
                ImGui.TableSetupColumn("##c4", ImGuiTableColumnFlags.WidthFixed, col4);

                foreach (var item in group)
                {
                    string id = item.id;
                    InputAction action = item.action;

                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.Text(action.Name);

                    ImGui.TableNextColumn();
                    ImGui.TextWrapped(action.Description);

                    ImGui.TableNextColumn();
                    string bindingText = GetBindingDisplayText(action);

                    if (sameKeys.Contains(bindingText))
                        ImGui.TextColored(new Vector4(1, 0.5f, 0, 1), bindingText);
                    else
                        ImGui.Text(bindingText);

                    ImGui.TableNextColumn();

                    if (isRemapping && remappingAction == id)
                    {
                        ImGui.TextColored(new Vector4(1, 1, 0, 1), "Listening...");
                    }
                    else
                    {
                        ImGui.Dummy(new Vector2(rowH, rowH / 2f)); ImGui.SameLine();
                        if (ImGui.Button($"Change##{action.Name} "))
                        {
                            StartRemapping(id);
                        }
                    }
                }
                ImGui.EndTable();
            }
        }
    }

    private string GetBindingDisplayText(Engine.InputPro.InputAction action)
    {
        if (action.Bindings.Count == 0)
            return "None";

        var displayNames = action.Bindings.Select(b => b.GetDisplayName());
        return string.Join(" / ", displayNames);
    }

    private List<string> ValidateSameBindings()
    {
        var actions = InputManager.Instance.GetAllActions();

        var same = new List<string>();
        var checkedBindings = new List<string>();

        foreach (var action in actions)
        {
            string bindingText = GetBindingDisplayText(action.action);

            if (bindingText == "None") continue;

            if (checkedBindings.Contains(bindingText))
            {
                same.Add(bindingText);
            }
            else
            {
                checkedBindings.Add(bindingText);
            }
        }

        return same;
    }

    private void StartRemapping(string actionID)
    {
        remappingAction = actionID;
        isRemapping = true;
        remapTimeout = MAX_REMAP_TIME;
        remapper.StartRemapping(actionID);
    }

    private void RenderRemapOverlay()
    {
        var io = ImGui.GetIO();
        var displaySize = io.DisplaySize;

        ImGui.SetNextWindowPos(Vector2.Zero);
        ImGui.SetNextWindowSize(displaySize);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 0.7f));
        ImGui.Begin("RemapOverlay", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
                                   ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoInputs);


        float windowWidth = displaySize.X * 0.2f;
        float windowHeight = displaySize.Y * 0.15f;

        var windowPos = new Vector2((displaySize.X - windowWidth) / 2f, (displaySize.Y - windowHeight) / 2f);


        ImGui.SetCursorPos(windowPos);
        ImGui.BeginChild("RemapBackground", new Vector2(windowWidth, windowHeight), ImGuiChildFlags.Border);
        ImGui.EndChild();


        GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), displaySize.Y, 0.005f);


        float contentPadding = windowWidth * 0.05f;
        var contentPos = windowPos + new Vector2(contentPadding, contentPadding);
        var contentSize = new Vector2(windowWidth - contentPadding * 2, windowHeight - contentPadding * 2);

        ImGui.SetCursorPos(contentPos);
        ImGui.BeginChild("RemapContent", contentSize, ImGuiChildFlags.None);


        float fontSize = displaySize.Y * 0.018f;
        float lineHeight = contentSize.Y / 6f;

        var action = InputManager.Instance.GetAction(remappingAction);

        float currentY = 0;


        var headerText = "Press any key or mouse button";
        var headerSize = ImGui.CalcTextSize(headerText);
        ImGui.SetCursorPos(new Vector2((contentSize.X - headerSize.X) / 2f, currentY));
        ImGui.TextColored(new Vector4(1, 1, 0.4f, 1), headerText);
        currentY += lineHeight;

        var list = ImGui.GetWindowDrawList();

        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.8f, 0.8f, 0.8f, 1f));
        var cancelText = "ESC to cancel";
        var cancelSize = ImGui.CalcTextSize(cancelText);
        ImGui.SetCursorPos(new Vector2((contentSize.X - cancelSize.X) / 2f, contentSize.Y - lineHeight * 3.5f));
        ImGui.Text(cancelText);
        ImGui.PopStyleColor();

        float timeoutBarWidth = contentSize.X * 0.7f;
        float timeoutBarHeight = contentSize.Y * 0.02f;
        timeoutBarHeight = Math.Max(timeoutBarHeight, 3f);
        float timeoutBarX = contentPos.X + (contentSize.X - timeoutBarWidth) / 2f;
        float timeoutBarY = contentPos.Y + contentSize.Y - lineHeight * 0.6f;
        float timeoutProgress = remapTimeout / MAX_REMAP_TIME;


        list.AddRectFilled(
            new Vector2(timeoutBarX, timeoutBarY),
            new Vector2(timeoutBarX + timeoutBarWidth, timeoutBarY + timeoutBarHeight),
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.2f, 0.2f, 0.5f)),
            timeoutBarHeight * 0.5f
        );


        var barColor = timeoutProgress > 0.3f ?
            new Vector4(0.3f, 1f, 0.3f, 0.8f) :
            new Vector4(1f, 0.3f, 0.3f, 0.8f);

        if (timeoutProgress > 0)
        {
            list.AddRectFilled(
                new Vector2(timeoutBarX, timeoutBarY),
                new Vector2(timeoutBarX + timeoutBarWidth * timeoutProgress, timeoutBarY + timeoutBarHeight),
                ImGui.ColorConvertFloat4ToU32(barColor),
                timeoutBarHeight * 0.5f
            );
        }


        var timeText = $"{(int)remapTimeout}s";
        var timeSize = ImGui.CalcTextSize(timeText);
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 1f, 1f, 0.9f));
        ImGui.SetCursorPos(new Vector2((contentSize.X - timeSize.X) / 2f,
            timeoutBarY - contentPos.Y - fontSize * 1.2f));
        ImGui.Text(timeText);
        ImGui.PopStyleColor();

        ImGui.EndChild();
        ImGui.End();
        ImGui.PopStyleColor();
    }

    public void Update()
    {
        if (isRemapping)
        {
            remapTimeout -= Time.Delta;
            if (remapTimeout <= 0)
            {
                CancelRemapping();
                return;
            }

            if (remapper.Update())
            {
                isRemapping = false;
                remappingAction = null;
                sameKeys = ValidateSameBindings();
            }
        }
    }

    private void CancelRemapping()
    {
        remapper.CancelRemapping();
        isRemapping = false;
        remappingAction = null;
        sameKeys = ValidateSameBindings();
    }
}