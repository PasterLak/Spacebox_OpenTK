using Engine;
using Engine.InputPro;
using System.Linq;

namespace Spacebox.Game.GUI.Menu;

public class ControlsWindow : MenuWindow, IOptionsCategory
{
    private readonly IOptionsNavigator _options;
    private InputRemapper remapper;
    private string remappingAction = null;
    private bool isRemapping = false;
    private float remapTimeout = 0f;
    private const float MAX_REMAP_TIME = 10f;
    private List<string> sameKeys = new List<string>();

    public ControlsWindow(IOptionsNavigator options)
    {
        _options = options;
        remapper = new InputRemapper();
        InputManager.Instance.LoadConfiguration(OptionsMenu.InputConfigPath);
    }

    public string CaptureState() => InputManager.Instance.SerializeConfiguration();

    public void Save() => InputManager.Instance.SaveConfiguration(OptionsMenu.InputConfigPath);

    public void Discard() => InputManager.Instance.LoadConfiguration(OptionsMenu.InputConfigPath);

    public bool IsRemapping => isRemapping;
    public string RemappingAction => remappingAction;
    public float RemapTimeLeft => remapTimeout;
    public float RemapProgress => remapTimeout / MAX_REMAP_TIME;
    public int BindingsVersion { get; private set; }
    public IOptionsNavigator Options => _options;

    public bool IsDuplicate(string bindingText) => sameKeys.Contains(bindingText);

    public string BindingText(InputAction action) => GetBindingDisplayText(action);

    public void Remap(string actionId) => StartRemapping(actionId);

    public void Cancel()
    {
        if (isRemapping) CancelRemapping();
    }

    protected override Spacebox.Game.GUI.MenuUi.MenuPage CreatePage() => new Spacebox.Game.GUI.MenuUi.ControlsPage(this);

    public override void OnWindowChanged()
    {
        sameKeys = ValidateSameBindings();
        BindingsVersion++;
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
                BindingsVersion++;
            }
        }
    }

    private void CancelRemapping()
    {
        remapper.CancelRemapping();
        isRemapping = false;
        remappingAction = null;
        sameKeys = ValidateSameBindings();
        BindingsVersion++;
    }
}