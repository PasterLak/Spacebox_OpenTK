using System.Reflection;
using Engine.CanvasUI;
using Engine.SceneManagement;
using OpenTK.Mathematics;

namespace Engine.UI;

public sealed class DevSceneSection : DevPanelSection
{
    private readonly ItemList _scenes = new ItemList { CustomMinimumSize = new Vector2(0f, 110f) };
    private readonly Label _current = new Label();
    private string _currentName;

    public DevSceneSection() : base("Scenes")
    {
        var buttons = new HBoxContainer();
        var reload = new Button { Text = "Reload scene" };
        reload.Pressed += _ => SceneManager.Reload();
        var back = new Button { Text = "Back" };
        back.Pressed += _ => SceneManager.LoadPrevious();
        buttons.AddChild(reload);
        buttons.AddChild(back);

        Content.AddChild(_current);
        Content.AddChild(buttons);
        Content.AddChild(_scenes);
        _scenes.TooltipText = "Double click to load";
        _scenes.ItemActivated += index =>
        {
            if (_scenes.Items[index].Metadata is Type type) SceneManager.Load(type);
        };

        foreach (var type in RegisteredScenes())
            _scenes.AddItem(type.Name.Replace("Scene", ""), null, type);
    }

    public override void Refresh()
    {
        string name = SceneManager.Current?.GetType().Name ?? "-";
        if (name == _currentName) return;
        _currentName = name;
        _current.Text = "Current: " + name;
    }

    private static IEnumerable<Type> RegisteredScenes()
    {
        var field = typeof(SceneManager).GetField("_registeredScenes", BindingFlags.NonPublic | BindingFlags.Static);
        if (field?.GetValue(null) is not HashSet<Type> set) return Array.Empty<Type>();

        var list = new List<Type>();
        foreach (var type in set)
            if (type != typeof(ErrorScene)) list.Add(type);
        list.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        return list;
    }
}
