using Engine;
using Engine.InputPro;
using ImGuiNET;

using System.Numerics;


namespace Spacebox.Game.GUI.Menu;

public class ControlsWindow : MenuWindow
{
    private GameMenu menu;
    private InputRemapper remapper;
    private string remappingAction = null;
    private bool isRemapping = false;
    private float remapTimeout = 0f;
    private const float MAX_REMAP_TIME = 10f;

    public ControlsWindow(GameMenu menu)
    {
        this.menu = menu;
        remapper = new InputRemapper();
        InputManager.Instance.LoadConfiguration("Resources/default_input.json");
    }

    public override void Render()
    {
        SettingsUI.Render("Controls", "Controls", menu, 5,
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

    private void RenderControlsTable(Vector2 listSize, float rowH)
    {
        ImGui.BeginTable("table##controls", 4, ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.ScrollY | ImGuiTableFlags.BordersInnerV );
        float totalW = listSize.X;
        ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed , totalW * 0.22f);
        ImGui.TableSetupColumn("Description", ImGuiTableColumnFlags.WidthFixed, totalW * 0.4f);
        ImGui.TableSetupColumn("Key", ImGuiTableColumnFlags.WidthFixed, totalW * 0.2f);
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, totalW * 0.15f);

        ImGui.TableHeadersRow();

        var actions = InputManager.Instance.GetAllActions();
        foreach (var action in actions.OrderBy(a => a.Value.Name))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Text(action.Value.Name);

            ImGui.TableNextColumn();
            ImGui.TextWrapped(action.Value.Description);

            ImGui.TableNextColumn();
            string bindingText = GetBindingDisplayText(action.Value);
            ImGui.Text(bindingText);

            ImGui.TableNextColumn();
            if (isRemapping && remappingAction == action.Key)
            {
                ImGui.TextColored(new Vector4(1, 1, 0, 1), "Listening...");
            }
            else
            {
                ImGui.Dummy(new Vector2(rowH, rowH/2f));ImGui.SameLine();
                if (ImGui.Button($"Change##{action.Value.Name} "))
                {
                    StartRemapping(action.Key);
                }
            }
        }
        ImGui.EndTable();
    }

    private string GetBindingDisplayText(Engine.InputPro.InputAction action)
    {
        if (action.Bindings.Count == 0)
            return "None";

        var displayNames = action.Bindings.Select(b => b.GetDisplayName());
        return string.Join(" / ", displayNames);
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
        var actionDisplayName = action?.Name ?? remappingAction;
        var actionDescription = !string.IsNullOrEmpty(action?.Description) ? action.Description : "";

        float currentY = 0;

       
        var headerText = "Press any key or mouse button";
        var headerSize = ImGui.CalcTextSize(headerText);
        ImGui.SetCursorPos(new Vector2((contentSize.X - headerSize.X) / 2f, currentY));
        ImGui.TextColored( new Vector4(1,1,0.4f,1), headerText);
        currentY += lineHeight;


        /*ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 1f, 0.5f, 1f));
        var actionText = actionDisplayName;
        var actionSize = ImGui.CalcTextSize(actionText);
        ImGui.SetCursorPos(new Vector2((contentSize.X - actionSize.X) / 2f, currentY));
        ImGui.Text(actionText);
        ImGui.PopStyleColor();
        currentY += lineHeight * 0.8f;*/


        /*if (!string.IsNullOrEmpty(actionDescription))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.8f, 0.8f, 0.8f, 0.8f));
            var descSize = ImGui.CalcTextSize(actionDescription);
            if (descSize.X > contentSize.X * 0.9f)
            {
                ImGui.SetCursorPos(new Vector2(contentSize.X * 0.05f, currentY));
                ImGui.PushTextWrapPos(contentPos.X + contentSize.X * 0.95f);
                ImGui.TextWrapped(actionDescription);
                ImGui.PopTextWrapPos();
            }
            else
            {
                ImGui.SetCursorPos(new Vector2((contentSize.X - descSize.X) / 2f, currentY));
                ImGui.Text(actionDescription);
            }
            ImGui.PopStyleColor();
            currentY += lineHeight * 0.8f;
        }*/

        /*
         var currentBinding = GetBindingDisplayText(action);
         if (!string.IsNullOrEmpty(currentBinding) && currentBinding != "None")
         {
             ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.6f, 0.6f, 0.6f, 0.7f));
             var currentText = $"Current: {currentBinding}";
             var currentSize = ImGui.CalcTextSize(currentText);
             ImGui.SetCursorPos(new Vector2((contentSize.X - currentSize.X) / 2f, currentY));
             ImGui.Text(currentText);
             ImGui.PopStyleColor();
             currentY += lineHeight * 0.8f;
         }
        */
       
        var list = ImGui.GetWindowDrawList();

        /*list.AddLine(
            new Vector2(contentPos.X + contentSize.X * 0.2f, contentPos.Y + currentY),
            new Vector2(contentPos.X + contentSize.X * 0.8f, contentPos.Y + currentY),
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 0.3f)),
            displaySize.Y * 0.001f 
        );*/

      
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

            if (remapper.Update(Input.Keyboard, Input.Mouse))
            {
                isRemapping = false;
                remappingAction = null;
            }
        }
    }

    private void CancelRemapping()
    {
        remapper.CancelRemapping();
        isRemapping = false;
        remappingAction = null;
    }
}

public static class SettingsUI
{
    public static void Render(string windowId, string header, GameMenu menu, int buttonCount,
        Action<Vector2, float> drawContent, Action onSave, Action onBack)
    {
        var io = ImGui.GetIO();
        float ww = io.DisplaySize.X * 0.4f;
        float wh = io.DisplaySize.Y * 0.5f;
        var pos = GameMenu.CenterNextWindow2(ww, wh);

        ImGui.SetNextWindowPos(pos);
        ImGui.SetNextWindowSize(new Vector2(ww, wh));
        ImGui.Begin(windowId, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar);

        GameMenu.DrawElementColors(pos, new Vector2(ww, wh), io.DisplaySize.Y, 0.005f);

        float btnW = ww * 0.9f;
        float btnH = wh * 0.08f;
        float spacing = wh * 0.02f;

        ImGui.SetCursorPos(new Vector2((ww - btnW) / 2f, spacing));
        var textSize = ImGui.CalcTextSize(header);
        ImGui.SetCursorPos(new Vector2((ww - textSize.X) / 2f, spacing));
        ImGui.Text(header);

        float headerBlockH = ImGui.GetTextLineHeightWithSpacing() * 1.5f;
        var listSize = new Vector2(btnW, wh - btnH * 1.5f - spacing * 4 - headerBlockH);

        ImGui.SetCursorPos(new Vector2((ww - btnW) / 2f, spacing * 2 + headerBlockH));
        ImGui.BeginChild($"list##{windowId}", listSize);

        float rowH = 30f;
        drawContent(listSize, rowH);

        ImGui.EndChild();

        ImGui.SetCursorPos(new Vector2((ww - btnW) / 2f, wh - btnH - spacing));
        menu.ButtonWithBackground("Save", new Vector2(listSize.X / 2f - spacing, btnH),
            new Vector2((ww - btnW) / 2f, wh - btnH - spacing), onSave);
        menu.ButtonWithBackground("Back", new Vector2(listSize.X / 2f - spacing, btnH),
            new Vector2((ww - btnW) / 2f + listSize.X / 2f + spacing, wh - btnH - spacing), onBack);

        ImGui.End();
    }
}